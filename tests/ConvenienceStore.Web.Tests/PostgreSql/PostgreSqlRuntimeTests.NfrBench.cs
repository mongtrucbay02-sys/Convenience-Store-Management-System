using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests.PostgreSql;

// #183/#264: reproducible NFR measurement evidence from disposable PG17 only.
// Timings cover HTTP response and not keyboard-to-render or production network.
// No SRS PASS is inferred from these proxy measurements.
public sealed partial class PostgreSqlRuntimeTests
{
    [Fact]
    [Trait("Category", "NFRBench")]
    public async Task Nfr_HttpTimingsAtOfficialScale_ProduceRawEvidence()
    {
        await using (var db = Db())
        {
            await db.ChayAsync("""
                INSERT INTO danh_muc(ten) VALUES('NFR Synthetic');
                INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id)
                SELECT 'NFR-'||lpad(g::text,5,'0'),'NFR product '||g,
                       (SELECT id FROM danh_muc ORDER BY id LIMIT 1),'Hop',1
                FROM generate_series(1,1000) AS g;
                INSERT INTO ton_kho(san_pham_id,so_luong_ton,gia_von_binh_quan)
                SELECT id,1000,5000 FROM san_pham;
                INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id)
                SELECT id,20000,NOW()-INTERVAL '1 year',1 FROM san_pham;
                INSERT INTO tai_khoan(id,ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
                SELECT t.id,t.login,u.mat_khau_bam,t.label,t.role,FALSE
                FROM (VALUES
                    (3,'pg_cashier2','NFR cashier 2',2),
                    (4,'pg_cashier3','NFR cashier 3',2),
                    (5,'pg_warehouse','NFR warehouse',3)
                ) t(id,login,label,role)
                CROSS JOIN (SELECT mat_khau_bam FROM tai_khoan WHERE id=2) u;
                INSERT INTO gio_hang(thu_ngan_id,trang_thai,hoan_tat_luc)
                SELECT 2,'DA_HOAN_TAT', NOW()-(g%180)*INTERVAL '1 day'
                FROM generate_series(1,10000) g;
                INSERT INTO hoa_don(ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,
                                     tong_tien_hang,tong_giam,tong_thanh_toan,tien_khach_dua,tien_thua)
                SELECT 'HD-NFR-'||lpad(id::text,8,'0'),id,2,NOW()-(id%180)*INTERVAL '1 day',
                       20000,0,20000,20000,0 FROM gio_hang;
                INSERT INTO ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,
                                        so_luong,don_gia,giam_don_vi,gia_von)
                SELECT id,(id%1000)+1,'NFR synthetic item','Hop',1,20000,0,5000 FROM hoa_don;
                """, default);
            Assert.Equal(1000L, (long)(await db.DocMotGiaTriAsync(
                "SELECT COUNT(*) FROM san_pham", default))!);
            Assert.Equal(10000L, (long)(await db.DocMotGiaTriAsync(
                "SELECT COUNT(*) FROM hoa_don", default))!);
        }

        var address = await StartWeb();
        async Task<HttpClient> Login(string username)
        {
            var client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = new CookieContainer(), AllowAutoRedirect = false
            }) { BaseAddress = address, Timeout = TimeSpan.FromSeconds(45) };
            using var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { tenDangNhap = username, matKhau = Password });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return client;
        }

        using var cashier = await Login("pg_cashier");
        using var cashier2 = await Login("pg_cashier2");
        using var cashier3 = await Login("pg_cashier3");
        using var warehouse = await Login("pg_warehouse");
        using var admin = await Login("pg_admin");

        var records = new Dictionary<string, object>();
        async Task<double> TimedGet(HttpClient client, string path)
        {
            var clock = Stopwatch.StartNew();
            using var response = await client.GetAsync(path);
            var payload = await response.Content.ReadAsByteArrayAsync();
            clock.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(payload);
            return clock.Elapsed.TotalMilliseconds;
        }
        async Task<double[]> MeasureGet(HttpClient client, string path)
        {
            for (var i = 0; i < 5; i++) await TimedGet(client, path);
            var values = new double[30];
            for (var i = 0; i < values.Length; i++) values[i] = await TimedGet(client, path);
            return values;
        }

        // HN-01 search HTTP proxy (not time from keypress to rendered suggestions).
        records["HN-01_search_http"] = await MeasureGet(cashier, "/Pos?search=NFR-00001");
        // HN-03 dashboard HTTP proxy; production data and display rendering not covered.
        records["HN-03_dashboard_http"] = await MeasureGet(admin, "/Dashboard");
        // HN-04 list page HTTP proxy.
        records["HN-04_inventory_page_http"] = await MeasureGet(admin, "/Kho?page=1");

        // HN-05 five distinct authenticated sessions, requests start concurrently.
        var rounds = new List<double[]>();
        for (var i = 0; i < 30; i++)
        {
            rounds.Add(await Task.WhenAll(
                TimedGet(cashier, "/Pos?search=NFR-00001"),
                TimedGet(cashier2, "/Pos?search=NFR-00002"),
                TimedGet(cashier3, "/Pos?search=NFR-00003"),
                TimedGet(warehouse, "/Kho?page=1"),
                TimedGet(admin, "/Dashboard")));
        }
        records["HN-05_concurrent_5_sessions_http"] = rounds;

        // HN-02 checkout server HTTP proxy; no extra load is sent to shared Supabase.
        async Task<string> Token(string html)
        {
            var match = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
            Assert.True(match.Success, "Expected CSRF token in POS form.");
            return WebUtility.HtmlDecode(match.Groups[1].Value);
        }
        var checkoutTimes = new double[30];
        for (var i = 0; i < 30; i++)
        {
            using var pos = await cashier.GetAsync("/Pos");
            Assert.Equal(HttpStatusCode.OK, pos.StatusCode);
            var posPage = await pos.Content.ReadAsStringAsync();
            using (var add = await cashier.PostAsync("/Pos/Add", new FormUrlEncodedContent(
                new Dictionary<string, string> {
                    ["__RequestVerificationToken"] = await Token(posPage),
                    ["productId"] = "1"
                })))
                Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);

            using var cash = await cashier.GetAsync("/Pos/Cash");
            Assert.Equal(HttpStatusCode.OK, cash.StatusCode);
            var cashPage = await cash.Content.ReadAsStringAsync();
            var token = await Token(cashPage);
            var clock = Stopwatch.StartNew();
            using var complete = await cashier.PostAsync("/Pos/CompleteOrder",
                new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["__RequestVerificationToken"] = token, ["cashReceived"] = "20000"
                }));
            clock.Stop();
            Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
            Assert.Contains("/Pos/Receipt", complete.Headers.Location?.OriginalString ?? "");
            checkoutTimes[i] = clock.Elapsed.TotalMilliseconds;
        }
        records["HN-02_checkout_http"] = checkoutTimes;
        await using (var verify = Db())
        {
            Assert.Equal(10030, await verify.HoaDons.CountAsync());
            Assert.Equal(970, (await verify.TonKhos.SingleAsync(t => t.SanPhamId == 1)).SoLuongTon);
        }
        static object Summary(double[] samples)
        {
            var sorted = samples.OrderBy(x => x).ToArray();
            return new { count = samples.Length,
                p50Ms = sorted[(int)Math.Ceiling(samples.Length * .5) - 1],
                p95Ms = sorted[(int)Math.Ceiling(samples.Length * .95) - 1],
                maxMs = sorted[^1], samplesMs = samples };
        }
        var stats = new {
            status = "MEASURED_HTTP_PROXIES_NOT_FINAL_SRS_ACCEPTANCE",
            source = "official SRS v1.0.3 PR301 HN-01..HN-05",
            limitation = "isolated PG17 GitHub-hosted runner; raw HTTP response != keyboard-to-paint/browser/user or production deployment, no approved statistical PASS rule",
            products = 1000, invoicesSeeded = 10000, months = 6, users = 5,
            measuredAtUtc = DateTimeOffset.UtcNow,
            HN01 = Summary((double[])records["HN-01_search_http"]),
            HN02 = Summary(checkoutTimes),
            HN03 = Summary((double[])records["HN-03_dashboard_http"]),
            HN04 = Summary((double[])records["HN-04_inventory_page_http"]),
            HN05 = rounds.SelectMany(x => x).ToArray(),
        };
        var dir = Path.Combine("artifacts", "nfr");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "nfr-http-proxies.json"),
            JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true }));
    }
}

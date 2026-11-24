using System.Net;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns;
using ConvenienceStore.Web.Tests.HaTang;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace ConvenienceStore.Web.Tests.Returns;

public sealed class ReturnSlipHttpTests(CsdlKhoFixture database) : IClassFixture<CsdlKhoFixture>
{
    [Fact]
    public async Task CashierSeesOnlyOwnSlips_AdminCanFilterAndReprintBothKinds()
    {
        await using var app = await ListFeatureHost.Start(database, services =>
        {
            services.AddModuleReturns();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
        await database.ChayAsync("ALTER TABLE tai_khoan ALTER COLUMN id RESTART WITH 2147483648");
        using var cashier = await ListFeatureHost.Client(app, database, "TN");
        using var other = await ListFeatureHost.Client(app, database, "TN");
        using var admin = await ListFeatureHost.Client(app, database, "AD");
        using var stockClerk = await ListFeatureHost.Client(app, database, "NK");
        using var guest = await ListFeatureHost.Client(app, database, null);

        var own = await SeedAsync(await ActorAsync(cashier), "TH-DT07-OWN", "HD-DT07-OWN", false);
        var foreign = await SeedAsync(await ActorAsync(other), "DT-DT07-OTHER", "HD-DT07-OTHER", true);

        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("/Returns/Slips")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stockClerk.GetAsync("/Returns/Slips")).StatusCode);

        var cashierList = await PageAsync(cashier, "/Returns/Slips");
        Assert.Contains("TH-DT07-OWN", cashierList);
        Assert.DoesNotContain("DT-DT07-OTHER", cashierList);
        Assert.Equal(HttpStatusCode.NotFound, (await cashier.GetAsync($"/Returns/Slips/{foreign}")).StatusCode);
        using var deniedPrint = await cashier.GetAsync($"/Returns/Slips/{foreign}/Print");
        using var missingPrint = await cashier.GetAsync("/Returns/Slips/999999/Print");
        Assert.Equal(HttpStatusCode.NotFound, deniedPrint.StatusCode);
        Assert.Equal(missingPrint.StatusCode, deniedPrint.StatusCode);
        var deniedPage = WebUtility.HtmlDecode(await deniedPrint.Content.ReadAsStringAsync());
        Assert.Contains("Không tìm thấy phiếu đổi trả", deniedPage);
        Assert.DoesNotContain("DT-DT07-OTHER", deniedPage);
        Assert.Contains("BẢN IN LẠI", await PageAsync(cashier, $"/Returns/Slips/{own}/Print"));

        var adminList = await PageAsync(admin, "/Returns/Slips");
        Assert.Contains("TH-DT07-OWN", adminList);
        Assert.Contains("DT-DT07-OTHER", adminList);
        var filtered = await PageAsync(admin, "/Returns/Slips?code=dt-dt07-other&invoiceCode=hd-dt07-other&from=2026-11-24&to=2026-11-24&reason=HET_HAN&status=CHO_DUYET");
        Assert.Contains($"/Returns/Slips/{foreign}", filtered);
        Assert.DoesNotContain($"/Returns/Slips/{own}", filtered);

        foreach (var filter in new[] { "code=NO-MATCH", "invoiceCode=NO-MATCH", "to=2026-11-23",
            "from=2026-11-25", "code=DT-DT07-OTHER&status=HOAN_TAT", "reason=KHONG_DUNG_NHU_CAU" })
        {
            Assert.Contains("Không tìm thấy phiếu đổi trả phù hợp.",
                await PageAsync(admin, "/Returns/Slips?" + filter));
        }
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.GetAsync("/Returns/Slips?from=2026-11-25&to=2026-11-24")).StatusCode);
        var detail = await PageAsync(admin, $"/Returns/Slips/{foreign}");
        Assert.Contains("Hết hạn", detail);
        Assert.Contains("Sản phẩm kiểm thử DT-07", detail);
        Assert.Contains("HD-DT07-OTHER", detail);
        Assert.Contains("Thu thêm dự kiến", detail);
        using var print = await admin.GetAsync($"/Returns/Slips/{foreign}/Print");
        print.EnsureSuccessStatusCode();
        Assert.True(print.Headers.CacheControl?.NoStore);
        var printed = WebUtility.HtmlDecode(await print.Content.ReadAsStringAsync());
        Assert.Contains("BẢN IN LẠI", printed);
        Assert.Contains("slip-print", printed);
        Assert.Contains("window.print()", printed);
        Assert.Contains("HD-DT07-OTHER", printed);
        Assert.Contains("Thu thêm dự kiến", printed);
        Assert.DoesNotContain("sidebar-account", printed);
    }

    private static async Task<string> PageAsync(HttpClient client, string path) =>
        WebUtility.HtmlDecode(await client.GetStringAsync(path));

    private async Task<long> ActorAsync(HttpClient client)
    {
        string cookie = client.DefaultRequestHeaders.GetValues("Cookie").Single();
        return Convert.ToInt64(await database.DocMotGiaTriAsync(
            "SELECT tai_khoan_id FROM phien_dang_nhap WHERE token_bam=@token",
            ("@token", TokenPhien.Bam(cookie[(cookie.IndexOf('=') + 1)..]))));
    }

    private async Task<long> SeedAsync(long cashier, string code, string invoiceCode, bool exchange)
    {
        int product = await new DuLieuTestKho(database).TaoSanPhamAsync("DT07");
        var at = new DateTime(2026, 11, 24, 14, 0, 0);
        long cart = await InsertAsync("""
            INSERT INTO gio_hang (thu_ngan_id, trang_thai, hoan_tat_luc)
            VALUES (@cashier, 'DA_HOAN_TAT', @at) RETURNING id;
            """, ("@cashier", cashier), ("@at", at));
        long invoice = await InsertAsync("""
            INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, thanh_toan_luc,
                tong_tien_hang, tong_giam, tong_thanh_toan, tien_khach_dua, tien_thua)
            VALUES (@code, @cart, @cashier, @at, 10000, 0, 10000, 10000, 0) RETURNING id;
            """, ("@code", invoiceCode), ("@cart", cart), ("@cashier", cashier), ("@at", at));
        await database.ChayAsync("""
            INSERT INTO ct_hoa_don (hoa_don_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, don_gia, gia_von)
            VALUES (@invoice, @product, 'Sản phẩm kiểm thử DT-07', 'Cái', 1, 10000, 3000)
            """, ("@invoice", invoice), ("@product", product));
        long slip = await InsertAsync("""
            INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, chinh_sach_id, trang_thai,
                can_duyet_danh_muc, gia_tri_hang_tra, tien_hoan, tien_thu_them, thu_ngan_id, ngay_lap, hoan_tat_luc)
            VALUES (@code, @kind, @invoice, (SELECT MIN(id) FROM chinh_sach_doi_tra), @status,
                @exception, 10000, @refund, @cash, @cashier, @at, CASE WHEN @exception THEN NULL ELSE @at END) RETURNING id;
            """, ("@code", code), ("@kind", exchange ? "DOI_HANG" : "TRA_HANG"), ("@invoice", invoice),
            ("@status", exchange ? "CHO_DUYET" : "HOAN_TAT"), ("@exception", exchange),
            ("@refund", exchange ? 0 : 10000), ("@cash", exchange ? 5000 : 0), ("@cashier", cashier),
            ("@at", at));
        await database.ChayAsync("""
            INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong,
                don_gia_thuc_tra, ly_do, cach_xu_ly, ngoai_le, gia_von)
            VALUES (@slip, @product, @invoice, 1, 10000, @reason, 'HUY', @exception, 3000)
            """, ("@slip", slip), ("@product", product), ("@invoice", invoice),
            ("@reason", exchange ? "HET_HAN" : "LOI_SAN_PHAM"), ("@exception", exchange));
        return slip;
    }

    private async Task<long> InsertAsync(string sql, params (string, object?)[] args) =>
        Convert.ToInt64(await database.DocMotGiaTriAsync(sql, args));
}

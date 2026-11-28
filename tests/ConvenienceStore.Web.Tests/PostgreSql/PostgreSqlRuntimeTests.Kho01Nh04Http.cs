using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests.PostgreSql;

// SRS official Must KHO-01 and NH-04, real HTTP through disposable PG17.
// This is not a browser test or a live Supabase role acceptance test.
public sealed partial class PostgreSqlRuntimeTests
{
    [Fact]
    public async Task Kho01_HttpStockListFiltersAndNoMutation()
    {
        var product = await Product(stock: 3);
        await using (var db = Db())
            await db.ChayAsync("UPDATE ton_kho SET nguong_toi_thieu=5 WHERE san_pham_id=@id",
                default, ("@id", product));

        var address = await StartWeb();
        using var client = new HttpClient(new HttpClientHandler
        {
            CookieContainer = new CookieContainer(), AllowAutoRedirect = false
        }) { BaseAddress = address };
        using (var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { tenDangNhap = "pg_admin", matKhau = Password }))
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using (var response = await client.GetAsync("/Kho?status=low"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Sữa tươi", html);
            Assert.Contains("Sắp hết", html);
            Assert.Contains("Ngưỡng tối thiểu", html);
        }
        using (var response = await client.GetAsync("/Kho?status=out"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("Sữa tươi", await response.Content.ReadAsStringAsync());
        }
        using (var response = await client.GetAsync("/Kho?page=0"))
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var verify = Db();
        Assert.Equal(3, (await verify.TonKhos.AsNoTracking().SingleAsync()).SoLuongTon);
        Assert.Empty(await verify.TheKhos.ToListAsync());
        Assert.Empty(await verify.HoaDons.ToListAsync());
    }

    [Fact]
    public async Task Nh04_HttpCreateDraftPersistsLinesAndDoesNotMoveStock()
    {
        var product = await Product(stock: 8);
        long supplierId;
        await using (var db = Db())
        {
            supplierId = await db.InsertIdAsync(
                "INSERT INTO nha_cung_cap(ma,ten,so_dien_thoai) VALUES('NCC-NH04','Nhà cung cấp NH04','0909876543')",
                default);
        }

        var address = await StartWeb();
        using var client = new HttpClient(new HttpClientHandler
        {
            CookieContainer = new CookieContainer(), AllowAutoRedirect = false
        }) { BaseAddress = address };
        using (var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { tenDangNhap = "pg_admin", matKhau = Password }))
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var form = await client.GetAsync("/NhapHang/Tao");
        Assert.Equal(HttpStatusCode.OK, form.StatusCode);
        var html = await form.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "Receipt creation requires a real antiforgery token.");
        using (var create = await client.PostAsync("/NhapHang/Tao",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
                ["SupplierId"] = supplierId.ToString(),
                ["SupplierDocument"] = "NH04-SYNTHETIC",
                ["Lines[0].ProductId"] = product.ToString(),
                ["Lines[0].Quantity"] = "4",
                ["Lines[0].UnitCost"] = "9000"
            })))
            Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

        await using var verify = Db();
        var receipt = await verify.PhieuNhaps.AsNoTracking()
            .Include(r => r.CtPhieuNhaps).SingleAsync();
        Assert.Equal("NHAP", receipt.TrangThai);
        Assert.Equal(supplierId, receipt.NhaCungCapId);
        Assert.Equal(36000L, receipt.TongTien);
        var line = Assert.Single(receipt.CtPhieuNhaps);
        Assert.Equal(4L, line.SoLuong);
        Assert.Equal(9000L, line.GiaNhap);
        Assert.Equal(8, (await verify.TonKhos.SingleAsync()).SoLuongTon);
        Assert.Empty(await verify.TheKhos.ToListAsync());

        using (var details = await client.GetAsync("/NhapHang/" + receipt.Id))
            Assert.Equal(HttpStatusCode.OK, details.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests.PostgreSql;

// QA-02: official SRS POS-02. HTTP actions with a real authenticated cashier,
// real antiforgery tokens and a disposable loopback PostgreSQL 17 database.
// This does NOT count as browser E2E or exact smartmart_backend ACL acceptance.
public sealed partial class PostgreSqlRuntimeTests
{
    [Fact]
    public async Task Pos02_HttpEditRemoveAndInvalidQuantity_DoNotChangeStockOrCreateInvoice()
    {
        var product = await Product(stock: 3);
        var address = await StartWeb();
        using var client = new HttpClient(new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = false
        }) { BaseAddress = address };
        using (var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { tenDangNhap = "pg_cashier", matKhau = Password }))
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        async Task<string> Index()
        {
            using var response = await client.GetAsync("/Pos");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        async Task<HttpResponseMessage> Post(string route, string page, params (string Key, string Value)[] fields)
        {
            var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(match.Success, "A POS form antiforgery token must be rendered.");
            var values = fields.Select(field =>
                new KeyValuePair<string, string>(field.Key, field.Value)).ToList();
            values.Add(new("__RequestVerificationToken", WebUtility.HtmlDecode(match.Groups[1].Value)));
            return await client.PostAsync(route, new FormUrlEncodedContent(values));
        }

        async Task<int> Quantity()
        {
            await using var db = Db();
            return Convert.ToInt32(await db.DocMotGiaTriAsync(
                "SELECT COALESCE((SELECT so_luong FROM ct_gio_hang WHERE san_pham_id=@p), 0)",
                default, ("@p", product)));
        }

        var page = await Index();
        using (var add = await Post("/Pos/Add", page, ("productId", product.ToString())))
            Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);
        Assert.Equal(1, await Quantity());

        page = await Index();
        using (var update = await Post("/Pos/Update", page,
            ("productId", product.ToString()), ("quantity", "3")))
            Assert.Equal(HttpStatusCode.Redirect, update.StatusCode);
        Assert.Equal(3, await Quantity());

        foreach (var invalid in new[] { "4", "-1", "abc", "1.5" })
        {
            page = await Index();
            using var rejected = await Post("/Pos/Update", page,
                ("productId", product.ToString()), ("quantity", invalid));
            Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
            Assert.Equal(3, await Quantity());
        }

        // An anonymous direct POST without an anti-forgery token must not mutate the cart.
        using (var missingToken = await client.PostAsync("/Pos/Remove",
            new FormUrlEncodedContent(new Dictionary<string, string>
            { ["productId"] = product.ToString() })))
            Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
        Assert.Equal(3, await Quantity());

        page = await Index();
        using (var remove = await Post("/Pos/Remove", page, ("productId", product.ToString())))
            Assert.Equal(HttpStatusCode.Redirect, remove.StatusCode);
        Assert.Equal(0, await Quantity());

        // POS-02 also permits deleting a line by setting quantity zero.
        page = await Index();
        using (var add = await Post("/Pos/Add", page, ("productId", product.ToString())))
            Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);
        page = await Index();
        using (var deleteByQuantity = await Post("/Pos/Update", page,
            ("productId", product.ToString()), ("quantity", "0")))
            Assert.Equal(HttpStatusCode.Redirect, deleteByQuantity.StatusCode);
        Assert.Equal(0, await Quantity());

        await using var verify = Db();
        Assert.Equal(3, (await verify.TonKhos.SingleAsync()).SoLuongTon);
        Assert.Empty(await verify.HoaDons.ToListAsync());
        Assert.Empty(await verify.TheKhos.ToListAsync());
    }
}

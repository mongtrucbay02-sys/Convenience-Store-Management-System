using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests.PostgreSql;

public sealed partial class PostgreSqlRuntimeTests
{
    [Fact]
    public async Task PosHttp_CashierCheckoutToReceipt()
    {
        var product = await Product();
        var address = await StartWeb();
        using var client = new HttpClient(new HttpClientHandler
        {
            CookieContainer = new CookieContainer(), AllowAutoRedirect = false
        }) { BaseAddress = address };
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { tenDangNhap = "pg_cashier", matKhau = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        async Task<string> Page(string path)
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }
        async Task<HttpResponseMessage> Submit(string path, string html,
            params (string Key, string Value)[] fields)
        {
            var token = Regex.Match(html, """name="__RequestVerificationToken"[^>]*value="([^"]+)" """.Trim());
            Assert.True(token.Success, "Missing rendered antiforgery token");
            var values = fields.Select(f => new KeyValuePair<string,string>(f.Key,f.Value)).ToList();
            values.Add(new("__RequestVerificationToken", WebUtility.HtmlDecode(token.Groups[1].Value)));
            return await client.PostAsync(path, new FormUrlEncodedContent(values));
        }
        var index = await Page("/Pos");
        using (var add = await Submit("/Pos/Add", index, ("productId", product.ToString())))
            Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);
        index = await Page("/Pos");
        using (var update = await Submit("/Pos/Update", index, ("productId", product.ToString()), ("quantity", "2")))
            Assert.Equal(HttpStatusCode.Redirect, update.StatusCode);
        await Page("/Pos/Checkout");
        var cash = await Page("/Pos/Cash");
        using (var confirm = await Submit("/Pos/ConfirmCash", cash, ("cashReceived", "100000")))
        {
            Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
            cash = await confirm.Content.ReadAsStringAsync();
        }
        Assert.Contains("completeOrderForm", cash);
        using (var complete = await Submit("/Pos/CompleteOrder", cash, ("cashReceived", "100000")))
        {
            Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
            Assert.NotNull(complete.Headers.Location);
            Assert.Contains("/Pos/Receipt", complete.Headers.Location!.OriginalString, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("receipt-page", await Page(complete.Headers.Location.OriginalString));
        }
        await using var db = Db();
        Assert.Equal(40000, (await db.HoaDons.SingleAsync()).TongThanhToan);
        Assert.Equal(18, (await db.TonKhos.SingleAsync()).SoLuongTon);
        Assert.Equal(1040000, (await db.QuyTienMats.SingleAsync()).SoDu);
        Assert.Single(await db.GiaoDichTienMats.ToListAsync());
    }
}

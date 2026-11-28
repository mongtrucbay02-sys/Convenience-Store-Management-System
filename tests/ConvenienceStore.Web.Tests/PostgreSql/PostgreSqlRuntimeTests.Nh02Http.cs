using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests.PostgreSql;

// Official Must NH-02: independent create/edit supplier HTTP regression.
// Runs only against a unique disposable loopback PG17 database.
public sealed partial class PostgreSqlRuntimeTests
{
    [Fact]
    public async Task Nh02_HttpCreateEditDuplicatePhoneStaleVersionAndCsrf()
    {
        var address = await StartWeb();
        using var client = new HttpClient(new HttpClientHandler
        {
            CookieContainer = new CookieContainer(), AllowAutoRedirect = false
        }) { BaseAddress = address };
        using (var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { tenDangNhap = "pg_admin", matKhau = Password }))
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        async Task<string> Form(string path)
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        async Task<HttpResponseMessage> Save(string html, long id, long version,
            string name, string phone)
        {
            var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(token.Success, "Supplier form must contain an antiforgery token.");
            return await client.PostAsync("/NhaCungCap/Sua", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
                    ["Id"] = id.ToString(),
                    ["Version"] = version.ToString(),
                    ["Name"] = name,
                    ["Phone"] = phone
                }));
        }

        var form = await Form("/NhaCungCap/Sua");
        using (var create = await Save(form, 0, 0, "NCC kiểm thử", "0901234567"))
            Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

        long id, version;
        await using (var db = Db())
        {
            var supplier = await db.NhaCungCaps.AsNoTracking().SingleAsync();
            id = supplier.Id; version = supplier.PhienBan;
            Assert.StartsWith("NCC-", supplier.Ma);
            Assert.Equal("NCC kiểm thử", supplier.Ten);
            Assert.Equal("0901234567", supplier.SoDienThoai);
        }

        // Server rejects a duplicate unique phone and does not insert a second row.
        form = await Form("/NhaCungCap/Sua");
        using (var duplicate = await Save(form, 0, 0, "NCC trùng điện thoại", "0901234567"))
        {
            Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
            // Keep the assertion independent of localized UI text/HTML encoding.
            Assert.Contains("<form", await duplicate.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
        await using (var db = Db()) Assert.Single(await db.NhaCungCaps.ToListAsync());

        form = await Form("/NhaCungCap/Sua/" + id);
        using (var update = await Save(form, id, version, "NCC đã sửa", "0901234568"))
            Assert.Equal(HttpStatusCode.Redirect, update.StatusCode);

        await using (var db = Db())
        {
            var edited = await db.NhaCungCaps.AsNoTracking().SingleAsync();
            Assert.Equal("NCC đã sửa", edited.Ten);
            Assert.Equal("0901234568", edited.SoDienThoai);
            Assert.Equal(version + 1, edited.PhienBan);
        }

        // Prior version cannot silently overwrite a more recent update.
        form = await Form("/NhaCungCap/Sua/" + id);
        using (var stale = await Save(form, id, version, "Dữ liệu cũ", "0901234569"))
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        using (var missingCsrf = await client.PostAsync("/NhaCungCap/Sua",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(), ["Version"] = (version + 1).ToString(),
                ["Name"] = "Thiếu CSRF", ["Phone"] = "0901234570"
            })))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        await using (var final = Db())
        {
            var unchanged = await final.NhaCungCaps.AsNoTracking().SingleAsync();
            Assert.Equal("NCC đã sửa", unchanged.Ten);
            Assert.Equal("0901234568", unchanged.SoDienThoai);
            Assert.Equal(version + 1, unchanged.PhienBan);
        }
    }
}

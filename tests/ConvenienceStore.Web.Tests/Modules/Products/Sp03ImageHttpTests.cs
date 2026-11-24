using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Products;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp03ImageHttpTests(CsdlMySqlTam db) : IClassFixture<CsdlMySqlTam>
{
    internal static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl6eAAAAABJRU5ErkJggg==");
    [Theory]
    [InlineData(null, 401)]
    [InlineData("TN", 403)]
    [InlineData("FORCED", 403)]
    [InlineData("AD", 200)]
    [InlineData("NK", 200)]
    public async Task UploadUsesContentValidationAuthorizationAndVersion(string? role, int status)
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, role); Sp08CategoryHttpTests.AddXsrf(client);
        using var content = Upload(Png, "image/png", 1);
        using var response = await client.PostAsync($"/api/v1/products/{ids.Active}/image", content);
        Assert.Equal(status, (int)response.StatusCode);
        if (status != 200) return;
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var url = json.RootElement.GetProperty("anhUrl").GetString()!;
        Assert.StartsWith("/uploads/products/", url); Assert.EndsWith(".png", url); Assert.DoesNotContain("unsafe", url);
        Assert.Equal(2, json.RootElement.GetProperty("phienBan").GetInt32());
        var path = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.Equal(Png, await File.ReadAllBytesAsync(path)); File.Delete(path); // Only the exact new random fixture-owned file.
    }

    [Fact]
    public async Task InvalidImagesXsrfAndStaleVersionsLeaveDatabaseUntouched()
    {
        var ids = await ProductReadSeed.Create(db);
        await using var app = await ListFeatureHost.Start(db, s => { s.AddModuleProducts(); s.AddModuleInventory(); });
        using var client = await ListFeatureHost.Client(app, db, "AD");
        using (var content = Upload(Png, "image/png", 1)) Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/v1/products/{ids.Active}/image", content)).StatusCode);
        Sp08CategoryHttpTests.AddXsrf(client);
        foreach (var (bytes, type) in new[] { (Array.Empty<byte>(), "image/png"), ("<script>evil</script>"u8.ToArray(), "image/png"), (Png, "application/pdf"), (new byte[2 * 1024 * 1024 + 1], "image/png") })
        {
            using var content = Upload(bytes, type, 1);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync($"/api/v1/products/{ids.Active}/image", content)).StatusCode);
        }
        using (var content = Upload(Png, "image/png", 77)) Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/v1/products/{ids.Active}/image", content)).StatusCode);
        using (var content = Upload(Png, "image/png", 1)) Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/v1/products/4294967295/image", content)).StatusCode);
        Assert.Null(await db.DocMotGiaTriAsync("SELECT anh_url FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        Assert.Equal(1, Convert.ToInt32(await db.DocMotGiaTriAsync("SELECT phien_ban FROM san_pham WHERE id=@id", ("@id", ids.Active))));
    }

    internal static MultipartFormDataContent Upload(byte[] bytes, string type, long version)
    {
        var content = new MultipartFormDataContent(); var file = new ByteArrayContent(bytes); file.Headers.ContentType = new MediaTypeHeaderValue(type);
        content.Add(file, "image", "../../unsafe.html"); content.Add(new StringContent(version.ToString()), "phienBan"); return content;
    }
}

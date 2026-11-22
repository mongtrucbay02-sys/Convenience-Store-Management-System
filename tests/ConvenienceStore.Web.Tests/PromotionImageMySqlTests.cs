using System.Security.Claims;
using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests;

public sealed class PromotionImageMySqlTests(CsdlKhoFixture database) : IClassFixture<CsdlKhoFixture>
{
    private IHttpContextAccessor Accessor() => new HttpContextAccessor { HttpContext = new DefaultHttpContext {
        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, database.TaiKhoanAdminId.ToString())], "test")) } };

    [Fact]
    public async Task ImageSurvivesContextRecreationAndCanBeReplacedOrRemoved()
    {
        long id;
        await using (var db = database.TaoDbContext())
        {
            var record = new PromotionRecord(0, "", "Ảnh kiểm thử", null, DiscountType.Percent, 10, null, [],
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), false, PromotionStatus.Draft, 0, "https://example.com/promo.jpg");
            id = (await new MySqlPromotionDataSource(db, Accessor()).CreateAsync(record, default)).Id;
        }
        await using (var db = database.TaoDbContext())
        {
            var source = new MySqlPromotionDataSource(db, Accessor());
            var saved = (await source.GetByIdAsync(id, default))!;
            Assert.Equal("https://example.com/promo.jpg", saved.ImageUrl);
            Assert.Equal(saved.ImageUrl, (await db.ChuongTrinhKhuyenMais.SingleAsync(x => x.Id == id)).AnhUrl);
            Assert.True(await source.UpdateAsync(saved with { ImageUrl = "https://example.com/new.jpg" }, default));
        }
        await using (var db = database.TaoDbContext())
        {
            var source = new MySqlPromotionDataSource(db, Accessor());
            var saved = (await source.GetByIdAsync(id, default))!;
            Assert.Equal("https://example.com/new.jpg", saved.ImageUrl);
            await source.UpdateAsync(saved with { ImageUrl = null }, default);
        }
        await using var last = database.TaoDbContext();
        Assert.Null((await new MySqlPromotionDataSource(last, Accessor()).GetByIdAsync(id, default))!.ImageUrl);
    }
}

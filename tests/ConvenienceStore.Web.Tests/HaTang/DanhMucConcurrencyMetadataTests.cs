using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests.HaTang;

public sealed class DanhMucConcurrencyMetadataTests
{
    [Fact]
    public void DanhMucVersion_IsConcurrencyToken()
    {
        var options = new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseMySQL("server=localhost;database=metadata_only")
            .Options;
        using var db = new CuaHangDbContext(options);

        var property = db.Model.FindEntityType(typeof(DanhMuc))!
            .FindProperty(nameof(DanhMuc.PhienBan))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal("phien_ban", property.GetColumnName());
    }

    [Fact]
    public void PromotionsVersion_RetainsExistingMapping()
    {
        var options = new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseMySQL("server=localhost;database=metadata_only")
            .Options;
        using var db = new CuaHangDbContext(options);

        var property = db.Model.FindEntityType(typeof(ChuongTrinhKhuyenMai))!
            .FindProperty(nameof(ChuongTrinhKhuyenMai.PhienBan))!;

        Assert.True(property.IsConcurrencyToken);
    }
}

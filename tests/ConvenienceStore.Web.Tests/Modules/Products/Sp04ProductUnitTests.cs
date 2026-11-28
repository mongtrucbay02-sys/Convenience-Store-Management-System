using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp04ProductUnitTests
{
    [Theory]
    [InlineData("Hộp", "Cái", 0, false, true)]
    [InlineData("Hộp", "Cái", 7, false, false)]
    [InlineData("Hộp", "Cái", 0, true, false)]
    [InlineData("Hộp", "Hộp", 7, true, true)]
    public void UnitIsImmutableAfterStockOrTransaction(string previous, string next, int stock, bool history, bool allowed)
    {
        if (allowed) ProductEditService.EnsureUnitChangeAllowed(previous, next, stock, history);
        else Assert.Equal(409, Assert.Throws<LoiNghiepVu>(() => ProductEditService.EnsureUnitChangeAllowed(previous, next, stock, history)).MaHttp);
    }

    [Fact]
    public void ValidatesRequiredBoundariesAndTrimming()
    {
        Assert.Throws<ArgumentNullException>(() => ProductEditService.Normalize(null!));
        var valid = Valid() with { TenSanPham = " Product ", DonViTinh = " Unit ", MoTa = "  " };
        var normalized = ProductEditService.Normalize(valid); Assert.Equal("Product", normalized.TenSanPham); Assert.Equal("Unit", normalized.DonViTinh); Assert.Null(normalized.MoTa);
        Assert.Equal(5, Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(Assert.Throws<LoiNghiepVu>(() => ProductEditService.Normalize(new() { NguongToiThieu = -1 })).ChiTiet).Count);
        Assert.Throws<LoiNghiepVu>(() => ProductEditService.Normalize(Valid() with { TenSanPham = new string('a', 201), DonViTinh = new string('a', 21), MoTa = new string('a', 1001) }));
        Assert.Equal(200, ProductEditService.Normalize(Valid() with { TenSanPham = new string('a', 200), DonViTinh = new string('a', 20), MoTa = new string('a', 1000), NguongToiThieu = 0 }).TenSanPham!.Length);
    }

    internal static ProductEditDto Valid() => new() { TenSanPham = "Product", DanhMucId = 1, DonViTinh = "Hộp", NguongToiThieu = 5, PhienBan = 1 };
}

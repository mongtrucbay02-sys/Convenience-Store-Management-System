using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp03ProductUnitTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(-100L)]
    [InlineData(101L)]
    [InlineData(4294967300L)]
    public void InvalidPriceIsRejected(long? price) => Assert.Equal(422, Assert.Throws<LoiNghiepVu>(() => ProductCreationService.Normalize(Valid() with { GiaBan = price })).MaHttp);

    [Fact]
    public void RequiredFieldsLengthBoundariesAndTrimFollowContract()
    {
        var clean = ProductCreationService.Normalize(Valid() with { Ma = " Code ", TenSanPham = " Product ", DonViTinh = " Unit ", MoTa = " Description " });
        Assert.Equal("Code", clean.Ma); Assert.Equal("Product", clean.TenSanPham); Assert.Equal("Unit", clean.DonViTinh); Assert.Equal("Description", clean.MoTa);
        Assert.Null(ProductCreationService.Normalize(Valid() with { Ma = "  " }).Ma);
        Assert.Equal(5, new ProductCreateDto().NguongToiThieu);
        Assert.Throws<ArgumentNullException>(() => ProductCreationService.Normalize(null!));
        Assert.Throws<LoiNghiepVu>(() => ProductCreationService.Normalize(Valid() with { Ma = new string('a', 21) }));
        Assert.Throws<LoiNghiepVu>(() => ProductCreationService.Normalize(Valid() with { TenSanPham = new string('a', 201) }));
        Assert.Throws<LoiNghiepVu>(() => ProductCreationService.Normalize(Valid() with { DonViTinh = new string('a', 21) }));
        Assert.Throws<LoiNghiepVu>(() => ProductCreationService.Normalize(Valid() with { MoTa = new string('a', 1001) }));
        var errors = Assert.Throws<LoiNghiepVu>(() => ProductCreationService.Normalize(new() { NguongToiThieu = -1 }));
        Assert.Equal(new[] { "danhMucId", "donViTinh", "giaBan", "nguongToiThieu", "tenSanPham" }, Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(errors.ChiTiet).Select(x => x.Truong).Order().ToArray());
        var boundary = ProductCreationService.Normalize(Valid() with { Ma = new string('a', 20), TenSanPham = new string('a', 200), DonViTinh = new string('a', 20), MoTa = new string('a', 1000), GiaBan = 4294967200L, NguongToiThieu = 0 });
        Assert.Equal(4294967200L, boundary.GiaBan);
    }

    internal static ProductCreateDto Valid() => new() { TenSanPham = "Product", DanhMucId = 1, DonViTinh = "Unit", GiaBan = 10000 };
}

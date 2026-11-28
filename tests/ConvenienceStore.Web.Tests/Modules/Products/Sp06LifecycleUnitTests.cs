using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp06LifecycleUnitTests
{
    [Theory]
    [InlineData(ProductLifecycleOperation.Discontinue, "DANG_KINH_DOANH", 0, false, false, true)]
    [InlineData(ProductLifecycleOperation.Discontinue, "DANG_KINH_DOANH", 7, false, false, false)]
    [InlineData(ProductLifecycleOperation.Discontinue, "DANG_KINH_DOANH", 7, true, false, true)]
    [InlineData(ProductLifecycleOperation.Discontinue, "NGUNG_KINH_DOANH", 0, false, false, false)]
    [InlineData(ProductLifecycleOperation.Reactivate, "NGUNG_KINH_DOANH", 7, false, true, true)]
    [InlineData(ProductLifecycleOperation.Reactivate, "DANG_KINH_DOANH", 0, false, false, false)]
    [InlineData(ProductLifecycleOperation.Delete, "DANG_KINH_DOANH", 0, false, false, true)]
    [InlineData(ProductLifecycleOperation.Delete, "DANG_KINH_DOANH", 0, true, true, false)]
    [InlineData(ProductLifecycleOperation.Delete, "DANG_KINH_DOANH", 7, true, false, false)]
    public void StateRulesProtectStockAndHistoricalReferences(ProductLifecycleOperation operation, string state, int quantity, bool stockConfirmed, bool referenced, bool allowed)
    {
        var preview = new ProductLifecyclePreview(1, "Product", state, 1, quantity, 0, 0, referenced, "HOAT_DONG");
        var input = new ProductLifecycleCommand { PhienBan = 1, XacNhan = true, XacNhanTonKho = stockConfirmed };
        if (allowed) ProductLifecycleRules.EnsureOperation(preview, operation, input);
        else Assert.Equal(409, Assert.Throws<LoiNghiepVu>(() => ProductLifecycleRules.EnsureOperation(preview, operation, input)).MaHttp);
    }
    [Fact]
    public void ExplicitConfirmationVersionAndStockIntegrityAreRequired()
    {
        Assert.Throws<ArgumentNullException>(() => ProductLifecycleRules.Validate(null!));
        Assert.Equal(2, Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(Assert.Throws<LoiNghiepVu>(() => ProductLifecycleRules.Validate(new())).ChiTiet).Count);
        Assert.Equal(422, Assert.Throws<LoiNghiepVu>(() => ProductLifecycleRules.Validate(new() { PhienBan = 1 })).MaHttp);
        var preview = new ProductLifecyclePreview(1, "Product", "DANG_KINH_DOANH", 2, 0, 1, 1, true, "HOAT_DONG");
        Assert.Equal(409, Assert.Throws<LoiNghiepVu>(() => ProductLifecycleRules.EnsureOperation(preview, ProductLifecycleOperation.Discontinue, new() { PhienBan = 1, XacNhan = true })).MaHttp);
        Assert.Equal(409, Assert.Throws<LoiNghiepVu>(() => ProductLifecycleRules.EnsureOperation(preview with { CoDuLieuTonKho = false }, ProductLifecycleOperation.Discontinue, new() { PhienBan = 2, XacNhan = true })).MaHttp);
        ProductLifecycleRules.EnsureOperation(preview, ProductLifecycleOperation.Discontinue, new() { PhienBan = 2, XacNhan = true }); // E2 warns, not an invented additional veto.
    }
}

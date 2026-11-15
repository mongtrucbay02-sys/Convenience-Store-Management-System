using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp02ProductUnitTests
{
    [Theory]
    [InlineData("AD", false, 2)]
    [InlineData("NK", false, 1)]
    [InlineData("TN", true, 1)]
    public async Task DetailServiceReadsInventoryAndOnlyAdminHistory(string role, bool activeOnly, int priceCalls)
    {
        var repository = new DetailStub(); var service = new ProductDetailService(repository, new StockStub());
        var result = await service.FindAsync(1, role);
        Assert.Equal(activeOnly, repository.ActiveOnly); Assert.Equal(priceCalls, repository.PriceCalls);
        Assert.Equal(7, result.TonKho); Assert.Equal(5, result.NguongToiThieu);
        Assert.Equal(role == "AD" ? 8000m : null, result.GiaVon);
        Assert.Equal(role == "AD", result.LichSuGia is not null);
        Assert.Equal(role == "AD" ? 3u : null, result.GiaDaLenLich.Items[0].NguoiTaoId);
    }

    [Fact]
    public async Task GuardMissingResourceCancellationAndHistoryAuthorization()
    {
        var repository = new DetailStub(); var service = new ProductDetailService(repository, new StockStub());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.FindAsync(1, "unknown"));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.FindAsync(1, "AD", cancel.Token));
        Assert.Equal(403, (await Assert.ThrowsAsync<LoiNghiepVu>(() => service.PricesAsync(1, new() { DaLenLich = false }, "NK"))).MaHttp);
        Assert.Equal(0, repository.PriceCalls);
        repository.Missing = true;
        Assert.Equal(404, (await Assert.ThrowsAsync<LoiNghiepVu>(() => service.FindAsync(1, "AD"))).MaHttp);
        Assert.Equal(404, (await Assert.ThrowsAsync<LoiNghiepVu>(() => service.PricesAsync(1, new(), "AD"))).MaHttp);
        Assert.Equal(422, Assert.Throws<LoiNghiepVu>(() => ProductDetailService.Normalize(new() { Page = 0 })).MaHttp);
        Assert.Equal(100, ProductDetailService.Normalize(new() { Size = 500 }).Size);
    }

    [Theory]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("data:text/html,evil", null)]
    [InlineData("//evil.example/image", null)]
    [InlineData("/uploads/products/test.png", "/uploads/products/test.png")]
    [InlineData("https://example.com/a.png", "https://example.com/a.png")]
    public void ImageSchemeValidation(string input, string? expected) => Assert.Equal(expected, ProductDetailService.SafeImageUrl(input));

    private sealed class DetailStub : IProductDetailRepository
    {
        public bool ActiveOnly; public int PriceCalls; public bool Missing;
        public Task<ProductDetailsDto?> FindAsync(uint id, bool activeOnly, CancellationToken ct = default)
        {
            ActiveOnly = activeOnly;
            return Task.FromResult<ProductDetailsDto?>(Missing ? null : new() { Id = id, Ma = "P", TenSanPham = "Product", DanhMuc = new(1, "Category"), DonViTinh = "Unit", TrangThai = "DANG_KINH_DOANH" });
        }
        public Task<PagedResultDto<ProductPriceDto>> PricesAsync(uint id, ProductPricesQuery query, CancellationToken ct = default)
        { PriceCalls++; return Task.FromResult(new PagedResultDto<ProductPriceDto>([new(1, 10000, DateTimeOffset.UtcNow, 3, DateTimeOffset.UtcNow, null, null)], 1, 1, 20)); }
    }

    private sealed class StockStub : IDichVuKho
    {
        public Task<IReadOnlyList<TonKhoHienTai>> LayTonKhoAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TonKhoHienTai>>([new(1, 7, 8000, 5)]);
        public Task<KetQuaBienDong> GhiNhanBienDongAsync(YeuCauBienDong request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task KhoiTaoTonKhoAsync(int id, int threshold = 5, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> DemSanPhamDuoiNguongAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}

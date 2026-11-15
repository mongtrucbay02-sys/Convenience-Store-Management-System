using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Inventory.Models;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp01ProductUnitTests
{
    [Theory]
    [InlineData("AD", false, true)]
    [InlineData("NK", false, false)]
    [InlineData("TN", true, false)]
    public async Task ServiceBatchesInventoryAndRestrictsCost(string role, bool activeOnly, bool cost)
    {
        var repository = new FakeProducts(); var inventory = new FakeInventory();
        var service = new ProductReadService(repository, inventory);
        using var cancellation = new CancellationTokenSource();
        var page = await service.ListAsync(new() { Size = 1000 }, role, cancellation.Token);
        Assert.Equal(activeOnly, repository.ActiveOnly); Assert.Equal(100, repository.Query!.Size);
        Assert.Equal(cancellation.Token, repository.Token); Assert.Equal(cancellation.Token, inventory.Token);
        Assert.Equal(1, inventory.Calls); Assert.Equal(new[] { 1, 2 }, inventory.Ids);
        Assert.Equal(7, page.Items[0].TonKho); Assert.Equal(0, page.Items[1].TonKho);
        Assert.Equal(cost ? 8000m : null, page.Items[0].GiaVon); Assert.Null(page.Items[1].GiaVon);
    }

    [Fact]
    public async Task ServiceRejectsUnknownRoleCancellationAndPropagatesRepositoryFailure()
    {
        var repository = new FakeProducts(); var inventory = new FakeInventory(); var service = new ProductReadService(repository, inventory);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ListAsync(new(), "fake-admin"));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ListAsync(new(), "AD", cancel.Token));
        Assert.Null(repository.Query); Assert.Equal(0, inventory.Calls);
        repository.Failure = new InvalidOperationException("boundary failure");
        Assert.Same(repository.Failure, await Assert.ThrowsAsync<InvalidOperationException>(() => service.ListAsync(new(), "AD")));
        Assert.Equal(0, inventory.Calls);
    }

    private sealed class FakeProducts : IProductReadRepository
    {
        public bool ActiveOnly; public ProductListQuery? Query; public CancellationToken Token; public Exception? Failure;
        public Task<PagedResultDto<ProductListItemDto>> ListAsync(ProductListQuery query, bool activeOnly, CancellationToken ct = default)
        {
            Query = query; ActiveOnly = activeOnly; Token = ct;
            if (Failure is not null) return Task.FromException<PagedResultDto<ProductListItemDto>>(Failure);
            return Task.FromResult(new PagedResultDto<ProductListItemDto>(Enumerable.Range(1, 2).Select(id => new ProductListItemDto {
                Id = (uint)id, Ma = "P" + id, TenSanPham = "Product", DanhMuc = new(1, "Category"), DonViTinh = "Unit", TrangThai = "DANG_KINH_DOANH"
            }).ToArray(), 2, 1, query.Size));
        }
    }

    private sealed class FakeInventory : IDichVuKho
    {
        public int Calls; public int[] Ids = []; public CancellationToken Token;
        public Task<IReadOnlyList<TonKhoHienTai>> LayTonKhoAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        { Calls++; Ids = ids.ToArray(); Token = ct; return Task.FromResult<IReadOnlyList<TonKhoHienTai>>([new(1, 7, 8000, 5)]); }
        public Task<KetQuaBienDong> GhiNhanBienDongAsync(YeuCauBienDong input, CancellationToken ct = default) => throw new NotSupportedException();
        public Task KhoiTaoTonKhoAsync(int id, int threshold = 5, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> DemSanPhamDuoiNguongAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
    [Theory]
    [InlineData(0, 20, null, null)]
    [InlineData(1, 0, null, null)]
    [InlineData(int.MaxValue, 100, null, null)]
    [InlineData(1, 20, -1L, null)]
    [InlineData(1, 20, 200L, 100L)]
    public void RejectsInvalidBounds(int page, int size, long? from, long? to)
    {
        Assert.Equal(422, Assert.Throws<LoiNghiepVu>(() => ProductReadService.Normalize(new() { Page = page, Size = size, GiaBanTu = from, GiaBanDen = to })).MaHttp);
    }

    [Theory]
    [InlineData("giaVon")]
    [InlineData("tenSanPham,,ma")]
    [InlineData("id")] // ID is a server-owned final tie-breaker.
    [InlineData("-ma,ma")]
    public void RejectsUnsafeOrAmbiguousSort(string sort) => Assert.Throws<LoiNghiepVu>(() => ProductReadService.Normalize(new() { Sort = sort }));

    [Fact]
    public void NormalizesBoundedQueryAndValidatesEveryField()
    {
        var q = ProductReadService.Normalize(new() { Size = 101, Keyword = "  Sữa  ", Sort = "-giaBan,tenSanPham" });
        Assert.Equal(100, q.Size); Assert.Equal("Sữa", q.Keyword);
        Assert.Throws<ArgumentNullException>(() => ProductReadService.Normalize(null!));
        Assert.Throws<LoiNghiepVu>(() => ProductReadService.Normalize(new() { DanhMucId = 0 }));
        Assert.Throws<LoiNghiepVu>(() => ProductReadService.Normalize(new() { TrangThai = "INVALID" }));
        Assert.Throws<LoiNghiepVu>(() => ProductReadService.Normalize(new() { Keyword = new string('x', 201) }));
        var errors = Assert.Throws<LoiNghiepVu>(() => ProductReadService.Normalize(new() { Page = 0, Size = 0, GiaBanTu = -1 }));
        Assert.True(Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(errors.ChiTiet).Count >= 3);
    }
}

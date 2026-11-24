using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp08CategoryPostgreSqlTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    [Fact]
    public async Task PersistDuplicateAndConcurrentUpdatesPreserveRuntimeVersion()
    {
        await using var db = database.TaoDbContext(); var service = new CategoryMutationService(new EfCategoryMutationRepository(db));
        var created = await service.CreateAsync(new("SP08-Sua", null, true)); Assert.Equal(CategoryMutationStatus.Success, created.Status);
        var row = created.Category!; Assert.Equal(1L, row.PhienBan); Assert.True(row.KhongDoiTra);
        Assert.Equal(CategoryMutationStatus.Duplicate, (await service.CreateAsync(new("sp08-sua", null, false))).Status);
        async Task<CategoryMutationResult> Write(string name)
        { await using var other = database.TaoDbContext(); return await new CategoryMutationService(new EfCategoryMutationRepository(other)).UpdateAsync(row.Id, new(name, null, false, row.PhienBan)); }
        var results = await Task.WhenAll(Write("SP08 winner A"), Write("SP08 winner B"));
        Assert.Single(results, x => x.Status == CategoryMutationStatus.Success); Assert.Single(results, x => x.Status == CategoryMutationStatus.Conflict);
        await using var read = database.TaoDbContext(); var saved = await read.DanhMucs.FindAsync(row.Id); Assert.Equal(2L, saved!.PhienBan);
        Assert.Equal(CategoryMutationStatus.NotFound, (await service.UpdateAsync(4294967295L, new("Missing", null, false, 1))).Status);
        using var ct = new CancellationTokenSource(); ct.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.FindAsync(row.Id, ct.Token));
    }
}

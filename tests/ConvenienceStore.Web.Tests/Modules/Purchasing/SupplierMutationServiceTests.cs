using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using ConvenienceStore.Web.Modules.Purchasing.Services;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Purchasing;

public sealed class SupplierMutationServiceTests
{
    private sealed class CodeAllocator : ISupplierCodeAllocator
    {
        public int Calls;
        public Task<string> AllocateAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult("TEST001");
        }
    }
    private sealed class Repository : ISupplierMutationRepository
    {
        public string? Code;
        public SupplierMutationDto? Created;
        public SupplierUpdateDto? Updated;
        public Task<SupplierMutationResult> CreateAsync(string code, SupplierMutationDto input,
            CancellationToken cancellationToken = default)
        {
            Code = code;
            Created = input;
            return Task.FromResult(new SupplierMutationResult(SupplierMutationStatus.Success));
        }
        public Task<SupplierMutationResult> UpdateAsync(uint id, SupplierUpdateDto input,
            CancellationToken cancellationToken = default)
        {
            Updated = input;
            return Task.FromResult(new SupplierMutationResult(SupplierMutationStatus.Success));
        }
    }

    private static SupplierMutationDto Valid => new("  Supplier  ", " Contact ",
        " 0900000000 ", " person@example.com ", " Address ", null, null);

    [Fact]
    public async Task Create_ValidatesBeforeAllocatingCode()
    {
        var repo = new Repository();
        var codes = new CodeAllocator();
        var service = new SupplierMutationService(repo, codes);
        Assert.Equal(SupplierMutationStatus.Invalid,
            (await service.CreateAsync(Valid with { Ten = " " })).Status);
        Assert.Equal(SupplierMutationStatus.Invalid,
            (await service.CreateAsync(Valid with { SoDienThoai = "abc" })).Status);
        Assert.Equal(SupplierMutationStatus.Invalid,
            (await service.CreateAsync(Valid with { Email = "bad" })).Status);
        Assert.Equal(0, codes.Calls);
        Assert.Null(repo.Created);
    }

    [Fact]
    public async Task Create_UsesCodeContractAndNormalizes()
    {
        var repo = new Repository();
        var codes = new CodeAllocator();
        var result = await new SupplierMutationService(repo, codes).CreateAsync(Valid);
        Assert.Equal(SupplierMutationStatus.Success, result.Status);
        Assert.Equal("TEST001", repo.Code);
        Assert.Equal("Supplier", repo.Created?.Ten);
        Assert.Equal("0900000000", repo.Created?.SoDienThoai);
        Assert.Equal(1, codes.Calls);
    }

    [Fact]
    public async Task Update_RequiresVersionAndDoesNotAllocateCode()
    {
        var repo = new Repository();
        var codes = new CodeAllocator();
        var service = new SupplierMutationService(repo, codes);
        var update = new SupplierUpdateDto("Supplier", null, "0900000000",
            null, null, null, null, 0);
        Assert.Equal(SupplierMutationStatus.Invalid,
            (await service.UpdateAsync(1, update)).Status);
        Assert.Null(repo.Updated);
        Assert.Equal(SupplierMutationStatus.Success,
            (await service.UpdateAsync(1, update with { PhienBan = 2 })).Status);
        Assert.Equal((uint)2, repo.Updated?.PhienBan);
        Assert.Equal(0, codes.Calls);
    }
}

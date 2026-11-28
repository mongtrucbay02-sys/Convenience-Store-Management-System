using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp09CategoryUnitTests
{
    // Port PR #202's boundary tests; no old context/entities/test database target.
    private sealed class Repository : ICategoryRemovalRepository
    {
        public long LastId, LastVersion; public int Calls; public CancellationToken Token; public Exception? Failure;
        public Task<CategoryRemovalPreviewDto?> PreviewAsync(long id, CancellationToken ct = default) => Task.FromResult<CategoryRemovalPreviewDto?>(null);
        public Task<CategoryRemovalResult> DeleteAsync(long id, long version, CancellationToken ct = default) => Complete(id, version, ct, CategoryRemovalStatus.Referenced);
        public Task<CategoryRemovalResult> HideAsync(long id, long version, CancellationToken ct = default) => Complete(id, version, ct, CategoryRemovalStatus.Success);
        private Task<CategoryRemovalResult> Complete(long id, long version, CancellationToken ct, CategoryRemovalStatus status)
        { Calls++; LastId = id; LastVersion = version; Token = ct; return Failure is null ? Task.FromResult(new CategoryRemovalResult(status)) : Task.FromException<CategoryRemovalResult>(Failure); }
    }
    [Fact]
    public async Task DeleteRequiresIdAndVersion()
    {
        var repo = new Repository(); var service = new CategoryRemovalService(repo);
        Assert.Equal(CategoryRemovalStatus.Invalid, (await service.DeleteAsync(0, 1)).Status);
        Assert.Equal(CategoryRemovalStatus.Invalid, (await service.DeleteAsync(1, 0)).Status); Assert.Equal(0, repo.Calls);
    }
    [Fact]
    public async Task DeletePropagatesReferenceBlock()
    {
        var repo = new Repository(); var result = await new CategoryRemovalService(repo).DeleteAsync(9, 2);
        Assert.Equal(CategoryRemovalStatus.Referenced, result.Status); Assert.Equal(9L, repo.LastId); Assert.Equal(2L, repo.LastVersion);
    }
    [Fact]
    public async Task HideRequiresVersionAndDelegates()
    {
        var repo = new Repository(); var service = new CategoryRemovalService(repo);
        Assert.Equal(CategoryRemovalStatus.Invalid, (await service.HideAsync(1, 0)).Status); Assert.Equal(0, repo.Calls);
        Assert.Equal(CategoryRemovalStatus.Success, (await service.HideAsync(1, 1)).Status); Assert.Equal(1, repo.Calls);
    }
    [Fact]
    public async Task HttpCommandRequiresExplicitConfirmation()
    {
        var repo = new Repository(); var service = new CategoryRemovalService(repo);
        Assert.Throws<ArgumentNullException>(() => CategoryRemovalService.ValidateCommand(null!));
        Assert.Equal(2, Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(Assert.Throws<LoiNghiepVu>(() => CategoryRemovalService.ValidateCommand(new())).ChiTiet).Count);
        await Assert.ThrowsAsync<LoiNghiepVu>(() => service.DeleteAsync(1, new CategoryRemovalCommand { PhienBan = 1 })); Assert.Equal(0, repo.Calls);
        Assert.Equal(CategoryRemovalStatus.Success, (await service.HideAsync(1, new CategoryRemovalCommand { PhienBan = 1, XacNhan = true })).Status);
    }
    [Fact]
    public async Task CancellationForwardingAndBoundaryFailuresArePreserved()
    {
        var repo = new Repository(); var service = new CategoryRemovalService(repo); using var token = new CancellationTokenSource();
        await service.HideAsync(1, 1, token.Token); Assert.Equal(token.Token, repo.Token);
        token.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.HideAsync(1, 1, token.Token)); Assert.Equal(1, repo.Calls);
        repo.Failure = new InvalidOperationException("boundary failure");
        Assert.Same(repo.Failure, await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(1, 1)));
    }
}

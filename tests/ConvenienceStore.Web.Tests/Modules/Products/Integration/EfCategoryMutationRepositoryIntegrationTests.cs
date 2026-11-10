using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Products.Integration;

public sealed class EfCategoryMutationRepositoryIntegrationTests
{
    private static ApplicationDbContext CreateContext()
    {
        var connection = Environment.GetEnvironmentVariable("SP08_TEST_CONNECTION")
            ?? throw new InvalidOperationException("SP08_TEST_CONNECTION is required for isolated local MySQL.");
        var parts = new DbConnectionStringBuilder { ConnectionString = connection };
        if (parts["Server"]?.ToString() != "127.0.0.1" ||
            parts["Port"]?.ToString() != "3307" ||
            parts["Database"]?.ToString() != "sp08_repository_tests" ||
            parts["User"]?.ToString() != "sp08_test")
            throw new InvalidOperationException("Unsafe SP-08 test database target.");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(connection).Options);
    }

    [Fact]
    public async Task CreateUpdateDuplicateAndStaleVersion_PreserveStoredData()
    {
        await using var db = CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var repo = new EfCategoryMutationRepository(db);
            var name = "sp08_" + Guid.NewGuid().ToString("N");
            var created = await repo.CreateAsync(new(name, "before", true));
            Assert.Equal(CategoryMutationStatus.Success, created.Status);
            Assert.NotNull(created.Category);
            Assert.True(created.Category.KhongDoiTra);

            var duplicate = await repo.CreateAsync(new(name, null, false));
            Assert.Equal(CategoryMutationStatus.Duplicate, duplicate.Status);

            var updated = await repo.UpdateAsync(created.Category.Id,
                new(name, "after", false, created.Category.PhienBan));
            Assert.Equal(CategoryMutationStatus.Success, updated.Status);
            Assert.Equal((uint)2, updated.Category?.PhienBan);

            var stale = await repo.UpdateAsync(created.Category.Id,
                new(name, "wrong", true, created.Category.PhienBan));
            Assert.Equal(CategoryMutationStatus.Conflict, stale.Status);

            db.ChangeTracker.Clear();
            var stored = await db.DanhMucs.AsNoTracking()
                .SingleAsync(x => x.Id == created.Category.Id);
            Assert.Equal("after", stored.MoTa);
            Assert.False(stored.KhongDoiTra);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}

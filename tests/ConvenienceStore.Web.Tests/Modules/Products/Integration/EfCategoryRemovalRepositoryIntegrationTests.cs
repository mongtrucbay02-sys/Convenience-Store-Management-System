using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Entities;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Products.Integration;

public sealed class EfCategoryRemovalRepositoryIntegrationTests
{
    private static ApplicationDbContext CreateContext()
    {
        var connection = Environment.GetEnvironmentVariable("SP09_TEST_CONNECTION")
            ?? throw new InvalidOperationException("SP09_TEST_CONNECTION is required for isolated local MySQL.");
        var parts = new DbConnectionStringBuilder { ConnectionString = connection };
        if (parts["Server"]?.ToString() != "127.0.0.1" ||
            parts["Port"]?.ToString() != "3307" ||
            parts["Database"]?.ToString() != "sp09_repository_tests" ||
            parts["User"]?.ToString() != "sp09_test")
            throw new InvalidOperationException("Unsafe SP-09 test database target.");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(connection).Options);
    }

    [Fact]
    public async Task HideThenDeleteEmptyCategory_PreservesOtherCategory()
    {
        await using var db = CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var suffix = Guid.NewGuid().ToString("N");
            var target = new DanhMuc { Ten = "sp09_target_" + suffix };
            var other = new DanhMuc { Ten = "sp09_other_" + suffix };
            db.DanhMucs.AddRange(target, other);
            await db.SaveChangesAsync();
            var repo = new EfCategoryRemovalRepository(db);
            Assert.Equal(CategoryRemovalStatus.Success,
                (await repo.HideAsync(target.Id, target.PhienBan)).Status);
            Assert.Equal("DA_AN", target.TrangThai);
            Assert.Equal(CategoryRemovalStatus.Conflict,
                (await repo.DeleteAsync(target.Id, 1)).Status);
            Assert.Equal(CategoryRemovalStatus.Success,
                (await repo.DeleteAsync(target.Id, target.PhienBan)).Status);
            db.ChangeTracker.Clear();
            Assert.False(await db.DanhMucs.AnyAsync(x => x.Id == target.Id));
            Assert.True(await db.DanhMucs.AnyAsync(x => x.Id == other.Id));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}

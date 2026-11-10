using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using ConvenienceStore.Web.Modules.Purchasing.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Purchasing.Integration;

public sealed class EfSupplierMutationRepositoryIntegrationTests
{
    private static ApplicationDbContext CreateContext()
    {
        var connection = Environment.GetEnvironmentVariable("NH02_TEST_CONNECTION")
            ?? throw new InvalidOperationException("NH02_TEST_CONNECTION is required for isolated local MySQL.");
        var parts = new DbConnectionStringBuilder { ConnectionString = connection };
        if (parts["Server"]?.ToString() != "127.0.0.1" ||
            parts["Port"]?.ToString() != "3307" ||
            parts["Database"]?.ToString() != "nh02_repository_tests" ||
            parts["User"]?.ToString() != "nh02_test")
            throw new InvalidOperationException("Unsafe NH-02 test database target.");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(connection).Options);
    }

    [Fact]
    public async Task CreateDuplicateAndUpdate_PreserveSupplierIdentity()
    {
        await using var db = CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var repo = new EfSupplierMutationRepository(db);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var input = new SupplierMutationDto("Supplier " + suffix, "Contact",
                "0900000000", "contact@example.com", "Address", null, null);
            var created = await repo.CreateAsync("N" + suffix, input);
            Assert.Equal(SupplierMutationStatus.Success, created.Status);
            Assert.Equal("HOAT_DONG", created.Supplier?.TrangThai);
            Assert.Equal(SupplierMutationStatus.DuplicatePhone,
                (await repo.CreateAsync("X" + suffix, input)).Status);
            var id = created.Supplier!.Id;
            var updated = await repo.UpdateAsync(id, new(input.Ten, "Changed",
                input.SoDienThoai, input.Email, "New address", null, null,
                created.Supplier.PhienBan));
            Assert.Equal(SupplierMutationStatus.Success, updated.Status);
            Assert.Equal("N" + suffix, updated.Supplier?.Ma);
            Assert.Equal(SupplierMutationStatus.Conflict,
                (await repo.UpdateAsync(id, new(input.Ten, null, input.SoDienThoai,
                    null, null, null, null, created.Supplier.PhienBan))).Status);
            db.ChangeTracker.Clear();
            var stored = await db.NhaCungCaps.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("Changed", stored.NguoiLienHe);
            Assert.Equal("New address", stored.DiaChi);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}

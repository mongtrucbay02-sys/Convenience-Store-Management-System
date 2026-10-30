using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using ConvenienceStore.Web.Modules.Purchasing.Entities;
using ConvenienceStore.Web.Modules.Purchasing.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Purchasing.Integration;

public sealed class EfSupplierListRepositoryIntegrationTests
{
    private static ApplicationDbContext CreateContext()
    {
        var connection = Environment.GetEnvironmentVariable("NH01_TEST_CONNECTION")
            ?? throw new InvalidOperationException("NH01_TEST_CONNECTION is required for isolated local MySQL.");
        var parts = new DbConnectionStringBuilder { ConnectionString = connection };
        if (parts["Server"]?.ToString() != "127.0.0.1" ||
            parts["Port"]?.ToString() != "3307" ||
            parts["Database"]?.ToString() != "nh01_repository_tests" ||
            parts["User"]?.ToString() != "nh01_test")
            throw new InvalidOperationException("Unsafe NH-01 test database target.");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(connection).Options);
    }

    [Fact]
    public async Task SearchStatusAndEmptyResult_UsePersistedSuppliers()
    {
        await using var db = CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var active = new NhaCungCap
            {
                Ma = "A" + suffix, Ten = "Milk " + suffix,
                SoDienThoai = "09" + Random.Shared.NextInt64(100000000, 999999999).ToString(),
                NguoiLienHe = "Contact", DiaChi = "Address"
            };
            var stopped = new NhaCungCap
            {
                Ma = "B" + suffix, Ten = "Bread " + suffix,
                SoDienThoai = "08" + Random.Shared.NextInt64(100000000, 999999999).ToString(),
                TrangThai = "NGUNG_HOP_TAC"
            };
            db.NhaCungCaps.AddRange(active, stopped);
            await db.SaveChangesAsync();
            var repo = new EfSupplierListRepository(db);
            var byName = await repo.ListAsync(new("Milk " + suffix));
            Assert.Single(byName);
            Assert.Equal(active.Ma, byName[0].Ma);
            Assert.Equal("Contact", byName[0].NguoiLienHe);
            Assert.Null(byName[0].LanNhapGanNhat);
            var byPhone = await repo.ListAsync(new(active.SoDienThoai));
            Assert.Single(byPhone);
            var byStatus = await repo.ListAsync(new(Status: "NGUNG_HOP_TAC"));
            Assert.Contains(byStatus, x => x.Id == stopped.Id);
            Assert.DoesNotContain(byStatus, x => x.Id == active.Id);
            Assert.Empty(await repo.ListAsync(new("unmatched_" + suffix)));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}

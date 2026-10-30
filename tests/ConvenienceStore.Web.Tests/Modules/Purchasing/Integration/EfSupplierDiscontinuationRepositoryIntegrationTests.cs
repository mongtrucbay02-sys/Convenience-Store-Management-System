using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Identity.Entities;
using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using ConvenienceStore.Web.Modules.Purchasing.Entities;
using ConvenienceStore.Web.Modules.Purchasing.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Modules.Purchasing.Integration;

public sealed class EfSupplierDiscontinuationRepositoryIntegrationTests
{
    private static ApplicationDbContext CreateContext()
    {
        var connection = Environment.GetEnvironmentVariable("NH03_TEST_CONNECTION")
            ?? throw new InvalidOperationException("NH03_TEST_CONNECTION is required for isolated local MySQL.");
        var parts = new DbConnectionStringBuilder { ConnectionString = connection };
        if (parts["Server"]?.ToString() != "127.0.0.1" ||
            parts["Port"]?.ToString() != "3307" ||
            parts["Database"]?.ToString() != "nh03_repository_tests" ||
            parts["User"]?.ToString() != "nh03_test")
            throw new InvalidOperationException("Unsafe NH-03 test database target.");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(connection).Options);
    }

    [Fact]
    public async Task PendingReceiptBlocksDiscontinuationAndDeletion()
    {
        await using var db = CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var role = new VaiTro { Id = 3, Ma = "NK", Ten = "Test warehouse" };
            db.VaiTros.Add(role);
            await db.SaveChangesAsync();
            var user = new TaiKhoan
            {
                TenDangNhap = "nh03_" + suffix,
                MatKhauBam = "integration-test-only",
                HoTen = "NH03 Tester",
                VaiTroId = role.Id
            };
            db.TaiKhoans.Add(user);
            await db.SaveChangesAsync();
            var supplier = new NhaCungCap
            {
                Ma = "N" + suffix, Ten = "Supplier " + suffix,
                SoDienThoai = "0900000000"
            };
            db.NhaCungCaps.Add(supplier);
            await db.SaveChangesAsync();
            db.PhieuNhaps.Add(new PhieuNhap
            {
                MaPhieu = "P" + suffix,
                NhaCungCapId = supplier.Id,
                NgayNhap = DateOnly.FromDateTime(DateTime.UtcNow),
                NguoiLapId = user.Id,
                TrangThai = "NHAP"
            });
            await db.SaveChangesAsync();

            var repo = new EfSupplierDiscontinuationRepository(db);
            Assert.Equal(SupplierDiscontinuationStatus.PendingReceipts,
                (await repo.DiscontinueAsync(supplier.Id, supplier.PhienBan)).Status);
            Assert.Equal(SupplierDiscontinuationStatus.HasReceiptHistory,
                (await repo.DeleteUnusedAsync(supplier.Id, supplier.PhienBan)).Status);
            db.ChangeTracker.Clear();
            Assert.Equal("HOAT_DONG",
                (await db.NhaCungCaps.SingleAsync(x => x.Id == supplier.Id)).TrangThai);
            Assert.True(await db.PhieuNhaps.AnyAsync(x => x.NhaCungCapId == supplier.Id));
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}

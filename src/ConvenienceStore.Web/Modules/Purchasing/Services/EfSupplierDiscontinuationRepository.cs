using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

public sealed class EfSupplierDiscontinuationRepository(ApplicationDbContext db)
    : ISupplierDiscontinuationRepository
{
    public async Task<SupplierDiscontinuationResult> DiscontinueAsync(uint id, uint version,
        CancellationToken cancellationToken = default)
    {
        var supplier = await db.NhaCungCaps.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (supplier is null) return new(SupplierDiscontinuationStatus.NotFound);
        if (supplier.PhienBan != version) return new(SupplierDiscontinuationStatus.Conflict);
        // NHAP is the schema's draft state; approval of the full pending-state mapping remains open.
        if (await db.PhieuNhaps.AnyAsync(
                x => x.NhaCungCapId == id && x.TrangThai == "NHAP", cancellationToken))
            return new(SupplierDiscontinuationStatus.PendingReceipts);
        if (supplier.TrangThai == "NGUNG_HOP_TAC")
            return new(SupplierDiscontinuationStatus.Success);
        supplier.TrangThai = "NGUNG_HOP_TAC";
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(SupplierDiscontinuationStatus.Conflict);
        }
        return new(SupplierDiscontinuationStatus.Success);
    }

    public async Task<SupplierDiscontinuationResult> DeleteUnusedAsync(uint id, uint version,
        CancellationToken cancellationToken = default)
    {
        var supplier = await db.NhaCungCaps.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (supplier is null) return new(SupplierDiscontinuationStatus.NotFound);
        if (supplier.PhienBan != version) return new(SupplierDiscontinuationStatus.Conflict);
        if (await db.PhieuNhaps.AnyAsync(x => x.NhaCungCapId == id, cancellationToken))
            return new(SupplierDiscontinuationStatus.HasReceiptHistory);
        db.NhaCungCaps.Remove(supplier);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(SupplierDiscontinuationStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            if (await db.PhieuNhaps.AsNoTracking().AnyAsync(
                    x => x.NhaCungCapId == id, cancellationToken))
                return new(SupplierDiscontinuationStatus.HasReceiptHistory);
            throw;
        }
        return new(SupplierDiscontinuationStatus.Success);
    }
}

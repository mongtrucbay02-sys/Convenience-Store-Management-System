using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using ConvenienceStore.Web.Modules.Purchasing.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

public sealed class EfSupplierMutationRepository(ApplicationDbContext db)
    : ISupplierMutationRepository
{
    public async Task<SupplierMutationResult> CreateAsync(string code, SupplierMutationDto input,
        CancellationToken cancellationToken = default)
    {
        if (await db.NhaCungCaps.AnyAsync(x => x.SoDienThoai == input.SoDienThoai, cancellationToken))
            return new(SupplierMutationStatus.DuplicatePhone);
        if (await db.NhaCungCaps.AnyAsync(x => x.Ma == code, cancellationToken))
            return new(SupplierMutationStatus.DuplicateCode);
        var supplier = new NhaCungCap { Ma = code };
        Assign(supplier, input);
        db.NhaCungCaps.Add(supplier);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await db.NhaCungCaps.AsNoTracking().AnyAsync(
                    x => x.SoDienThoai == input.SoDienThoai, cancellationToken))
                return new(SupplierMutationStatus.DuplicatePhone);
            if (await db.NhaCungCaps.AsNoTracking().AnyAsync(
                    x => x.Ma == code, cancellationToken))
                return new(SupplierMutationStatus.DuplicateCode);
            throw;
        }
        return new(SupplierMutationStatus.Success, ToDto(supplier));
    }

    public async Task<SupplierMutationResult> UpdateAsync(uint id, SupplierUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        var supplier = await db.NhaCungCaps.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (supplier is null) return new(SupplierMutationStatus.NotFound);
        if (supplier.PhienBan != input.PhienBan) return new(SupplierMutationStatus.Conflict);
        if (await db.NhaCungCaps.AnyAsync(
                x => x.Id != id && x.SoDienThoai == input.SoDienThoai, cancellationToken))
            return new(SupplierMutationStatus.DuplicatePhone);
        Assign(supplier, new(input.Ten, input.NguoiLienHe, input.SoDienThoai,
            input.Email, input.DiaChi, input.MaSoThue, input.GhiChu));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(SupplierMutationStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            if (await db.NhaCungCaps.AsNoTracking().AnyAsync(
                    x => x.Id != id && x.SoDienThoai == input.SoDienThoai, cancellationToken))
                return new(SupplierMutationStatus.DuplicatePhone);
            throw;
        }
        return new(SupplierMutationStatus.Success, ToDto(supplier));
    }

    private static void Assign(NhaCungCap x, SupplierMutationDto input)
    {
        x.Ten = input.Ten;
        x.NguoiLienHe = input.NguoiLienHe;
        x.SoDienThoai = input.SoDienThoai;
        x.Email = input.Email;
        x.DiaChi = input.DiaChi;
        x.MaSoThue = input.MaSoThue;
        x.GhiChu = input.GhiChu;
    }

    private static SupplierDetailsDto ToDto(NhaCungCap x)
        => new(x.Id, x.Ma, x.Ten, x.NguoiLienHe, x.SoDienThoai,
            x.Email, x.DiaChi, x.MaSoThue, x.GhiChu, x.TrangThai, x.PhienBan);
}

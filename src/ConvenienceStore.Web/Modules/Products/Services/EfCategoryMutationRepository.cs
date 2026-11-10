using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfCategoryMutationRepository(ApplicationDbContext db)
    : ICategoryMutationRepository
{
    public async Task<CategoryMutationResult> CreateAsync(CategoryMutationDto input,
        CancellationToken cancellationToken = default)
    {
        if (await db.DanhMucs.AnyAsync(x => x.Ten == input.Ten, cancellationToken))
            return new(CategoryMutationStatus.Duplicate);

        var category = new DanhMuc
        {
            Ten = input.Ten,
            MoTa = input.MoTa,
            KhongDoiTra = input.KhongDoiTra
        };
        db.DanhMucs.Add(category);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await db.DanhMucs.AsNoTracking().AnyAsync(
                    x => x.Ten == input.Ten, cancellationToken))
                return new(CategoryMutationStatus.Duplicate);
            throw;
        }
        return new(CategoryMutationStatus.Success, ToDto(category));
    }

    public async Task<CategoryMutationResult> UpdateAsync(uint id, CategoryUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        var category = await db.DanhMucs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null) return new(CategoryMutationStatus.NotFound);
        if (category.PhienBan != input.PhienBan) return new(CategoryMutationStatus.Conflict);
        if (await db.DanhMucs.AnyAsync(x => x.Id != id && x.Ten == input.Ten, cancellationToken))
            return new(CategoryMutationStatus.Duplicate);

        category.Ten = input.Ten;
        category.MoTa = input.MoTa;
        category.KhongDoiTra = input.KhongDoiTra;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(CategoryMutationStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            if (await db.DanhMucs.AsNoTracking().AnyAsync(
                    x => x.Id != id && x.Ten == input.Ten, cancellationToken))
                return new(CategoryMutationStatus.Duplicate);
            throw;
        }
        return new(CategoryMutationStatus.Success, ToDto(category));
    }

    private static CategoryDetailsDto ToDto(DanhMuc x)
        => new(x.Id, x.Ten, x.MoTa, x.KhongDoiTra, x.TrangThai, x.PhienBan);
}

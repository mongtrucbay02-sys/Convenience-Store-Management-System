using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfCategoryRemovalRepository(ApplicationDbContext db)
    : ICategoryRemovalRepository
{
    public async Task<CategoryRemovalResult> DeleteAsync(uint id, uint version,
        CancellationToken cancellationToken = default)
    {
        var category = await db.DanhMucs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null) return new(CategoryRemovalStatus.NotFound);
        if (category.PhienBan != version) return new(CategoryRemovalStatus.Conflict);
        if (await db.SanPhams.AnyAsync(x => x.DanhMucId == id, cancellationToken))
            return new(CategoryRemovalStatus.Referenced);

        db.DanhMucs.Remove(category);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(CategoryRemovalStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            if (await db.SanPhams.AsNoTracking().AnyAsync(
                    x => x.DanhMucId == id, cancellationToken))
                return new(CategoryRemovalStatus.Referenced);
            throw;
        }
        return new(CategoryRemovalStatus.Success);
    }

    public async Task<CategoryRemovalResult> HideAsync(uint id, uint version,
        CancellationToken cancellationToken = default)
    {
        var category = await db.DanhMucs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null) return new(CategoryRemovalStatus.NotFound);
        if (category.PhienBan != version) return new(CategoryRemovalStatus.Conflict);
        if (category.TrangThai == "DA_AN") return new(CategoryRemovalStatus.Success);
        category.TrangThai = "DA_AN";
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(CategoryRemovalStatus.Conflict);
        }
        return new(CategoryRemovalStatus.Success);
    }
}

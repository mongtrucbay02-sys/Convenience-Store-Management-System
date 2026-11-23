using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Products.DTOs;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfCategoryRemovalRepository(CuaHangDbContext db) : ICategoryRemovalRepository
{
    public Task<CategoryRemovalPreviewDto?> PreviewAsync(long id, CancellationToken ct = default) => db.DanhMucs.AsNoTracking().Where(x => x.Id == id)
        .Select(x => new CategoryRemovalPreviewDto(x.Id, x.Ten, x.TrangThai, x.PhienBan,
            db.SanPhams.Count(p => p.DanhMucId == x.Id), db.SanPhams.Count(p => p.DanhMucId == x.Id && p.TrangThai == "DANG_KINH_DOANH"),
            db.PhieuKiemKes.Any(p => p.DanhMucId == x.Id))).SingleOrDefaultAsync(ct);
    public Task<CategoryRemovalResult> DeleteAsync(long id, long version, CancellationToken ct = default) => Change(id, version, false, ct);
    public Task<CategoryRemovalResult> HideAsync(long id, long version, CancellationToken ct = default) => Change(id, version, true, ct);

    private async Task<CategoryRemovalResult> Change(long id, long version, bool hide, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        var tracked = db.ChangeTracker.Entries<DanhMuc>().SingleOrDefault(x => x.Entity.Id == id);
        if (tracked is not null && tracked.State != EntityState.Unchanged) throw new InvalidOperationException("Pending category changes must be saved before removal.");
        // The parent lock serializes new FK references with deletion. Existing EF token/hook
        // remain responsible for the version predicate/increment; do not duplicate them.
        var category = (await db.DanhMucs.FromSqlInterpolated($"SELECT * FROM danh_muc WHERE id={id} FOR UPDATE").AsTracking().ToListAsync(ct)).SingleOrDefault();
        if (category is null) return new(CategoryRemovalStatus.NotFound);
        if (tracked is not null) await tracked.ReloadAsync(ct);
        if (category.PhienBan != version) return new(CategoryRemovalStatus.Conflict);
        var preview = (await PreviewAsync(id, ct))!;
        if (!hide && (preview.SoSanPham > 0 || preview.CoPhieuKiemKe)) return new(CategoryRemovalStatus.Referenced, preview);
        if (hide && category.TrangThai == "DA_AN") { await transaction.CommitAsync(ct); return new(CategoryRemovalStatus.Success, preview); } // PR #202's idempotent no-op.
        if (hide) category.TrangThai = "DA_AN"; else db.DanhMucs.Remove(category);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return new(CategoryRemovalStatus.Conflict); }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1451 }) { return new(CategoryRemovalStatus.Referenced, preview); }
        await transaction.CommitAsync(ct);
        return new(CategoryRemovalStatus.Success, preview with { TrangThai = category.TrangThai, PhienBan = category.PhienBan });
    }
}

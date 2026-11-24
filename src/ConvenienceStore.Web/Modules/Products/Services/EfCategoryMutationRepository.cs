using System.Data.Common;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Products.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfCategoryMutationRepository(CuaHangDbContext db):ICategoryMutationRepository
{
    public Task<CategoryDetailsDto?> FindAsync(long id,CancellationToken ct=default) => db.DanhMucs.AsNoTracking()
        .Where(x=>x.Id==id).Select(x=>new CategoryDetailsDto(x.Id,x.Ten,x.MoTa,x.KhongDoiTra,x.TrangThai,x.PhienBan)).SingleOrDefaultAsync(ct);

    public async Task<CategoryMutationResult> CreateAsync(CategoryMutationDto input,CancellationToken cancellationToken=default)
    {
        if(await db.DanhMucs.AnyAsync(x=>x.Ten==input.Ten,cancellationToken)) return new(CategoryMutationStatus.Duplicate);
        var row=new DanhMuc{Ten=input.Ten!,MoTa=input.MoTa,KhongDoiTra=input.KhongDoiTra};db.DanhMucs.Add(row);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch(DbUpdateException ex) when(ex.InnerException is DbException inner && SqlDialect.IsUniqueViolation(inner))
        {db.Entry(row).State=EntityState.Detached;return new(CategoryMutationStatus.Duplicate);}
        return new(CategoryMutationStatus.Success,Map(row));
    }
    public async Task<CategoryMutationResult> UpdateAsync(long id,CategoryUpdateDto input,CancellationToken cancellationToken=default)
    {
        var row=await db.DanhMucs.SingleOrDefaultAsync(x=>x.Id==id,cancellationToken);
        if(row is null)return new(CategoryMutationStatus.NotFound);
        if(row.PhienBan!=input.PhienBan)return new(CategoryMutationStatus.Conflict);
        if(await db.DanhMucs.AnyAsync(x=>x.Id!=id && x.Ten==input.Ten,cancellationToken))return new(CategoryMutationStatus.Duplicate);
        row.Ten=input.Ten!;row.MoTa=input.MoTa;row.KhongDoiTra=input.KhongDoiTra;
        // CuaHangDbContext owns increment and OriginalValues concurrency predicate.
        try {await db.SaveChangesAsync(cancellationToken);}
        catch(DbUpdateConcurrencyException){db.Entry(row).State=EntityState.Detached;return new(CategoryMutationStatus.Conflict);}
        catch(DbUpdateException ex) when(ex.InnerException is DbException inner && SqlDialect.IsUniqueViolation(inner))
        {db.Entry(row).State=EntityState.Detached;return new(CategoryMutationStatus.Duplicate);}
        return new(CategoryMutationStatus.Success,Map(row));
    }
    private static CategoryDetailsDto Map(DanhMuc x)=>new(x.Id,x.Ten,x.MoTa,x.KhongDoiTra,x.TrangThai,x.PhienBan);
}

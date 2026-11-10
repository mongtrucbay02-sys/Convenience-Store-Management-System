
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfCategoryReadRepository
    : ICategoryReadRepository
{
    private readonly ApplicationDbContext _db;

    public EfCategoryReadRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResultDto<CategoryListItemDto>>
        GetCategoriesAsync(
            CategoryListQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page <= 0 || query.Size <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Page and Size must be positive.");

        int size = Math.Min(query.Size, 100);
        long offset = ((long)query.Page - 1) * size;

        if (offset > int.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Pagination offset is too large.");

        // POLICY PENDING:
        // Currently returns both HOAT_DONG and DA_AN.
        var categories = _db.DanhMucs.AsNoTracking();

        int total = await categories.CountAsync(
            cancellationToken);

        // POLICY PENDING:
        // Currently counts all products,
        // including NGUNG_KINH_DOANH.
        var rows = await categories
            .OrderBy(x => x.Id)
            .Skip((int)offset)
            .Take(size)
            .Select(x => new
            {
                x.Id,
                x.Ten,
                x.KhongDoiTra,
                x.TrangThai,
                x.PhienBan,
                SoSanPham = _db.SanPhams.Count(
                    p => p.DanhMucId == x.Id)
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x =>
    new CategoryListItemDto(
        Id: x.Id,
        Ten: x.Ten,
        KhongDoiTra: x.KhongDoiTra,
        SoSanPham: x.SoSanPham,
        TrangThai: x.TrangThai,
        PhienBan: x.PhienBan
        )
        ).ToList();
        return new PagedResultDto<CategoryListItemDto>(
            items,
            total,
            query.Page,
            size
        );
    }
}

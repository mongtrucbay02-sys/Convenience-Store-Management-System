using System.Linq.Expressions;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Products.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfProductReadRepository(CuaHangDbContext db) : IProductReadRepository
{
    public async Task<PagedResultDto<ProductListItemDto>> ListAsync(ProductListQuery query, bool activeOnly, CancellationToken ct = default)
    {
        query = ProductReadService.Normalize(query);
        var rows = db.SanPhams.AsNoTracking().AsQueryable();
        if (activeOnly) rows = rows.Where(x => x.TrangThai == "DANG_KINH_DOANH");
        // Existing utf8mb4_0900_ai_ci columns provide case/accent-insensitive search on MySQL.
        if (!string.IsNullOrEmpty(query.Keyword))
        {
            // LIKE respects the column's accent-insensitive collation. Escape pattern metacharacters
            // so the keyword remains literal; Oracle's Contains/LOCATE translation does not meet BR-SP-01.
            var pattern = "%" + query.Keyword.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_") + "%";
            if(db.IsPostgres())
            {
                pattern=VietnameseSearch.Fold(pattern);
                rows=rows.Where(x=>EF.Functions.Like(VietnameseSearch.Fold(x.Ma),pattern,"!")
                    || EF.Functions.Like(VietnameseSearch.Fold(x.TenSanPham),pattern,"!"));
            }
            else rows = rows.Where(x => EF.Functions.Like(x.Ma, pattern, "!") || EF.Functions.Like(x.TenSanPham, pattern, "!"));
        }
        if (query.DanhMucId.HasValue) rows = rows.Where(x => x.DanhMucId == query.DanhMucId);
        if (!string.IsNullOrEmpty(query.TrangThai)) rows = rows.Where(x => x.TrangThai == query.TrangThai);
        if (query.GiaBanTu.HasValue) rows = rows.Where(x => db.VGiaBanHienHanhs.Any(p => p.SanPhamId == x.Id && p.GiaBan >= query.GiaBanTu));
        if (query.GiaBanDen.HasValue) rows = rows.Where(x => db.VGiaBanHienHanhs.Any(p => p.SanPhamId == x.Id && p.GiaBan <= query.GiaBanDen));
        var total = await rows.CountAsync(ct);
        IOrderedQueryable<SanPham>? ordered = null;
        foreach (var field in query.Sort.Split(','))
        {
            var descending = field.StartsWith('-');
            ordered = field.TrimStart('-') switch {
                "ma" => Order(rows, ordered, x => x.Ma, descending),
                "tenSanPham" => Order(rows, ordered, x => x.TenSanPham, descending),
                "trangThai" => Order(rows, ordered, x => x.TrangThai, descending),
                "ngayTao" => Order(rows, ordered, x => x.NgayTao, descending),
                "giaBan" => Order(rows, ordered, x => db.VGiaBanHienHanhs.Where(p => p.SanPhamId == x.Id).Select(p => (long?)p.GiaBan).FirstOrDefault(), descending),
                _ => throw new InvalidOperationException("Query was not validated.")
            };
        }
        var page = await ordered!.ThenBy(x => x.Id).Skip((int)(((long)query.Page - 1) * query.Size)).Take(query.Size)
            .Select(x => new ProductListItemDto {
                Id = x.Id, Ma = x.Ma, TenSanPham = x.TenSanPham, DanhMucId = x.DanhMucId,
                DanhMuc = new ProductCategoryDto(x.DanhMucId, x.DanhMuc.Ten), DonViTinh = x.DonViTinh,
                GiaBan = db.VGiaBanHienHanhs.Where(p => p.SanPhamId == x.Id).Select(p => (long?)p.GiaBan).FirstOrDefault(),
                TrangThai = x.TrangThai, PhienBan = x.PhienBan
            }).ToArrayAsync(ct);
        return new(page, total, query.Page, query.Size);
    }

    private static IOrderedQueryable<SanPham> Order<T>(IQueryable<SanPham> rows, IOrderedQueryable<SanPham>? ordered, Expression<Func<SanPham, T>> key, bool descending) =>
        ordered is null ? descending ? rows.OrderByDescending(key) : rows.OrderBy(key) : descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
}

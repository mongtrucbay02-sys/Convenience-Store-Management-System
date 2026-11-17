using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfProductDetailRepository(CuaHangDbContext db) : IProductDetailRepository
{
    public Task<ProductDetailsDto?> FindAsync(uint id, bool activeOnly, CancellationToken ct = default) =>
        db.SanPhams.AsNoTracking().Where(x => x.Id == id && (!activeOnly || x.TrangThai == "DANG_KINH_DOANH"))
        .Select(x => new ProductDetailsDto {
            Id = x.Id, Ma = x.Ma, TenSanPham = x.TenSanPham, DanhMucId = x.DanhMucId,
            DanhMuc = new(x.DanhMucId, x.DanhMuc.Ten), DonViTinh = x.DonViTinh,
            GiaBan = db.VGiaBanHienHanhs.Where(p => p.SanPhamId == x.Id).Select(p => (uint?)p.GiaBan).FirstOrDefault(),
            TrangThai = x.TrangThai, PhienBan = x.PhienBan, AnhUrl = x.AnhUrl, MoTa = x.MoTa,
            NgayCapNhat = GioVietNam.ThanhOffset(x.NgayCapNhat)
        }).SingleOrDefaultAsync(ct);

    public async Task<PagedResultDto<ProductPriceDto>> PricesAsync(uint id, ProductPricesQuery query, CancellationToken ct = default)
    {
        query = ProductDetailService.Normalize(query);
        var rows = db.LichSuGia.AsNoTracking().Where(x => x.SanPhamId == id);
        if (query.DaLenLich)
        {
            var now = Convert.ToDateTime(await db.DocMotGiaTriAsync("SELECT NOW()", ct));
            rows = rows.Where(x => x.HuyLuc == null && x.HieuLucTu > now);
        }
        var total = await rows.CountAsync(ct);
        var page = await rows.OrderByDescending(x => x.HieuLucTu).ThenByDescending(x => x.Id)
            .Skip((int)(((long)query.Page - 1) * query.Size)).Take(query.Size).ToArrayAsync(ct);
        return new(page.Select(x => new ProductPriceDto(x.Id, x.GiaBan, GioVietNam.ThanhOffset(x.HieuLucTu), x.NguoiTaoId,
            GioVietNam.ThanhOffset(x.NgayTao), GioVietNam.ThanhOffset(x.HuyLuc), x.NguoiHuyId)).ToArray(), total, query.Page, query.Size);
    }
}

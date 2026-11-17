using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfProductPricingRepository(CuaHangDbContext db) : IProductPricingRepository
{
    public async Task<ProductPriceResult?> FindAsync(uint id, uint priceId, CancellationToken ct = default)
    {
        var row = await db.LichSuGia.AsNoTracking().Where(x => x.Id == priceId && x.SanPhamId == id).Select(x => new { Price = x, Version = x.SanPham.PhienBan }).SingleOrDefaultAsync(ct);
        return row is null ? null : Map(row.Price, row.Version);
    }
    public async Task<ProductPriceResult> CreateAsync(uint id, ProductPriceChangeDto input, uint actorId, CancellationToken ct = default)
    {
        input = ProductPriceRules.Normalize(input);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        var cost = await LockActive(id, input.PhienBan, ct);
        var now = Convert.ToDateTime(await db.DocMotGiaTriAsync("SELECT NOW()", ct));
        var effective = ProductPriceRules.ResolveEffectiveAt(input.HieuLucTu, now);
        ProductPriceRules.EnsureCostConfirmed(input.GiaBan!.Value, cost, input.XacNhanDuoiGiaVon);
        await Bump(id, input.PhienBan, ct);
        var row = new LichSuGium { SanPhamId = id, GiaBan = checked((uint)input.GiaBan.Value), HieuLucTu = effective, NguoiTaoId = actorId };
        db.LichSuGia.Add(row); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Map(row, input.PhienBan + 1);
    }
    public async Task<ProductPriceResult> CancelAsync(uint id, uint priceId, uint version, uint actorId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        await LockActive(id, version, ct);
        var changed = await db.ChayAsync("UPDATE lich_su_gia SET huy_luc=NOW(),nguoi_huy_id=@actor WHERE id=@price AND san_pham_id=@id AND huy_luc IS NULL AND hieu_luc_tu>NOW()", ct,
            ("@actor", actorId), ("@price", priceId), ("@id", id));
        if (changed == 0) {
            if (!await db.LichSuGia.AnyAsync(x => x.Id == priceId && x.SanPhamId == id, ct)) throw LoiNghiepVu.KhongTimThay("Mức giá không còn tồn tại.");
            throw LoiNghiepVu.XungDot(MaLoiChung.DaDuocXuLy, "Chỉ hủy mức giá chưa hủy và chưa đến thời điểm hiệu lực.");
        }
        await Bump(id, version, ct); var result = (await FindAsync(id, priceId, ct))!;
        await transaction.CommitAsync(ct); return result;
    }
    private async Task<decimal> LockActive(uint id, uint version, CancellationToken ct)
    {
        var stock = (await db.TonKhos.FromSqlInterpolated($"SELECT * FROM ton_kho WHERE san_pham_id={id} FOR UPDATE").AsNoTracking().ToListAsync(ct)).SingleOrDefault();
        var product = (await db.SanPhams.FromSqlInterpolated($"SELECT * FROM san_pham WHERE id={id} FOR UPDATE").AsNoTracking().ToListAsync(ct)).SingleOrDefault() ?? throw LoiNghiepVu.KhongTimThay("Sản phẩm không còn tồn tại");
        if (product.PhienBan != version) throw Stale();
        if (product.TrangThai != "DANG_KINH_DOANH") throw LoiNghiepVu.XungDot("SAN_PHAM_NGUNG_KINH_DOANH", "Chỉ đổi giá sản phẩm đang kinh doanh.");
        if (stock is null) throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Thiếu dữ liệu tồn kho. Cần đối soát trước khi đổi giá.");
        return stock.GiaVonBinhQuan;
    }
    private async Task Bump(uint id, uint version, CancellationToken ct)
    { if (await db.ChayAsync("UPDATE san_pham SET phien_ban=phien_ban+1 WHERE id=@id AND phien_ban=@version", ct, ("@id", id), ("@version", version)) != 1) throw Stale(); }
    private static LoiNghiepVu Stale() => LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, ThongBaoLoi.XungDotDuLieu);
    private static ProductPriceResult Map(LichSuGium x, uint version) => new(x.Id, x.SanPhamId, x.GiaBan, GioVietNam.ThanhOffset(x.HieuLucTu), x.NguoiTaoId, GioVietNam.ThanhOffset(x.NgayTao), GioVietNam.ThanhOffset(x.HuyLuc), x.NguoiHuyId, version);
}

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfProductLifecycleRepository(CuaHangDbContext db) : IProductLifecycleRepository
{
    public Task<ProductLifecyclePreview?> PreviewAsync(long id, CancellationToken ct = default) => db.SanPhams.AsNoTracking().Where(x => x.Id == id)
        .Select(x => new ProductLifecyclePreview(x.Id, x.TenSanPham, x.TrangThai, x.PhienBan, x.TonKho == null ? 0 : x.TonKho.SoLuongTon,
            db.CtGioHangs.Count(d => d.SanPhamId == x.Id && d.GioHang.TrangThai == "TAM_GIU"),
            db.CtPhieuNhaps.Count(d => d.SanPhamId == x.Id && d.PhieuNhap.TrangThai == "NHAP"),
            db.TheKhos.Any(d => d.SanPhamId == x.Id) || db.CtHoaDons.Any(d => d.SanPhamId == x.Id) || db.CtPhieuNhaps.Any(d => d.SanPhamId == x.Id)
                || db.CtKiemKes.Any(d => d.SanPhamId == x.Id) || db.CtXuatHuys.Any(d => d.SanPhamId == x.Id) || db.CtGioHangs.Any(d => d.SanPhamId == x.Id) || x.ChuongTrinhs.Any(),
            x.DanhMuc.TrangThai, x.TonKho != null)).SingleOrDefaultAsync(ct);

    public async Task<ProductLifecyclePreview> ExecuteAsync(long id, ProductLifecycleOperation operation, ProductLifecycleCommand input, CancellationToken ct = default)
    {
        ProductLifecycleRules.Validate(input);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        await db.DocMotGiaTriAsync("SELECT san_pham_id FROM ton_kho WHERE san_pham_id=@id FOR UPDATE", ct, ("@id", id));
        await db.DocMotGiaTriAsync("SELECT id FROM san_pham WHERE id=@id FOR UPDATE", ct, ("@id", id));
        var row = await PreviewAsync(id, ct) ?? throw LoiNghiepVu.KhongTimThay("Sản phẩm không còn tồn tại");
        ProductLifecycleRules.EnsureOperation(row, operation, input);
        if (operation == ProductLifecycleOperation.Delete) {
            try {
                // Only zero-stock, never-referenced products reach here. These are owned
                // configuration rows, not immutable sales/purchase/inventory snapshots.
                await db.ChayAsync("DELETE FROM lich_su_gia WHERE san_pham_id=@id", ct, ("@id", id));
                await db.ChayAsync("DELETE FROM ton_kho WHERE san_pham_id=@id", ct, ("@id", id));
                if (await db.ChayAsync("DELETE FROM san_pham WHERE id=@id AND phien_ban=@version", ct, ("@id", id), ("@version", input.PhienBan)) != 1) throw Stale();
            }
            catch (MySqlException ex) when (ex.Number == 1451) { throw LoiNghiepVu.XungDot("SAN_PHAM_DA_THAM_CHIEU", "Sản phẩm được tham chiếu. Hãy ngừng kinh doanh thay vì xóa hẳn."); }
        }
        else {
            var target = operation == ProductLifecycleOperation.Discontinue ? "NGUNG_KINH_DOANH" : "DANG_KINH_DOANH";
            if (await db.ChayAsync("UPDATE san_pham SET trang_thai=@state,phien_ban=phien_ban+1 WHERE id=@id AND phien_ban=@version", ct,
                ("@state", target), ("@id", id), ("@version", input.PhienBan)) != 1) throw Stale();
            row = row with { TrangThai = target, PhienBan = row.PhienBan + 1 };
        }
        await transaction.CommitAsync(ct); return row;
    }
    private static LoiNghiepVu Stale() => LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, ThongBaoLoi.XungDotDuLieu);
}

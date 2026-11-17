using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfProductEditRepository(CuaHangDbContext db) : IProductEditRepository
{
    public async Task UpdateAsync(uint id, ProductEditDto input, string? imageUrl = null, CancellationToken ct = default)
    {
        input = ProductEditService.Normalize(input);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        // Match the inventory writer's stock-before-product lock order. Parent FK lock also
        // prevents a new business reference racing the unit/history check below.
        var stock = (await db.TonKhos.FromSqlInterpolated($"SELECT * FROM ton_kho WHERE san_pham_id={id} FOR UPDATE").AsNoTracking().ToListAsync(ct)).SingleOrDefault();
        var category = await db.DocMotGiaTriAsync("SELECT id FROM danh_muc WHERE id=@id FOR SHARE", ct, ("@id", input.DanhMucId));
        var row = (await db.SanPhams.FromSqlInterpolated($"SELECT * FROM san_pham WHERE id={id} FOR UPDATE").AsNoTracking().ToListAsync(ct)).SingleOrDefault()
            ?? throw LoiNghiepVu.KhongTimThay("Sản phẩm không còn tồn tại");
        if (row.PhienBan != input.PhienBan) throw Stale();
        if (category is null) throw LoiNghiepVu.DuLieuKhongHopLe("danhMucId", "Danh mục không còn tồn tại.");
        if (stock is null) throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Thiếu dữ liệu tồn kho. Cần đối soát trước khi sửa.");
        var history = false;
        if (!string.Equals(row.DonViTinh, input.DonViTinh, StringComparison.Ordinal))
            history = Convert.ToBoolean(await db.DocMotGiaTriAsync("""
                SELECT EXISTS(SELECT 1 FROM the_kho WHERE san_pham_id=@id)
                    OR EXISTS(SELECT 1 FROM ct_hoa_don WHERE san_pham_id=@id)
                    OR EXISTS(SELECT 1 FROM ct_phieu_nhap WHERE san_pham_id=@id)
                    OR EXISTS(SELECT 1 FROM ct_kiem_ke WHERE san_pham_id=@id)
                    OR EXISTS(SELECT 1 FROM ct_xuat_huy WHERE san_pham_id=@id)
                    OR EXISTS(SELECT 1 FROM ct_gio_hang WHERE san_pham_id=@id)
                """, ct, ("@id", id)));
        ProductEditService.EnsureUnitChangeAllowed(row.DonViTinh, input.DonViTinh!, stock.SoLuongTon, history);
        // SanPham.PhienBan is not configured as an EF token. Use an explicit atomic CAS;
        // do not silently alter the consolidated model or migration history.
        var changed = await db.ChayAsync("""
            UPDATE san_pham SET ten_san_pham=@name,danh_muc_id=@category,don_vi_tinh=@unit,mo_ta=@description,
                anh_url=COALESCE(@image,anh_url),phien_ban=phien_ban+1 WHERE id=@id AND phien_ban=@version
            """, ct, ("@name", input.TenSanPham), ("@category", input.DanhMucId), ("@unit", input.DonViTinh),
            ("@description", input.MoTa), ("@image", imageUrl), ("@id", id), ("@version", input.PhienBan));
        if (changed != 1) throw Stale();
        await db.ChayAsync("UPDATE ton_kho SET nguong_toi_thieu=@threshold WHERE san_pham_id=@id", ct, ("@threshold", input.NguongToiThieu), ("@id", id));
        await transaction.CommitAsync(ct);
    }
    private static LoiNghiepVu Stale() => LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Dữ liệu đã được người khác thay đổi, vui lòng tải lại.");
}

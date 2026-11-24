using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class EfProductCreationRepository(CuaHangDbContext db, IDichVuKho inventory) : IProductCreationRepository
{
    public async Task<long> CreateAsync(ProductCreateDto input, long actorId, string? imageUrl = null, CancellationToken ct = default)
    {
        input = ProductCreationService.Normalize(input);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        var category = await db.DocMotGiaTriAsync("SELECT trang_thai FROM danh_muc WHERE id=@id FOR SHARE", ct, ("@id", input.DanhMucId));
        if (!Equals(category, "HOAT_DONG")) throw LoiNghiepVu.DuLieuKhongHopLe("danhMucId", "Chọn danh mục đang hoạt động.");
        var folded=VietnameseSearch.Fold(input.Ma!);
        await db.ChayAsync("SELECT pg_advisory_xact_lock(hashtextextended(@key,0))",ct,("@key","product-code:"+folded));
        if(await db.SanPhams.AnyAsync(p=>VietnameseSearch.Fold(p.Ma)==folded,ct))
            throw LoiNghiepVu.XungDot("TRUNG_MA_SAN_PHAM","Mã sản phẩm đã tồn tại");
        var row = new SanPham { Ma = input.Ma!, TenSanPham = input.TenSanPham!, DanhMucId = input.DanhMucId, DonViTinh = input.DonViTinh!, MoTa = input.MoTa, AnhUrl = imageUrl, NguoiTaoId = actorId };
        db.SanPhams.Add(row);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is DbException inner && SqlDialect.IsUniqueViolation(inner))
        { db.Entry(row).State = EntityState.Detached; throw LoiNghiepVu.XungDot("TRUNG_MA_SAN_PHAM", "Mã sản phẩm đã tồn tại"); }
        await db.ChayAsync("INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id) VALUES(@id,@price,NOW(),@actor)", ct,
            ("@id", row.Id), ("@price", input.GiaBan), ("@actor", actorId));
        await inventory.KhoiTaoTonKhoAsync(row.Id, input.NguongToiThieu, ct);
        await transaction.CommitAsync(ct);
        return row.Id;
    }
}

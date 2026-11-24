using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

public sealed record PromotionUse(long PromotionId, string PromotionName, long InvoiceId, string InvoiceCode,
    DateTime PaidAt, string CashierName, string CustomerName, long Discount, long InvoiceNet);
public sealed record PromotionHistory(long Id, string Code, string Name, IReadOnlyList<PromotionUse> Uses)
{
    public long TotalDiscount => Uses.Sum(x => x.Discount);
}

/// <summary>KM-06 and BC-11 share the paid-invoice usage view. Deletion additionally guards all historical references.</summary>
public sealed class PromotionUsageStore(CuaHangDbContext db)
{
    public Task<List<PromotionUse>> ReadAsync(DateTime? from, DateTime? to, long? promotionId, CancellationToken ct) =>
        db.DocDanhSachAsync("""
            SELECT v.chuong_trinh_id,p.ten,v.hoa_don_id,v.ma_hoa_don,v.thanh_toan_luc,
              a.ho_ten,CASE WHEN k.trang_thai='HOAT_DONG' THEN k.ho_ten
                WHEN k.id IS NOT NULL THEN CONCAT('Khách đã ẩn #',k.id) ELSE 'Khách lẻ' END,
              v.tien_giam,h.tong_thanh_toan
            FROM v_luot_dung_khuyen_mai v JOIN hoa_don h ON h.id=v.hoa_don_id
            JOIN chuong_trinh_khuyen_mai p ON p.id=v.chuong_trinh_id
            JOIN tai_khoan a ON a.id=v.thu_ngan_id LEFT JOIN khach_hang k ON k.id=v.khach_hang_id
            WHERE (@from IS NULL OR v.thanh_toan_luc>=@from) AND (@to IS NULL OR v.thanh_toan_luc<@to)
              AND (@promotion IS NULL OR v.chuong_trinh_id=@promotion)
            ORDER BY v.thanh_toan_luc DESC,v.hoa_don_id DESC,v.chuong_trinh_id
            """, r => new PromotionUse(Convert.ToInt64(r[0]),r.GetString(1),Convert.ToInt64(r[2]),
                r.GetString(3),r.VietnamDateTime(4),r.GetString(5),r.GetString(6),Convert.ToInt64(r[7]),Convert.ToInt64(r[8])),
            ct,("@from",from),("@to",to),("@promotion",promotionId));

    public async Task<PromotionHistory> HistoryAsync(long id, CancellationToken ct)
    {
        var item=await db.DocMotDongAsync("SELECT id,ma,ten FROM chuong_trinh_khuyen_mai WHERE id=@id",
            r=>new { Id=Convert.ToInt64(r[0]),Code=r.GetString(1),Name=r.GetString(2) },ct,("@id",id))
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy chương trình khuyến mãi.");
        return new(item.Id,item.Code,item.Name,await ReadAsync(null,null,id,ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var found=await db.DocMotGiaTriAsync("SELECT id FROM chuong_trinh_khuyen_mai WHERE id=@id FOR UPDATE",ct,("@id",id));
        if(found is null) throw LoiNghiepVu.KhongTimThay("Không tìm thấy chương trình khuyến mãi.");
        // A cancelled invoice is absent from KM-06, but its snapshot must never lose the promotion reference.
        var referenced=Convert.ToInt32(await db.DocMotGiaTriAsync("""
            SELECT EXISTS(SELECT 1 FROM ct_hoa_don WHERE khuyen_mai_id=@id)
              OR EXISTS(SELECT 1 FROM ct_gio_hang WHERE khuyen_mai_id=@id)
            """,ct,("@id",id)));
        if(referenced!=0 || (await ReadAsync(null,null,id,ct)).Count!=0)
            throw new LoiNghiepVu("KHUYEN_MAI_DA_DUOC_DUNG",409,"Chương trình đã được dùng hoặc đang gắn với đơn. Hãy dùng Kết thúc thay cho Xóa.");
        try
        {
            await db.ChayAsync("DELETE FROM khuyen_mai_san_pham WHERE chuong_trinh_id=@id",ct,("@id",id));
            await db.ChayAsync("DELETE FROM chuong_trinh_khuyen_mai WHERE id=@id",ct,("@id",id));
            await tx.CommitAsync(ct);
        }
        catch(DbException e) when(SqlDialect.IsForeignKeyViolation(e))
        {
            throw new LoiNghiepVu("KHUYEN_MAI_DA_DUOC_DUNG",409,"Chương trình vừa phát sinh tham chiếu. Hãy tải lại và dùng Kết thúc.");
        }
    }
}

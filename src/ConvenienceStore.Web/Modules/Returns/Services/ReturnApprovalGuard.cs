using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Returns.Services;

internal static class ReturnApprovalGuard
{
    internal static LoiNghiepVu ExchangeUnavailable() => LoiNghiepVu.XungDot(
        "DT06_CHO_THIET_KE_PHUONG_AN",
        "Tạm chặn đổi hàng ngoại lệ: chưa có thiết kế lưu phương án được duyệt. Không thu tiền hoặc dùng giỏ POS để hoàn tất phiếu này.");

    internal static async Task RequireActorAsync(CuaHangDbContext db, long actor, string role, CancellationToken ct)
    {
        var id = await db.DocMotGiaTriAsync("""
            SELECT t.id FROM tai_khoan t JOIN vai_tro v ON v.id=t.vai_tro_id
            WHERE t.id=@id AND v.ma=@role AND t.trang_thai='HOAT_DONG'
              AND t.bat_buoc_doi_mk=FALSE AND (t.khoa_tam_den IS NULL OR t.khoa_tam_den<=NOW())
            FOR SHARE OF t
            """, ct, ("@id", actor), ("@role", role));
        if (id is null) throw new LoiNghiepVu("KHONG_DU_QUYEN", 403, "Tài khoản thực hiện không còn hợp lệ.");
    }
}

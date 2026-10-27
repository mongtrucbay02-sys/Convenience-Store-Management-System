using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Inventory.Services;

public sealed class LoiDichVuKho : LoiNghiepVu
{
    private LoiDichVuKho(string ma, int maHttp, string thongBao, object? chiTiet, Exception? loiGoc = null)
        : base(ma, maHttp, thongBao, chiTiet, loiGoc)
    {
    }

    public static LoiDichVuKho TonKhongDu(string thongBao, IReadOnlyList<ThieuTon> chiTiet) =>
        new(MaLoiKho.TonKhongDu, StatusCodes.Status409Conflict, thongBao, chiTiet);

    public static LoiDichVuKho KhongHopLe(IReadOnlyList<LoiTruong> chiTiet) =>
        new(MaLoiKho.DuLieuKhongHopLe, StatusCodes.Status422UnprocessableEntity, ThongBaoLoi.DuLieuKhongHopLe, chiTiet);

    public static LoiDichVuKho SanPhamKhongTonTai(IReadOnlyList<int> sanPhamIds) =>
        new(
            MaLoiKho.SanPhamKhongTonTai,
            StatusCodes.Status422UnprocessableEntity,
            "Sản phẩm chưa có bản ghi tồn kho.",
            sanPhamIds.Select(id => new { sanPhamId = id }).ToList());

    public static LoiDichVuKho LoiGhiDuLieu(Exception loiGoc) =>
        new(
            MaLoiKho.LoiGhiDuLieu,
            StatusCodes.Status500InternalServerError,
            "Không ghi được dữ liệu kho, vui lòng thử lại.",
            null,
            loiGoc);
}

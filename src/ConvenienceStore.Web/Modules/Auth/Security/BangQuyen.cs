namespace ConvenienceStore.Web.Modules.Auth.Security;

public static class BangQuyen
{
    public const string Admin = "AD";
    public const string ThuNgan = "TN";
    public const string NhanVienKho = "NK";

    private static readonly Dictionary<string, string[]> QuyenTheoVaiTro = new(StringComparer.Ordinal)
    {
        [Admin] =
        [
            MaQuyen.TaiKhoanCaNhan, MaQuyen.QuanTriTaiKhoan, MaQuyen.SanPhamXem, MaQuyen.SanPhamSua,
            MaQuyen.SanPhamGiaDanhMuc, MaQuyen.TonKhoXem, MaQuyen.TheKhoCanhBaoXem, MaQuyen.NguongTonSua,
            MaQuyen.KiemKeXuatHuyDuyet, MaQuyen.NhaCungCapQuanLy, MaQuyen.NhaCungCapNgung,
            MaQuyen.PhieuNhapXacNhan, MaQuyen.PhieuNhapXem, MaQuyen.NhapHangLichSuNcc,
            MaQuyen.HoaDonXemTatCa, MaQuyen.HoaDonHuy, MaQuyen.BaoCaoXem, MaQuyen.KhachHangQuanLy,
            MaQuyen.KhachHangQuanTri, MaQuyen.KhuyenMaiXem, MaQuyen.KhuyenMaiQuanLy, MaQuyen.DoiTraDuyetCauHinh,
        ],
        [ThuNgan] =
        [
            MaQuyen.TaiKhoanCaNhan, MaQuyen.SanPhamXem, MaQuyen.TonKhoXem, MaQuyen.BanHang,
            MaQuyen.HoaDonXemCuaMinh, MaQuyen.KhachHangQuanLy, MaQuyen.KhuyenMaiXem, MaQuyen.DoiTra,
        ],
        [NhanVienKho] =
        [
            MaQuyen.TaiKhoanCaNhan, MaQuyen.SanPhamXem, MaQuyen.SanPhamSua, MaQuyen.TonKhoXem,
            MaQuyen.TheKhoCanhBaoXem, MaQuyen.KiemKeXuatHuyLap, MaQuyen.NhaCungCapQuanLy,
            MaQuyen.PhieuNhapLap, MaQuyen.PhieuNhapXacNhan, MaQuyen.PhieuNhapXem,
        ],
    };

    public static IReadOnlyList<string> CacVaiTro { get; } = [Admin, ThuNgan, NhanVienKho];

    public static bool LaVaiTroHopLe(string? maVaiTro) =>
        maVaiTro is not null && QuyenTheoVaiTro.ContainsKey(maVaiTro);

    public static IReadOnlyList<string> LayQuyen(string maVaiTro) =>
        QuyenTheoVaiTro.TryGetValue(maVaiTro, out var quyen) ? quyen : [];

    public static string ManHinhChinh(string maVaiTro) => maVaiTro switch
    {
        ThuNgan => "POS",
        NhanVienKho => "KHO",
        _ => "DASHBOARD",
    };
}

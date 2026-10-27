namespace ConvenienceStore.Web.Modules.Auth.Security;

public static class MaQuyen
{
    public const string TaiKhoanCaNhan = "TAI_KHOAN_CA_NHAN";
    public const string QuanTriTaiKhoan = "QUAN_TRI_TAI_KHOAN";
    public const string SanPhamXem = "SAN_PHAM_XEM";
    public const string SanPhamSua = "SAN_PHAM_SUA";
    public const string SanPhamGiaDanhMuc = "SAN_PHAM_GIA_DANH_MUC";
    public const string TonKhoXem = "TON_KHO_XEM";
    public const string TheKhoCanhBaoXem = "THE_KHO_CANH_BAO_XEM";
    public const string NguongTonSua = "NGUONG_TON_SUA";
    public const string KiemKeXuatHuyLap = "KIEM_KE_XUAT_HUY_LAP";
    public const string KiemKeXuatHuyDuyet = "KIEM_KE_XUAT_HUY_DUYET";
    public const string NhaCungCapQuanLy = "NHA_CUNG_CAP_QUAN_LY";
    public const string NhaCungCapNgung = "NHA_CUNG_CAP_NGUNG";
    public const string PhieuNhapLap = "PHIEU_NHAP_LAP";
    public const string PhieuNhapXacNhan = "PHIEU_NHAP_XAC_NHAN";
    public const string PhieuNhapXem = "PHIEU_NHAP_XEM";
    public const string NhapHangLichSuNcc = "NHAP_HANG_LICH_SU_NCC";
    public const string BanHang = "BAN_HANG";
    public const string HoaDonXemTatCa = "HOA_DON_XEM_TAT_CA";
    public const string HoaDonXemCuaMinh = "HOA_DON_XEM_CUA_MINH";
    public const string HoaDonHuy = "HOA_DON_HUY";
    public const string BaoCaoXem = "BAO_CAO_XEM";
    public const string KhachHangQuanLy = "KHACH_HANG_QUAN_LY";
    public const string KhachHangQuanTri = "KHACH_HANG_QUAN_TRI";
    public const string KhuyenMaiXem = "KHUYEN_MAI_XEM";
    public const string KhuyenMaiQuanLy = "KHUYEN_MAI_QUAN_LY";
    public const string DoiTra = "DOI_TRA";
    public const string DoiTraDuyetCauHinh = "DOI_TRA_DUYET_CAU_HINH";

    public static IReadOnlyList<string> TatCa { get; } =
    [
        TaiKhoanCaNhan, QuanTriTaiKhoan, SanPhamXem, SanPhamSua, SanPhamGiaDanhMuc, TonKhoXem,
        TheKhoCanhBaoXem, NguongTonSua, KiemKeXuatHuyLap, KiemKeXuatHuyDuyet, NhaCungCapQuanLy,
        NhaCungCapNgung, PhieuNhapLap, PhieuNhapXacNhan, PhieuNhapXem, NhapHangLichSuNcc, BanHang,
        HoaDonXemTatCa, HoaDonXemCuaMinh, HoaDonHuy, BaoCaoXem, KhachHangQuanLy, KhachHangQuanTri,
        KhuyenMaiXem, KhuyenMaiQuanLy, DoiTra, DoiTraDuyetCauHinh,
    ];
}

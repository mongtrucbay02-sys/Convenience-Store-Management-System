using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Auth;

public sealed record TaiKhoanTest(int Id, string TenDangNhap, string MatKhau);

public sealed class DuLieuTestXacThuc(CsdlMySqlTam csdl)
{
    public const string MatKhauMacDinh = "MatKhau123";

    public static string TenMoi(string tienTo = "tk") => $"{tienTo}.{Guid.NewGuid().ToString("N")[..10]}";

    public static string SoDienThoaiMoi() => "09" + Random.Shared.Next(0, 100_000_000).ToString("D8");

    public async Task<TaiKhoanTest> TaoTaiKhoanAsync(
        string vaiTro, string? matKhau = null, bool batBuocDoiMatKhau = false, string? tenDangNhap = null)
    {
        var ten = tenDangNhap ?? TenMoi(vaiTro.ToLowerInvariant());
        var mk = matKhau ?? MatKhauMacDinh;
        await csdl.ChayAsync(
            """
            INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id, bat_buoc_doi_mk)
            VALUES (@ten, @bam, @hoTen, (SELECT id FROM vai_tro WHERE ma = @vaiTro), @batBuoc)
            """,
            ("@ten", ten), ("@bam", BamMatKhau.Bam(mk)), ("@hoTen", "Người dùng " + ten),
            ("@vaiTro", vaiTro), ("@batBuoc", batBuocDoiMatKhau));
        var id = Convert.ToInt32(await csdl.DocMotGiaTriAsync(
            "SELECT id FROM tai_khoan WHERE ten_dang_nhap = @ten", ("@ten", ten)));
        return new TaiKhoanTest(id, ten, mk);
    }

    public Task KhoaMoiAdminDangHoatDongAsync() => csdl.ChayAsync(
        """
        UPDATE tai_khoan
        SET trang_thai = 'DA_KHOA', ly_do_khoa = 'Khóa để kiểm thử'
        WHERE trang_thai = 'HOAT_DONG' AND vai_tro_id = (SELECT id FROM vai_tro WHERE ma = 'AD')
        """);

    public async Task<object?> DocCotAsync(int taiKhoanId, string cot) =>
        await csdl.DocMotGiaTriAsync($"SELECT {cot} FROM tai_khoan WHERE id = @id", ("@id", taiKhoanId));
}

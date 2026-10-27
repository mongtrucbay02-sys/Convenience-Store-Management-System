using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Auth.Services;

public sealed class DichVuXacThuc(CuaHangDbContext db) : IDichVuXacThuc
{
    private const string SqlTaiKhoanDangNhap =
        """
        SELECT t.id, t.ten_dang_nhap, t.mat_khau_bam, t.ho_ten, t.trang_thai, t.bat_buoc_doi_mk, t.so_lan_sai,
               t.khoa_tam_den, TIMESTAMPDIFF(SECOND, NOW(), t.khoa_tam_den), v.ma, v.ten
        FROM tai_khoan t
        JOIN vai_tro v ON v.id = t.vai_tro_id
        WHERE t.ten_dang_nhap = @ten
        FOR UPDATE OF t
        """;

    private static readonly string SqlPhien =
        $"""
        SELECT p.id, p.tai_khoan_id, p.huy_luc IS NOT NULL,
               p.hoat_dong_cuoi < NOW() - INTERVAL {ThongTinXacThuc.SoPhutPhien} MINUTE,
               p.hoat_dong_cuoi < NOW(),
               t.ten_dang_nhap, t.ho_ten, t.trang_thai, t.bat_buoc_doi_mk, v.ma
        FROM phien_dang_nhap p
        JOIN tai_khoan t ON t.id = p.tai_khoan_id
        JOIN vai_tro v ON v.id = t.vai_tro_id
        WHERE p.token_bam = @bam
        """;

    public async Task<KetQuaDangNhapNoiBo> DangNhapAsync(YeuCauDangNhap yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);
        var tenDangNhap = yeuCau.TenDangNhap?.Trim();
        var matKhau = yeuCau.MatKhau;

        var loi = new List<LoiTruong>();
        if (string.IsNullOrEmpty(tenDangNhap))
            loi.Add(new LoiTruong("tenDangNhap", "Vui lòng nhập đầy đủ thông tin."));
        if (string.IsNullOrEmpty(matKhau))
            loi.Add(new LoiTruong("matKhau", "Vui lòng nhập đầy đủ thông tin."));
        if (loi.Count > 0)
            throw LoiNghiepVu.DuLieuKhongHopLe(loi);

        if (tenDangNhap!.Length > 50)
            loi.Add(new LoiTruong("tenDangNhap", "Tên đăng nhập tối đa 50 ký tự."));
        if (matKhau!.Length > 128)
            loi.Add(new LoiTruong("matKhau", "Mật khẩu tối đa 128 ký tự."));
        if (loi.Count > 0)
            throw LoiNghiepVu.DuLieuKhongHopLe(loi);

        await using var giaoTac = await db.Database.BeginTransactionAsync(ct);
        var taiKhoan = await db.DocMotDongAsync(SqlTaiKhoanDangNhap, DocTaiKhoanDangNhap, ct, ("@ten", tenDangNhap));

        if (taiKhoan is null)
        {
            BamMatKhau.KiemTraGia(matKhau);
            await giaoTac.RollbackAsync(ct);
            throw LoiSaiThongTin();
        }

        if (taiKhoan.SoGiayKhoaTamConLai > 0)
        {
            await giaoTac.RollbackAsync(ct);
            throw LoiKhoaTam(taiKhoan.SoGiayKhoaTamConLai, taiKhoan.KhoaTamDen!.Value);
        }

        var ketQuaMatKhau = BamMatKhau.KiemTra(taiKhoan.MatKhauBam, matKhau);
        if (ketQuaMatKhau == PasswordVerificationResult.Failed)
        {
            var soLanSai = taiKhoan.SoLanSai + 1;
            if (soLanSai >= ThongTinXacThuc.SoLanSaiToiDa)
            {
                await db.ChayAsync(
                    $"""
                    UPDATE tai_khoan
                    SET so_lan_sai = 0, khoa_tam_den = NOW() + INTERVAL {ThongTinXacThuc.SoPhutKhoaTam} MINUTE
                    WHERE id = @id
                    """,
                    ct, ("@id", taiKhoan.Id));
                var khoaTamDen = Convert.ToDateTime(
                    await db.DocMotGiaTriAsync("SELECT khoa_tam_den FROM tai_khoan WHERE id = @id", ct, ("@id", taiKhoan.Id)));
                await giaoTac.CommitAsync(ct);
                throw LoiKhoaTam(ThongTinXacThuc.SoPhutKhoaTam * 60, khoaTamDen);
            }

            await db.ChayAsync(
                "UPDATE tai_khoan SET so_lan_sai = @soLan WHERE id = @id", ct, ("@soLan", soLanSai), ("@id", taiKhoan.Id));
            await giaoTac.CommitAsync(ct);
            throw LoiSaiThongTin();
        }

        if (taiKhoan.TrangThai == "DA_KHOA")
        {
            await giaoTac.RollbackAsync(ct);
            throw new LoiNghiepVu(
                MaLoiAuth.TaiKhoanBiKhoa, StatusCodes.Status423Locked, "Tài khoản đã bị khóa, vui lòng liên hệ quản lý.");
        }

        if (ketQuaMatKhau == PasswordVerificationResult.SuccessRehashNeeded)
            await db.ChayAsync(
                "UPDATE tai_khoan SET mat_khau_bam = @bam WHERE id = @id",
                ct, ("@bam", BamMatKhau.Bam(matKhau)), ("@id", taiKhoan.Id));

        await db.ChayAsync(
            "UPDATE tai_khoan SET so_lan_sai = 0, khoa_tam_den = NULL, lan_dang_nhap_cuoi = NOW() WHERE id = @id",
            ct, ("@id", taiKhoan.Id));

        var token = TokenPhien.TaoToken();
        await db.ChayAsync(
            "INSERT INTO phien_dang_nhap (tai_khoan_id, token_bam) VALUES (@id, @bam)",
            ct, ("@id", taiKhoan.Id), ("@bam", TokenPhien.Bam(token)));
        await giaoTac.CommitAsync(ct);

        var nguoiDung = new NguoiDungDangNhap(
            taiKhoan.Id,
            taiKhoan.TenDangNhap,
            taiKhoan.HoTen,
            new VaiTroTomTat(taiKhoan.MaVaiTro, taiKhoan.TenVaiTro),
            BangQuyen.LayQuyen(taiKhoan.MaVaiTro));
        var ketQua = new KetQuaDangNhap(nguoiDung, taiKhoan.BatBuocDoiMatKhau, BangQuyen.ManHinhChinh(taiKhoan.MaVaiTro));
        return new KetQuaDangNhapNoiBo(ketQua, token);
    }

    public async Task DangXuatAsync(long phienId, CancellationToken ct = default) =>
        await db.ChayAsync(
            "UPDATE phien_dang_nhap SET huy_luc = NOW() WHERE id = @id AND huy_luc IS NULL", ct, ("@id", phienId));

    public async Task DoiMatKhauAsync(int taiKhoanId, YeuCauDoiMatKhau yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);
        await using var giaoTac = await db.Database.BeginTransactionAsync(ct);
        var hienTai = await db.DocMotDongAsync(
            "SELECT mat_khau_bam, bat_buoc_doi_mk FROM tai_khoan WHERE id = @id FOR UPDATE",
            doc => new MatKhauHienTai(doc.Chuoi(0), doc.DungSai(1)),
            ct, ("@id", taiKhoanId))
            ?? throw LoiNghiepVu.KhongTimThay("Không tìm thấy tài khoản.");

        var loi = new List<LoiTruong>();
        if (!hienTai.BatBuocDoiMatKhau)
        {
            if (string.IsNullOrEmpty(yeuCau.MatKhauHienTai))
            {
                loi.Add(new LoiTruong("matKhauHienTai", "Vui lòng nhập mật khẩu hiện tại."));
            }
            else if (!BamMatKhau.Dung(hienTai.MatKhauBam, yeuCau.MatKhauHienTai))
            {
                await giaoTac.RollbackAsync(ct);
                const string thongBao = "Mật khẩu hiện tại không đúng.";
                throw new LoiNghiepVu(
                    MaLoiAuth.SaiMatKhauHienTai,
                    StatusCodes.Status422UnprocessableEntity,
                    thongBao,
                    new[] { new LoiTruong("matKhauHienTai", thongBao) });
            }
        }

        var matKhauMoi = yeuCau.MatKhauMoi;
        if (!QuyTacTaiKhoan.MatKhauHopLe(matKhauMoi))
            loi.Add(new LoiTruong("matKhauMoi", QuyTacTaiKhoan.LoiMatKhau));
        else if (BamMatKhau.Dung(hienTai.MatKhauBam, matKhauMoi!))
            loi.Add(new LoiTruong("matKhauMoi", "Mật khẩu mới phải khác mật khẩu hiện tại."));
        if (yeuCau.NhapLaiMatKhauMoi != matKhauMoi)
            loi.Add(new LoiTruong("nhapLaiMatKhauMoi", "Hai lần nhập mật khẩu mới không khớp."));
        if (loi.Count > 0)
        {
            await giaoTac.RollbackAsync(ct);
            throw LoiNghiepVu.DuLieuKhongHopLe(loi);
        }

        await db.ChayAsync(
            "UPDATE tai_khoan SET mat_khau_bam = @bam, bat_buoc_doi_mk = FALSE, phien_ban = phien_ban + 1 WHERE id = @id",
            ct, ("@bam", BamMatKhau.Bam(matKhauMoi!)), ("@id", taiKhoanId));
        await giaoTac.CommitAsync(ct);
    }

    public async Task<KetQuaXacThucPhien> XacThucPhienAsync(string tokenPhien, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(tokenPhien) || tokenPhien.Length > 128)
            return new KetQuaXacThucPhien(null, false);

        var phien = await db.DocMotDongAsync(SqlPhien, DocPhien, ct, ("@bam", TokenPhien.Bam(tokenPhien)));
        if (phien is null)
            return new KetQuaXacThucPhien(null, false);
        if (phien.DaHuy)
            return new KetQuaXacThucPhien(null, true);

        if (phien.HetHan || phien.TrangThai == "DA_KHOA")
        {
            await db.ChayAsync(
                "UPDATE phien_dang_nhap SET huy_luc = NOW() WHERE id = @id AND huy_luc IS NULL", ct, ("@id", phien.Id));
            return new KetQuaXacThucPhien(null, true);
        }

        if (phien.CanGiaHan)
            await db.ChayAsync(
                "UPDATE phien_dang_nhap SET hoat_dong_cuoi = NOW() WHERE id = @id AND huy_luc IS NULL", ct, ("@id", phien.Id));

        return new KetQuaXacThucPhien(
            new NguoiDungPhien(phien.Id, phien.TaiKhoanId, phien.TenDangNhap, phien.HoTen, phien.MaVaiTro, phien.BatBuocDoiMatKhau),
            false);
    }

    private static LoiNghiepVu LoiSaiThongTin() =>
        new(MaLoiAuth.SaiThongTinDangNhap, StatusCodes.Status401Unauthorized, "Tên đăng nhập hoặc mật khẩu không đúng.");

    private static LoiNghiepVu LoiKhoaTam(long soGiayConLai, DateTime khoaTamDen)
    {
        var soPhut = (int)Math.Max(1, Math.Ceiling(soGiayConLai / 60.0));
        return new LoiNghiepVu(
            MaLoiAuth.TaiKhoanKhoaTam,
            StatusCodes.Status423Locked,
            $"Tài khoản tạm khóa, vui lòng thử lại sau {soPhut} phút.",
            new { conLaiPhut = soPhut, khoaTamDen = GioVietNam.ThanhOffset(khoaTamDen) });
    }

    private static TaiKhoanDangNhap DocTaiKhoanDangNhap(DbDataReader doc) => new(
        doc.SoNguyen(0),
        doc.Chuoi(1),
        doc.Chuoi(2),
        doc.Chuoi(3),
        doc.Chuoi(4),
        doc.DungSai(5),
        doc.SoNguyen(6),
        doc.NgayGioHoacNull(7),
        doc.SoNguyenDaiHoacNull(8) ?? 0,
        doc.Chuoi(9),
        doc.Chuoi(10));

    private static PhienDoc DocPhien(DbDataReader doc) => new(
        doc.SoNguyenDai(0),
        doc.SoNguyen(1),
        doc.DungSai(2),
        doc.DungSai(3),
        doc.DungSai(4),
        doc.Chuoi(5),
        doc.Chuoi(6),
        doc.Chuoi(7),
        doc.DungSai(8),
        doc.Chuoi(9));

    private sealed record TaiKhoanDangNhap(
        int Id,
        string TenDangNhap,
        string MatKhauBam,
        string HoTen,
        string TrangThai,
        bool BatBuocDoiMatKhau,
        int SoLanSai,
        DateTime? KhoaTamDen,
        long SoGiayKhoaTamConLai,
        string MaVaiTro,
        string TenVaiTro);

    private sealed record PhienDoc(
        long Id,
        int TaiKhoanId,
        bool DaHuy,
        bool HetHan,
        bool CanGiaHan,
        string TenDangNhap,
        string HoTen,
        string TrangThai,
        bool BatBuocDoiMatKhau,
        string MaVaiTro);

    private sealed record MatKhauHienTai(string MatKhauBam, bool BatBuocDoiMatKhau);
}

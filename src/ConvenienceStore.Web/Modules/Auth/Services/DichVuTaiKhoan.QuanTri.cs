using System.Data.Common;
using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Auth.Services;

public sealed partial class DichVuTaiKhoan
{
    public async Task<TaiKhoanDto> SuaAsync(long id, YeuCauSuaTaiKhoan yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);
        var hoTen = QuyTacTaiKhoan.ChuanHoa(yeuCau.HoTen);
        var soDienThoai = QuyTacTaiKhoan.ChuanHoa(yeuCau.SoDienThoai);
        var email = QuyTacTaiKhoan.ChuanHoa(yeuCau.Email);

        var loi = new List<LoiTruong>();
        if (!QuyTacTaiKhoan.HoTenHopLe(hoTen))
            loi.Add(new LoiTruong("hoTen", QuyTacTaiKhoan.LoiHoTen));
        if (!QuyTacTaiKhoan.SoDienThoaiHopLe(soDienThoai))
            loi.Add(new LoiTruong("soDienThoai", QuyTacTaiKhoan.LoiSoDienThoai));
        if (!QuyTacTaiKhoan.EmailHopLe(email))
            loi.Add(new LoiTruong("email", QuyTacTaiKhoan.LoiEmail));
        if (yeuCau.PhienBan is null or < 1)
            loi.Add(new LoiTruong("phienBan", "Thiếu phiên bản dữ liệu, vui lòng tải lại tài khoản."));
        if (loi.Count > 0)
            throw LoiNghiepVu.DuLieuKhongHopLe(loi);

        await using var giaoTac = await db.Database.BeginTransactionAsync(ct);
        var hienTai = await KhoaTaiKhoanAsync(id, ct);
        if (hienTai.PhienBan != yeuCau.PhienBan)
            throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, ThongBaoLoi.XungDotDuLieu);
        await KiemTraTrungLienLacAsync(id, soDienThoai, email, ct);

        try
        {
            await db.ChayAsync(
                """
                UPDATE tai_khoan
                SET ho_ten = @hoTen, so_dien_thoai = @sdt, email = @email, phien_ban = phien_ban + 1
                WHERE id = @id
                """,
                ct, ("@hoTen", hoTen), ("@sdt", soDienThoai), ("@email", email), ("@id", id));
        }
        catch (DbException loiCsdl) when (ChuyenLoiTrung(loiCsdl) is { } loiTrung)
        {
            throw loiTrung;
        }

        await giaoTac.CommitAsync(ct);
        return await XemAsync(id, ct);
    }

    public async Task<TaiKhoanDto> KhoaAsync(
        long id, YeuCauKhoaTaiKhoan yeuCau, int nguoiThucHienId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);
        var lyDo = QuyTacTaiKhoan.ChuanHoa(yeuCau.LyDo);
        if (lyDo is null or { Length: > 255 })
            throw LoiNghiepVu.DuLieuKhongHopLe("lyDo", "Vui lòng nhập lý do khóa (tối đa 255 ký tự).");
        if (id == nguoiThucHienId)
            throw LoiNghiepVu.XungDot(MaLoiAuth.KhongThaoTacChinhMinh, "Bạn không thể khóa tài khoản của chính mình.");

        await using var giaoTac = await db.Database.BeginTransactionAsync(ct);
        var adminHoatDong = await KhoaAdminHoatDongAsync(ct);
        var hienTai = await KhoaTaiKhoanAsync(id, ct);

        if (hienTai.TrangThai == "DA_KHOA")
            throw LoiNghiepVu.XungDot(MaLoiChung.DaDuocXuLy, ThongBaoLoi.DaDuocXuLy);
        if (hienTai.MaVaiTro == BangQuyen.Admin && adminHoatDong.Count <= 1)
            throw LoiNghiepVu.XungDot(
                MaLoiAuth.MatAdminCuoiCung,
                "Không thể khóa vì hệ thống phải còn ít nhất một Admin đang hoạt động.");

        await db.ChayAsync(
            "UPDATE tai_khoan SET trang_thai = 'DA_KHOA', ly_do_khoa = @lyDo, phien_ban = phien_ban + 1 WHERE id = @id",
            ct, ("@lyDo", lyDo), ("@id", id));
        await HuyMoiPhienAsync(id, ct);
        await giaoTac.CommitAsync(ct);
        return await XemAsync(id, ct);
    }

    public async Task<TaiKhoanDto> MoKhoaAsync(long id, CancellationToken ct = default)
    {
        await using var giaoTac = await db.Database.BeginTransactionAsync(ct);
        var hienTai = await KhoaTaiKhoanAsync(id, ct);
        if (hienTai.TrangThai == "HOAT_DONG" && !hienTai.DangKhoaTam)
            throw LoiNghiepVu.XungDot(MaLoiChung.DaDuocXuLy, ThongBaoLoi.DaDuocXuLy);

        await db.ChayAsync(
            """
            UPDATE tai_khoan
            SET trang_thai = 'HOAT_DONG', ly_do_khoa = NULL, khoa_tam_den = NULL, so_lan_sai = 0,
                phien_ban = phien_ban + 1
            WHERE id = @id
            """,
            ct, ("@id", id));
        await giaoTac.CommitAsync(ct);
        return await XemAsync(id, ct);
    }

    public async Task<KetQuaDatLaiMatKhau> DatLaiMatKhauAsync(
        long id, YeuCauDatLaiMatKhau? yeuCau, CancellationToken ct = default)
    {
        var matKhauTam = string.IsNullOrEmpty(yeuCau?.MatKhauTam) ? MatKhauTamNgauNhien.Sinh() : yeuCau.MatKhauTam;
        if (!QuyTacTaiKhoan.MatKhauHopLe(matKhauTam))
            throw LoiNghiepVu.DuLieuKhongHopLe("matKhauTam", QuyTacTaiKhoan.LoiMatKhau);

        await using var giaoTac = await db.Database.BeginTransactionAsync(ct);
        var hienTai = await KhoaTaiKhoanAsync(id, ct);
        if (hienTai.TrangThai == "DA_KHOA")
            throw LoiNghiepVu.XungDot(
                MaLoiAuth.TaiKhoanDangKhoa, "Tài khoản đang bị khóa, vui lòng mở khóa trước khi đặt lại mật khẩu.");

        await db.ChayAsync(
            "UPDATE tai_khoan SET mat_khau_bam = @bam, bat_buoc_doi_mk = TRUE, phien_ban = phien_ban + 1 WHERE id = @id",
            ct, ("@bam", BamMatKhau.Bam(matKhauTam)), ("@id", id));
        await HuyMoiPhienAsync(id, ct);
        await giaoTac.CommitAsync(ct);
        return new KetQuaDatLaiMatKhau(id, hienTai.TenDangNhap, matKhauTam);
    }

    private async Task HuyMoiPhienAsync(long taiKhoanId, CancellationToken ct) =>
        await db.ChayAsync(
            "UPDATE phien_dang_nhap SET huy_luc = NOW() WHERE tai_khoan_id = @id AND huy_luc IS NULL",
            ct, ("@id", taiKhoanId));
}

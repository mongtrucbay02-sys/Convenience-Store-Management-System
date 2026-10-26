using System.Data.Common;
using System.Text;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Auth.Services;

public sealed partial class DichVuTaiKhoan(CuaHangDbContext db) : IDichVuTaiKhoan
{
    private const string KhongTimThayTaiKhoan = "Không tìm thấy tài khoản.";

    private const string SqlChonTaiKhoan =
        """
        SELECT t.id, t.ten_dang_nhap, t.ho_ten, t.so_dien_thoai, t.email, v.ma, v.ten, t.trang_thai, t.ly_do_khoa,
               t.khoa_tam_den, t.bat_buoc_doi_mk, t.lan_dang_nhap_cuoi, t.ngay_tao, t.ngay_cap_nhat, t.phien_ban
        FROM tai_khoan t
        JOIN vai_tro v ON v.id = t.vai_tro_id
        """;

    private const string SqlKhoaAdminHoatDong =
        """
        SELECT t.id
        FROM tai_khoan t
        JOIN vai_tro v ON v.id = t.vai_tro_id
        WHERE v.ma = 'AD' AND t.trang_thai = 'HOAT_DONG'
        ORDER BY t.id
        FOR UPDATE OF t
        """;

    private static readonly Dictionary<string, string> CotSapXep = new(StringComparer.Ordinal)
    {
        ["tenDangNhap"] = "t.ten_dang_nhap",
        ["hoTen"] = "t.ho_ten",
        ["lanDangNhapCuoi"] = "t.lan_dang_nhap_cuoi",
        ["ngayTao"] = "t.ngay_tao",
    };

    public async Task<TrangDuLieu<TaiKhoanDto>> XemDanhSachAsync(BoLocTaiKhoan boLoc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(boLoc);
        var loi = new List<LoiTruong>();
        var trang = boLoc.Page ?? 1;
        var coTrang = boLoc.Size ?? 20;
        if (trang < 1)
            loi.Add(new LoiTruong("page", "Số trang phải từ 1 trở lên."));
        if (coTrang < 1)
            loi.Add(new LoiTruong("size", "Số bản ghi mỗi trang phải từ 1 trở lên."));
        coTrang = Math.Min(coTrang, 100);

        var tuKhoa = QuyTacTaiKhoan.ChuanHoa(boLoc.Keyword);
        if (tuKhoa is { Length: > 100 })
            loi.Add(new LoiTruong("keyword", "Từ khóa tối đa 100 ký tự."));
        var vaiTro = QuyTacTaiKhoan.ChuanHoa(boLoc.VaiTro);
        if (vaiTro is not null && !BangQuyen.LaVaiTroHopLe(vaiTro))
            loi.Add(new LoiTruong("vaiTro", QuyTacTaiKhoan.LoiVaiTro));
        var trangThai = QuyTacTaiKhoan.ChuanHoa(boLoc.TrangThai);
        if (trangThai is not null && trangThai is not ("HOAT_DONG" or "DA_KHOA"))
            loi.Add(new LoiTruong("trangThai", "Trạng thái phải là HOAT_DONG hoặc DA_KHOA."));
        var sapXep = TaoSapXep(boLoc.Sort, loi);
        if (loi.Count > 0)
            throw LoiNghiepVu.DuLieuKhongHopLe(loi);

        var dieuKien = new StringBuilder(" WHERE 1 = 1");
        var thamSo = new List<(string, object?)>();
        if (tuKhoa is not null)
        {
            dieuKien.Append(" AND (t.ten_dang_nhap LIKE @tuKhoa OR t.ho_ten LIKE @tuKhoa)");
            thamSo.Add(("@tuKhoa", "%" + ThoatKyTuLike(tuKhoa) + "%"));
        }

        if (vaiTro is not null)
        {
            dieuKien.Append(" AND v.ma = @vaiTro");
            thamSo.Add(("@vaiTro", vaiTro));
        }

        if (trangThai is not null)
        {
            dieuKien.Append(" AND t.trang_thai = @trangThai");
            thamSo.Add(("@trangThai", trangThai));
        }

        var tong = Convert.ToInt32(await db.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM tai_khoan t JOIN vai_tro v ON v.id = t.vai_tro_id" + dieuKien, ct, [.. thamSo]));

        thamSo.Add(("@soDong", coTrang));
        thamSo.Add(("@boQua", (trang - 1) * coTrang));
        var danhSach = await db.DocDanhSachAsync(
            SqlChonTaiKhoan + dieuKien + " ORDER BY " + sapXep + " LIMIT @soDong OFFSET @boQua",
            DocTaiKhoan, ct, [.. thamSo]);

        return new TrangDuLieu<TaiKhoanDto>(danhSach, tong, trang, coTrang);
    }

    public async Task<TaiKhoanDto> XemAsync(long id, CancellationToken ct = default) =>
        await TimAsync(id, ct) ?? throw LoiNghiepVu.KhongTimThay(KhongTimThayTaiKhoan);

    public async Task<TaiKhoanDto> TaoAsync(YeuCauTaoTaiKhoan yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);
        var tenDangNhap = yeuCau.TenDangNhap?.Trim();
        var hoTen = QuyTacTaiKhoan.ChuanHoa(yeuCau.HoTen);
        var soDienThoai = QuyTacTaiKhoan.ChuanHoa(yeuCau.SoDienThoai);
        var email = QuyTacTaiKhoan.ChuanHoa(yeuCau.Email);
        var vaiTro = yeuCau.VaiTro?.Trim();

        var loi = new List<LoiTruong>();
        if (string.IsNullOrEmpty(tenDangNhap) || !QuyTacTaiKhoan.MauTenDangNhap().IsMatch(tenDangNhap))
            loi.Add(new LoiTruong("tenDangNhap", QuyTacTaiKhoan.LoiTenDangNhap));
        if (!QuyTacTaiKhoan.HoTenHopLe(hoTen))
            loi.Add(new LoiTruong("hoTen", QuyTacTaiKhoan.LoiHoTen));
        if (!QuyTacTaiKhoan.SoDienThoaiHopLe(soDienThoai))
            loi.Add(new LoiTruong("soDienThoai", QuyTacTaiKhoan.LoiSoDienThoai));
        if (!QuyTacTaiKhoan.EmailHopLe(email))
            loi.Add(new LoiTruong("email", QuyTacTaiKhoan.LoiEmail));
        if (!QuyTacTaiKhoan.MatKhauHopLe(yeuCau.MatKhauTam))
            loi.Add(new LoiTruong("matKhauTam", QuyTacTaiKhoan.LoiMatKhau));
        if (!BangQuyen.LaVaiTroHopLe(vaiTro))
            loi.Add(new LoiTruong("vaiTro", QuyTacTaiKhoan.LoiVaiTro));
        if (loi.Count > 0)
            throw LoiNghiepVu.DuLieuKhongHopLe(loi);

        await using var giaoTac = await db.Database.BeginTransactionAsync(ct);
        var soTrungTen = Convert.ToInt32(await db.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM tai_khoan WHERE ten_dang_nhap = @ten", ct, ("@ten", tenDangNhap)));
        if (soTrungTen > 0)
            throw LoiTrungTenDangNhap();
        await KiemTraTrungLienLacAsync(null, soDienThoai, email, ct);

        long id;
        try
        {
            id = Convert.ToInt64(await db.DocMotGiaTriAsync(
                """
                INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, so_dien_thoai, email, vai_tro_id,
                                       trang_thai, bat_buoc_doi_mk)
                VALUES (@ten, @bam, @hoTen, @sdt, @email, (SELECT id FROM vai_tro WHERE ma = @vaiTro), 'HOAT_DONG', TRUE);
                SELECT LAST_INSERT_ID();
                """,
                ct,
                ("@ten", tenDangNhap), ("@bam", BamMatKhau.Bam(yeuCau.MatKhauTam!)), ("@hoTen", hoTen),
                ("@sdt", soDienThoai), ("@email", email), ("@vaiTro", vaiTro)));
        }
        catch (DbException loiCsdl) when (ChuyenLoiTrung(loiCsdl) is { } loiTrung)
        {
            throw loiTrung;
        }

        await giaoTac.CommitAsync(ct);
        return await XemAsync(id, ct);
    }

    public async Task<TaiKhoanDto> GanVaiTroAsync(long id, YeuCauGanVaiTro yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);
        var vaiTroMoi = yeuCau.VaiTro?.Trim();
        if (!BangQuyen.LaVaiTroHopLe(vaiTroMoi))
            throw LoiNghiepVu.DuLieuKhongHopLe("vaiTro", QuyTacTaiKhoan.LoiVaiTro);

        await using var giaoTac = await db.Database.BeginTransactionAsync(ct);
        var adminHoatDong = await KhoaAdminHoatDongAsync(ct);
        var hienTai = await KhoaTaiKhoanAsync(id, ct);

        if (hienTai.MaVaiTro == vaiTroMoi)
        {
            await giaoTac.RollbackAsync(ct);
            return await XemAsync(id, ct);
        }

        if (hienTai.MaVaiTro == BangQuyen.Admin && hienTai.TrangThai == "HOAT_DONG" && adminHoatDong.Count <= 1)
            throw LoiNghiepVu.XungDot(
                MaLoiAuth.MatAdminCuoiCung,
                "Không thể đổi vai trò vì hệ thống phải còn ít nhất một Admin đang hoạt động.");

        await db.ChayAsync(
            """
            UPDATE tai_khoan
            SET vai_tro_id = (SELECT id FROM vai_tro WHERE ma = @vaiTro), phien_ban = phien_ban + 1
            WHERE id = @id
            """,
            ct, ("@vaiTro", vaiTroMoi), ("@id", id));
        await giaoTac.CommitAsync(ct);
        return await XemAsync(id, ct);
    }

    public async Task<TrangDuLieu<VaiTroDto>> XemDanhSachVaiTroAsync(CancellationToken ct = default)
    {
        var danhSach = await db.DocDanhSachAsync(
            "SELECT ma, ten FROM vai_tro ORDER BY id",
            doc => new VaiTroDto(doc.Chuoi(0), doc.Chuoi(1), BangQuyen.LayQuyen(doc.Chuoi(0))),
            ct);
        return new TrangDuLieu<VaiTroDto>(danhSach, danhSach.Count, 1, danhSach.Count);
    }

    private async Task<TaiKhoanDto?> TimAsync(long id, CancellationToken ct) =>
        id <= 0 ? null : await db.DocMotDongAsync(SqlChonTaiKhoan + " WHERE t.id = @id", DocTaiKhoan, ct, ("@id", id));

    private async Task<List<long>> KhoaAdminHoatDongAsync(CancellationToken ct) =>
        await db.DocDanhSachAsync(SqlKhoaAdminHoatDong, doc => doc.SoNguyenDai(0), ct);

    private async Task<TaiKhoanKhoa> KhoaTaiKhoanAsync(long id, CancellationToken ct)
    {
        if (id <= 0)
            throw LoiNghiepVu.KhongTimThay(KhongTimThayTaiKhoan);
        return await db.DocMotDongAsync(
            """
            SELECT t.ten_dang_nhap, v.ma, t.trang_thai, t.khoa_tam_den IS NOT NULL AND t.khoa_tam_den > NOW(), t.phien_ban
            FROM tai_khoan t
            JOIN vai_tro v ON v.id = t.vai_tro_id
            WHERE t.id = @id
            FOR UPDATE OF t
            """,
            doc => new TaiKhoanKhoa(doc.Chuoi(0), doc.Chuoi(1), doc.Chuoi(2), doc.DungSai(3), doc.SoNguyen(4)),
            ct, ("@id", id))
            ?? throw LoiNghiepVu.KhongTimThay(KhongTimThayTaiKhoan);
    }

    private async Task KiemTraTrungLienLacAsync(long? boQuaId, string? soDienThoai, string? email, CancellationToken ct)
    {
        if (soDienThoai is null && email is null)
            return;

        var trung = await db.DocDanhSachAsync(
            """
            SELECT so_dien_thoai, email
            FROM tai_khoan
            WHERE (@boQua IS NULL OR id <> @boQua)
              AND ((@sdt IS NOT NULL AND so_dien_thoai = @sdt) OR (@email IS NOT NULL AND email = @email))
            """,
            doc => (Sdt: doc.ChuoiHoacNull(0), Email: doc.ChuoiHoacNull(1)),
            ct, ("@boQua", boQuaId), ("@sdt", soDienThoai), ("@email", email));

        var trungSdt = soDienThoai is not null
            && trung.Any(t => string.Equals(t.Sdt, soDienThoai, StringComparison.OrdinalIgnoreCase));
        var trungEmail = email is not null
            && trung.Any(t => string.Equals(t.Email, email, StringComparison.OrdinalIgnoreCase));
        if (trungSdt || trungEmail)
            throw LoiTrungLienLac(trungSdt, trungEmail);
    }

    private static LoiNghiepVu LoiTrungTenDangNhap()
    {
        const string thongBao = "Tên đăng nhập đã tồn tại.";
        return LoiNghiepVu.XungDot(MaLoiAuth.TrungTenDangNhap, thongBao, new[] { new LoiTruong("tenDangNhap", thongBao) });
    }

    private static LoiNghiepVu LoiTrungLienLac(bool trungSdt, bool trungEmail)
    {
        var chiTiet = new List<LoiTruong>();
        if (trungSdt)
            chiTiet.Add(new LoiTruong("soDienThoai", "Số điện thoại đã được dùng cho tài khoản khác."));
        if (trungEmail)
            chiTiet.Add(new LoiTruong("email", "Email đã được dùng cho tài khoản khác."));
        var thongBao = (trungSdt, trungEmail) switch
        {
            (true, true) => "Số điện thoại và email đã được dùng cho tài khoản khác.",
            (true, false) => "Số điện thoại đã được dùng cho tài khoản khác.",
            _ => "Email đã được dùng cho tài khoản khác.",
        };
        return LoiNghiepVu.XungDot(MaLoiChung.TrungDuLieu, thongBao, chiTiet);
    }

    private static LoiNghiepVu? ChuyenLoiTrung(DbException loi)
    {
        if (TruyVanCsdl.LaLoiTrungKhoa(loi, "uq_tai_khoan_ten"))
            return LoiTrungTenDangNhap();
        if (TruyVanCsdl.LaLoiTrungKhoa(loi, "uq_tai_khoan_sdt"))
            return LoiTrungLienLac(true, false);
        if (TruyVanCsdl.LaLoiTrungKhoa(loi, "uq_tai_khoan_email"))
            return LoiTrungLienLac(false, true);
        return null;
    }

    private static string TaoSapXep(string? sort, List<LoiTruong> loi)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return "t.ten_dang_nhap, t.id";

        var phan = new List<string>();
        foreach (var muc in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var giam = muc.StartsWith('-');
            var ten = giam ? muc[1..] : muc;
            if (!CotSapXep.TryGetValue(ten, out var cot))
            {
                loi.Add(new LoiTruong("sort", "Chỉ sắp xếp được theo tenDangNhap, hoTen, lanDangNhapCuoi, ngayTao."));
                return "t.id";
            }

            phan.Add(giam ? cot + " DESC" : cot);
        }

        phan.Add("t.id");
        return string.Join(", ", phan);
    }

    private static string ThoatKyTuLike(string giaTri) =>
        giaTri.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static TaiKhoanDto DocTaiKhoan(DbDataReader doc) => new(
        doc.SoNguyenDai(0),
        doc.Chuoi(1),
        doc.Chuoi(2),
        doc.ChuoiHoacNull(3),
        doc.ChuoiHoacNull(4),
        new VaiTroTomTat(doc.Chuoi(5), doc.Chuoi(6)),
        doc.Chuoi(7),
        doc.ChuoiHoacNull(8),
        GioVietNam.ThanhOffset(doc.NgayGioHoacNull(9)),
        doc.DungSai(10),
        GioVietNam.ThanhOffset(doc.NgayGioHoacNull(11)),
        GioVietNam.ThanhOffset(doc.NgayGio(12)),
        GioVietNam.ThanhOffset(doc.NgayGio(13)),
        doc.SoNguyen(14));

    private sealed record TaiKhoanKhoa(
        string TenDangNhap,
        string MaVaiTro,
        string TrangThai,
        bool DangKhoaTam,
        int PhienBan);
}

using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ConvenienceStore.Web.Modules.Auth.Services;

/// <summary>
/// Cấu hình tài khoản Admin đầu tiên, đọc từ mục "AdminDauTien"
/// (biến môi trường AdminDauTien__TenDangNhap, AdminDauTien__MatKhauTam).
/// </summary>
public sealed class AdminDauTienOptions
{
    public const string TenMuc = "AdminDauTien";

    public string? TenDangNhap { get; set; }

    public string? MatKhauTam { get; set; }

    public string HoTen { get; set; } = "Quản trị viên";
}

/// <summary>Kết quả một lần chạy khởi tạo Admin đầu tiên.</summary>
public enum KetQuaKhoiTaoAdmin
{
    /// <summary>Đã tạo tài khoản Admin đầu tiên.</summary>
    DaTao,

    /// <summary>CSDL đã có tài khoản vai trò AD, không làm gì.</summary>
    DaCoAdmin,

    /// <summary>Thiếu tên đăng nhập hoặc mật khẩu tạm trong cấu hình.</summary>
    ThieuCauHinh,

    /// <summary>Tên đăng nhập, mật khẩu tạm hoặc họ tên sai quy tắc.</summary>
    CauHinhKhongHopLe,

    /// <summary>Tên đăng nhập đã thuộc về một tài khoản không phải Admin.</summary>
    TrungTenDangNhap,

    /// <summary>CSDL thiếu dòng vai_tro 'AD' (migration chưa chạy đủ).</summary>
    ThieuVaiTroAdmin,
}

/// <summary>
/// T4-16: tạo tài khoản Admin đầu tiên khi CSDL chưa có tài khoản vai trò AD
/// (auth-giai-thich.md mục 9.1; ADR-001 mục 3.8; ERD Tài khoản-Vai trò v1 mục 9).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Chỉ tạo khi chưa có tài khoản nào thuộc vai trò AD (kể cả đang bị khóa), nên chạy lại nhiều lần không tạo trùng.</item>
/// <item>Mật khẩu băm bằng PasswordHasher của ASP.NET Core (BR-AUTH-01); tài khoản bắt buộc đổi mật khẩu ở lần đăng nhập đầu (AUTH-01 A1).</item>
/// <item>Thiếu cấu hình hoặc cấu hình sai: không tạo, ghi cảnh báo, ứng dụng vẫn chạy.</item>
/// <item>Làm việc thẳng trên kết nối bằng SQL có tham số, không phụ thuộc lớp entity sinh tự động.</item>
/// </list>
/// </remarks>
public sealed partial class KhoiTaoAdminDauTien(
    IOptions<AdminDauTienOptions> cauHinh,
    ILogger<KhoiTaoAdminDauTien> log)
{
    private const string MaVaiTroAdmin = "AD";

    /// <summary>
    /// Bộ băm mật khẩu. Định dạng chuỗi băm không phụ thuộc kiểu người dùng,
    /// nên AUTH-01, 03, 06, 09 dùng PasswordHasher với bất kỳ kiểu nào đều kiểm tra được.
    /// </summary>
    public static readonly IPasswordHasher<object> BoBamMatKhau = new PasswordHasher<object>();

    [GeneratedRegex("^[A-Za-z0-9._]{3,50}$")]
    private static partial Regex MauTenDangNhap();

    [GeneratedRegex("^(?=.*[A-Za-z])(?=.*\\d).{8,128}$")]
    private static partial Regex MauMatKhau();

    /// <summary>
    /// Gọi một lần khi ứng dụng khởi động (Program.cs). Không bao giờ ném lỗi ra ngoài:
    /// lỗi CSDL được ghi log để ứng dụng vẫn chạy.
    /// </summary>
    public static async Task ChayKhiKhoiDongAsync(IServiceProvider dichVu, CancellationToken ct = default)
    {
        await using var phamVi = dichVu.CreateAsyncScope();
        var khoiTao = phamVi.ServiceProvider.GetRequiredService<KhoiTaoAdminDauTien>();
        try
        {
            var db = phamVi.ServiceProvider.GetRequiredService<CuaHangDbContext>();
            await db.Database.OpenConnectionAsync(ct); // GioVietNamInterceptor đặt múi giờ
            try
            {
                await khoiTao.ChayAsync(db.Database.GetDbConnection(), ct);
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
        catch (Exception loi) when (loi is not OperationCanceledException)
        {
            phamVi.ServiceProvider.GetRequiredService<ILogger<KhoiTaoAdminDauTien>>().LogError(loi,
                "Không khởi tạo được tài khoản Admin đầu tiên. Kiểm tra kết nối CSDL và migration.");
        }
    }

    /// <summary>
    /// Kiểm tra và tạo Admin đầu tiên trên một kết nối đã mở (múi giờ phiên đã là +07:00).
    /// </summary>
    public async Task<KetQuaKhoiTaoAdmin> ChayAsync(DbConnection ketNoi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ketNoi);
        if (ketNoi.State != ConnectionState.Open)
            throw new InvalidOperationException("Kết nối CSDL phải được mở trước khi khởi tạo Admin.");

        var ch = cauHinh.Value;
        var tenDangNhap = ch.TenDangNhap?.Trim();
        var matKhauTam = ch.MatKhauTam;
        var hoTen = string.IsNullOrWhiteSpace(ch.HoTen) ? "Quản trị viên" : ch.HoTen.Trim();

        if (string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(matKhauTam))
        {
            log.LogWarning(
                "Chưa tạo tài khoản Admin đầu tiên: thiếu biến môi trường AdminDauTien__TenDangNhap hoặc AdminDauTien__MatKhauTam.");
            return KetQuaKhoiTaoAdmin.ThieuCauHinh;
        }

        if (!MauTenDangNhap().IsMatch(tenDangNhap))
        {
            log.LogWarning(
                "Chưa tạo tài khoản Admin đầu tiên: tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới, dài 3–50 ký tự.");
            return KetQuaKhoiTaoAdmin.CauHinhKhongHopLe;
        }

        if (!MauMatKhau().IsMatch(matKhauTam))
        {
            // BR-AUTH-04. Không ghi mật khẩu vào log.
            log.LogWarning(
                "Chưa tạo tài khoản Admin đầu tiên: mật khẩu tạm phải dài 8–128 ký tự, có ít nhất một chữ cái và một chữ số.");
            return KetQuaKhoiTaoAdmin.CauHinhKhongHopLe;
        }

        if (hoTen.Length > 100)
        {
            log.LogWarning("Chưa tạo tài khoản Admin đầu tiên: họ tên dài quá 100 ký tự.");
            return KetQuaKhoiTaoAdmin.CauHinhKhongHopLe;
        }

        await using var giaoTac = await ketNoi.BeginTransactionAsync(ct);

        // Khóa dòng vai trò AD để hai tiến trình khởi động cùng lúc không cùng tạo Admin.
        var vaiTroAdminId = await DocMotGiaTriAsync(ketNoi, giaoTac,
            "SELECT id FROM vai_tro WHERE ma = @ma FOR UPDATE", ct, ("@ma", MaVaiTroAdmin));
        if (vaiTroAdminId is null)
        {
            log.LogError("Chưa tạo tài khoản Admin đầu tiên: bảng vai_tro chưa có dòng 'AD'. Hãy chạy migration.");
            await giaoTac.RollbackAsync(ct);
            return KetQuaKhoiTaoAdmin.ThieuVaiTroAdmin;
        }

        var soAdmin = Convert.ToInt64(await DocMotGiaTriAsync(ketNoi, giaoTac,
            "SELECT COUNT(*) FROM tai_khoan WHERE vai_tro_id = @vt", ct, ("@vt", vaiTroAdminId)));
        if (soAdmin > 0)
        {
            await giaoTac.RollbackAsync(ct);
            log.LogInformation("CSDL đã có tài khoản Admin, bỏ qua khởi tạo Admin đầu tiên.");
            return KetQuaKhoiTaoAdmin.DaCoAdmin;
        }

        // Không phân biệt hoa thường nhờ collation utf8mb4_0900_ai_ci (BR-AUTH-06).
        var soTrungTen = Convert.ToInt64(await DocMotGiaTriAsync(ketNoi, giaoTac,
            "SELECT COUNT(*) FROM tai_khoan WHERE ten_dang_nhap = @ten", ct, ("@ten", tenDangNhap)));
        if (soTrungTen > 0)
        {
            await giaoTac.RollbackAsync(ct);
            log.LogError(
                "Chưa tạo tài khoản Admin đầu tiên: tên đăng nhập {TenDangNhap} đã thuộc một tài khoản không phải Admin.",
                tenDangNhap);
            return KetQuaKhoiTaoAdmin.TrungTenDangNhap;
        }

        var matKhauBam = BoBamMatKhau.HashPassword(new object(), matKhauTam);

        await using (var lenh = TaoLenh(ketNoi, giaoTac,
            """
            INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id, trang_thai, bat_buoc_doi_mk)
            VALUES (@ten, @bam, @hoTen, @vt, 'HOAT_DONG', TRUE)
            """,
            ("@ten", tenDangNhap), ("@bam", matKhauBam), ("@hoTen", hoTen), ("@vt", vaiTroAdminId)))
        {
            await lenh.ExecuteNonQueryAsync(ct);
        }

        await giaoTac.CommitAsync(ct);
        log.LogInformation(
            "Đã tạo tài khoản Admin đầu tiên {TenDangNhap}; lần đăng nhập đầu phải đổi mật khẩu.", tenDangNhap);
        return KetQuaKhoiTaoAdmin.DaTao;
    }

    private static DbCommand TaoLenh(
        DbConnection ketNoi, DbTransaction giaoTac, string sql, params (string Ten, object GiaTri)[] thamSo)
    {
        var lenh = ketNoi.CreateCommand();
        lenh.Transaction = giaoTac;
        lenh.CommandText = sql;
        foreach (var (ten, giaTri) in thamSo)
        {
            var p = lenh.CreateParameter();
            p.ParameterName = ten;
            p.Value = giaTri;
            lenh.Parameters.Add(p);
        }

        return lenh;
    }

    private static async Task<object?> DocMotGiaTriAsync(
        DbConnection ketNoi, DbTransaction giaoTac, string sql, CancellationToken ct,
        params (string Ten, object GiaTri)[] thamSo)
    {
        await using var lenh = TaoLenh(ketNoi, giaoTac, sql, thamSo);
        var kq = await lenh.ExecuteScalarAsync(ct);
        return kq is DBNull ? null : kq;
    }
}

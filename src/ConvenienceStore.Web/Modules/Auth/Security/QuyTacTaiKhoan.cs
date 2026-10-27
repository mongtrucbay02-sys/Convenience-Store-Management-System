using System.Text.RegularExpressions;

namespace ConvenienceStore.Web.Modules.Auth.Security;

public static partial class QuyTacTaiKhoan
{
    public const string LoiTenDangNhap =
        "Tên đăng nhập dài 3–50 ký tự, chỉ gồm chữ không dấu, số, dấu chấm và gạch dưới.";
    public const string LoiMatKhau = "Mật khẩu tối thiểu 8 ký tự, có ít nhất một chữ cái và một chữ số.";
    public const string LoiSoDienThoai = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.";
    public const string LoiEmail = "Email không đúng định dạng.";
    public const string LoiHoTen = "Vui lòng nhập họ tên (tối đa 100 ký tự).";
    public const string LoiVaiTro = "Vai trò phải là AD, TN hoặc NK.";

    [GeneratedRegex("^[A-Za-z0-9._]{3,50}$")]
    public static partial Regex MauTenDangNhap();

    [GeneratedRegex("^(?=.*[A-Za-z])(?=.*[0-9]).{8,128}$")]
    public static partial Regex MauMatKhau();

    [GeneratedRegex("^0[0-9]{9}$")]
    public static partial Regex MauSoDienThoai();

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$")]
    public static partial Regex MauEmail();

    public static string? ChuanHoa(string? giaTri) =>
        string.IsNullOrWhiteSpace(giaTri) ? null : giaTri.Trim();

    public static bool HoTenHopLe(string? hoTen) => hoTen is { Length: > 0 and <= 100 };

    public static bool SoDienThoaiHopLe(string? soDienThoai) =>
        soDienThoai is null || MauSoDienThoai().IsMatch(soDienThoai);

    public static bool EmailHopLe(string? email) =>
        email is null || (email.Length <= 100 && MauEmail().IsMatch(email));

    public static bool MatKhauHopLe(string? matKhau) => matKhau is not null && MauMatKhau().IsMatch(matKhau);
}

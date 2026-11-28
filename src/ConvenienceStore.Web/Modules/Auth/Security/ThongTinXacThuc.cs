namespace ConvenienceStore.Web.Modules.Auth.Security;

public static class ThongTinXacThuc
{
    public const string TenScheme = "PhienDangNhap";
    public const string CookiePhien = "cs_session";
    public const string CookieXsrf = "XSRF-TOKEN";
    public const string HeaderXsrf = "X-XSRF-TOKEN";
    public const string ClaimQuyen = "quyen";
    public const string ClaimPhienId = "phien_id";
    public const string ClaimHoTen = "ho_ten";
    public const string ClaimBatBuocDoiMatKhau = "bat_buoc_doi_mk";
    public const int SoPhutPhien = 30;
    public const int SoPhutKhoaTam = 15;
    public const int SoLanSaiToiDa = 5;
}

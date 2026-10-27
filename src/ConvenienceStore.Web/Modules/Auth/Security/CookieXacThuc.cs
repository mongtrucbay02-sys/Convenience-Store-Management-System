namespace ConvenienceStore.Web.Modules.Auth.Security;

public static class CookieXacThuc
{
    public static void Dat(HttpResponse phanHoi, string tokenPhien, bool chiQuaHttps)
    {
        phanHoi.Cookies.Append(ThongTinXacThuc.CookiePhien, tokenPhien, TaoTuyChon(chiQuaHttps, true));
        phanHoi.Cookies.Append(
            ThongTinXacThuc.CookieXsrf, TokenPhien.TaoXsrf(tokenPhien), TaoTuyChon(chiQuaHttps, false));
    }

    public static void Xoa(HttpResponse phanHoi, bool chiQuaHttps)
    {
        phanHoi.Cookies.Delete(ThongTinXacThuc.CookiePhien, TaoTuyChon(chiQuaHttps, true));
        phanHoi.Cookies.Delete(ThongTinXacThuc.CookieXsrf, TaoTuyChon(chiQuaHttps, false));
    }

    private static CookieOptions TaoTuyChon(bool chiQuaHttps, bool anVoiJavaScript) => new()
    {
        HttpOnly = anVoiJavaScript,
        Secure = chiQuaHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true,
    };
}

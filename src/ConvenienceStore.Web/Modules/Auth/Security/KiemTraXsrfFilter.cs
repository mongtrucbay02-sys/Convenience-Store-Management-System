using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConvenienceStore.Web.Modules.Auth.Security;

public sealed class KiemTraXsrfFilter : IAuthorizationFilter
{
    private const string ThongBao = "Yêu cầu không hợp lệ, vui lòng tải lại trang và thử lại.";

    public void OnAuthorization(AuthorizationFilterContext nguCanh)
    {
        var http = nguCanh.HttpContext;
        if (!XuLyLoiApiMiddleware.LaDuongDanApi(http.Request.Path)
            || HttpMethods.IsGet(http.Request.Method)
            || HttpMethods.IsHead(http.Request.Method)
            || HttpMethods.IsOptions(http.Request.Method)
            || nguCanh.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()
            || http.User.Identity?.IsAuthenticated != true)
            return;

        var hopLe = http.Request.Cookies.TryGetValue(ThongTinXacThuc.CookiePhien, out var token)
            && !string.IsNullOrEmpty(token)
            && TokenPhien.KhopXsrf(token, http.Request.Headers[ThongTinXacThuc.HeaderXsrf].ToString());
        if (!hopLe)
            nguCanh.Result = PhanHoiLoiApi.TaoKetQua(
                http, StatusCodes.Status403Forbidden, MaLoiChung.KhongDuQuyen, ThongBao, null);
    }
}

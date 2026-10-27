using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConvenienceStore.Web.Modules.Auth.Security;

public sealed class KiemTraDoiMatKhauFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext nguCanh)
    {
        var http = nguCanh.HttpContext;
        if (!XuLyLoiApiMiddleware.LaDuongDanApi(http.Request.Path)
            || http.User.Identity?.IsAuthenticated != true
            || !http.User.DangPhaiDoiMatKhau()
            || nguCanh.ActionDescriptor.EndpointMetadata.OfType<ChoPhepKhiPhaiDoiMatKhauAttribute>().Any())
            return;

        nguCanh.Result = PhanHoiLoiApi.TaoKetQua(
            http, StatusCodes.Status403Forbidden, MaLoiChung.PhaiDoiMatKhau, ThongBaoLoi.PhaiDoiMatKhau, null);
    }
}

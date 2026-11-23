using ConvenienceStore.Web.Modules.Auth.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConvenienceStore.Web.Shared.Security;

/// <summary>Use on MVC business pages as AUTH's password-change filter covers only API paths.</summary>
[AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
public sealed class RequirePasswordChangedAttribute : Attribute,IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if(context.HttpContext.User.Identity?.IsAuthenticated==true && context.HttpContext.User.DangPhaiDoiMatKhau())
            context.Result=new ForbidResult();
    }
}

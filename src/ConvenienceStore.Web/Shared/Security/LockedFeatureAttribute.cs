using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConvenienceStore.Web.Shared.Security;

/// <summary>Server-side scope lock; retained domain/history code is not an enabled endpoint.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LockedFeatureAttribute(string feature) : Attribute, IAuthorizationFilter, IOrderedFilter
{
    // Reject before antiforgery, model binding, controller activation or domain I/O.
    public int Order => int.MinValue;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Chức năng đang khóa",
            Detail = $"{feature} đang khóa trong phạm vi hiện tại.",
            Extensions = { ["code"] = "CHUC_NANG_DANG_KHOA", ["feature"] = feature }
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }
}

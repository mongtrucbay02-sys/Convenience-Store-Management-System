using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Returns.Controllers;

[Authorize(Policy = MaQuyen.DoiTra, Roles = BangQuyen.ThuNgan)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Returns")]
public sealed class ReturnsController(InvoiceLookupService lookup) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] InvoiceLookupRequest query, CancellationToken cancellationToken)
    {
        try { ReturnLookupAccess.Require(User); }
        catch (LoiNghiepVu error) when (error.MaHttp == 403) { return Forbid(); }
        var model = new InvoiceLookupViewModel { Query = query };
        if (!ModelState.IsValid) model.Error = "Tham số tra cứu không đúng định dạng.";
        else if (Request.Query.Count > 0)
        {
            try { model.Result = await lookup.LookupAsync(query, User, cancellationToken); }
            catch (LoiNghiepVu error) when (error.MaHttp is 409 or 422) { model.Error = error.Message; }
        }
        return View("~/Modules/Returns/Views/Index.cshtml", model);
    }
}

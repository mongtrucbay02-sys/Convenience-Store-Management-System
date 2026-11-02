using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Returns.Controllers;

[ApiController]
[Authorize(Policy = MaQuyen.DoiTra, Roles = BangQuyen.ThuNgan)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/returns/invoice-lookup")]
public sealed class ReturnInvoiceLookupApiController(InvoiceLookupService lookup) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] InvoiceLookupRequest query, CancellationToken cancellationToken) =>
        Ok(await lookup.LookupAsync(query, User, cancellationToken));
}

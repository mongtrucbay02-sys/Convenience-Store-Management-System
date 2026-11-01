using ConvenienceStore.Web.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

[Authorize(Policy = PosInvoiceAccess.PolicyXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InvoiceDetailsController(PosInvoiceService invoices) : Controller
{
    [HttpGet("Invoices/{id:long}")]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return BadRequest("Mã hóa đơn không hợp lệ.");
        try
        {
            var invoice = await invoices.GetDetailsAsync(id, PosInvoiceAccess.FromUser(User), cancellationToken);
            return invoice is null ? NotFound("Không tìm thấy hóa đơn.") : View("~/Views/Invoices/Details.cshtml", invoice);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}

using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Returns.Controllers;

[ApiController]
[Authorize(Policy=MaQuyen.DoiTra,Roles=BangQuyen.ThuNgan)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[Route("api/v1/returns")]
public sealed class ReturnOrdersApiController(ReturnOrderService service, ExchangeService exchange) : ControllerBase
{
    [HttpGet("invoices/{invoiceId:long}")]
    public async Task<IActionResult> Form(long invoiceId,CancellationToken ct)=>Ok(await service.FormAsync(invoiceId,User,ct));
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(ReturnOrderRequest request,CancellationToken ct)=>Ok(await service.PreviewAsync(request,User,ct));
    [HttpPost]
    public async Task<IActionResult> Create(ReturnOrderRequest request,CancellationToken ct)
    {
        long id=await service.CreateAsync(request,User,ct);
        return CreatedAtAction(nameof(Receipt),new{id},await service.ReceiptAsync(id,User,ct));
    }
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Receipt(long id,CancellationToken ct)=>Ok(await service.ReceiptAsync(id,User,ct));

    [HttpGet("exchange/invoices/{invoiceId:long}")]
    public async Task<IActionResult> ExchangeForm(long invoiceId, CancellationToken ct) =>
        Ok(await exchange.FormAsync(new ExchangeRequest { InvoiceId = invoiceId }, User, ct));

    [HttpPost("exchange/preview")]
    public async Task<IActionResult> ExchangePreview(ExchangeRequest request, CancellationToken ct) =>
        Ok(await exchange.PreviewAsync(request, User, ct));

    [HttpPost("exchange")]
    public async Task<IActionResult> ExchangeCreate(ExchangeRequest request, CancellationToken ct)
    {
        var id = await exchange.CreateAsync(request, User, ct);
        return CreatedAtAction(nameof(ExchangeReceipt), new { id }, await exchange.ReceiptAsync(id, User, ct));
    }

    [HttpGet("exchange/{id:long}")]
    public async Task<IActionResult> ExchangeReceipt(long id, CancellationToken ct) =>
        Ok(await exchange.ReceiptAsync(id, User, ct));
}

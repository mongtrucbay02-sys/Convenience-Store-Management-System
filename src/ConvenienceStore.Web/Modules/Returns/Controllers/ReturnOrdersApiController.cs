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
public sealed class ReturnOrdersApiController(ReturnOrderService service) : ControllerBase
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
}

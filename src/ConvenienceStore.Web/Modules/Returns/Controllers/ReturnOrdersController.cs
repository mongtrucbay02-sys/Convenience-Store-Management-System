using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConvenienceStore.Web.Modules.Returns.Controllers;

[Authorize(Policy=MaQuyen.DoiTra,Roles=BangQuyen.ThuNgan)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[Route("Returns")]
public sealed class ReturnOrdersController(ReturnOrderService service, ExchangeService exchange) : Controller
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if(context.Exception is LoiNghiepVu e && e.MaHttp<500)
        {
            context.ExceptionHandled=true;
            context.Result=e.MaHttp switch {
                401=>Challenge(),403=>Forbid(),
                _=>new ContentResult { StatusCode=e.MaHttp,Content=e.Message,ContentType="text/plain; charset=utf-8" }
            };
        }
        base.OnActionExecuted(context);
    }
    [HttpGet("New")]
    public async Task<IActionResult> New(long invoiceId,CancellationToken ct)
    {
        var form=await service.FormAsync(invoiceId,User,ct);
        return View("~/Modules/Returns/Views/New.cshtml",new ReturnOrderPage {
            Invoice=form,Request=new() { InvoiceId=invoiceId,Lines=form.Lines.Select(l=>new ReturnLineRequest { ProductId=l.ProductId }).ToList() } });
    }
    [HttpPost("Preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([Bind(Prefix="Request")]ReturnOrderRequest request,CancellationToken ct)
    {
        if(!ModelState.IsValid)return await Error(request,"Dữ liệu nhập không hợp lệ. Kiểm tra số lượng và các trường bắt buộc.",422,ct);
        try
        {
            var preview=await service.PreviewAsync(request,User,ct);
            request.ConfirmationToken=preview.ConfirmationToken;
            return View("~/Modules/Returns/Views/Preview.cshtml",new ReturnOrderPage { Request=request,Preview=preview });
        }
        catch(LoiNghiepVu e) when(e.MaHttp is 409 or 422) { return await Error(request,e.Message, e.MaHttp,ct); }
    }
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix="Request")]ReturnOrderRequest request,CancellationToken ct)
    {
        if(!ModelState.IsValid)return await Error(request,"Dữ liệu xác nhận không hợp lệ.",422,ct);
        try { var id=await service.CreateAsync(request,User,ct);return RedirectToAction(nameof(Receipt),new{id}); }
        catch(LoiNghiepVu e) when(e.MaHttp is 409 or 422) { return await Error(request,e.Message,e.MaHttp,ct); }
    }
    private async Task<IActionResult> Error(ReturnOrderRequest request,string message,int status,CancellationToken ct)
    {
        var form=await service.FormAsync(request.InvoiceId,User,ct);
        Response.StatusCode=status;
        return View("~/Modules/Returns/Views/New.cshtml",new ReturnOrderPage { Invoice=form,Request=request,Error=message });
    }
    [HttpGet("Receipts/{id:long}")]
    public async Task<IActionResult> Receipt(long id,CancellationToken ct)=>
        View("~/Modules/Returns/Views/Receipt.cshtml",await service.ReceiptAsync(id,User,ct));
    [HttpGet("Exchange")]
    public Task<IActionResult> Exchange(long invoiceId, CancellationToken ct) =>
        ExchangeForm(new ExchangeRequest { InvoiceId = invoiceId }, ct);

    [HttpPost("Exchange/Preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExchangePreview([Bind(Prefix = "Request")] ExchangeRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return await ExchangeForm(request, ct, error: "Dữ liệu đổi hàng không hợp lệ.", status: 422);
        try
        {
            return await ExchangeForm(request, ct, await exchange.PreviewAsync(request, User, ct));
        }
        catch (LoiNghiepVu e) when (e.MaHttp is 409 or 422)
        {
            return await ExchangeForm(request, ct, error: ExchangeError(e), status: e.MaHttp);
        }
    }

    [HttpPost("Exchange/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExchangeCreate([Bind(Prefix = "Request")] ExchangeRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return await ExchangeForm(request, ct, error: "Dữ liệu xác nhận không hợp lệ.", status: 422);
        try
        {
            var id = await exchange.CreateAsync(request, User, ct);
            return RedirectToAction(nameof(ExchangeReceipt), new { id });
        }
        catch (LoiNghiepVu e) when (e.MaHttp is 409 or 422)
        {
            return await ExchangeForm(request, ct, error: ExchangeError(e), status: e.MaHttp);
        }
    }

    [HttpGet("Exchange/Receipts/{id:long}")]
    public async Task<IActionResult> ExchangeReceipt(long id, CancellationToken ct) =>
        View("~/Modules/Returns/Views/ExchangeReceipt.cshtml", await exchange.ReceiptAsync(id, User, ct));

    private static string ExchangeError(LoiNghiepVu error) =>
        error.ChiTiet is IReadOnlyList<LoiTruong> fields && fields.Count > 0
            ? string.Join(" ", fields.Select(field => field.Loi))
            : error.Message;

    private async Task<IActionResult> ExchangeForm(ExchangeRequest request, CancellationToken ct,
        ExchangePreview? preview = null, string? error = null, int status = 200)
    {
        var page = await exchange.FormAsync(request, User, ct);
        Response.StatusCode = status;
        return View("~/Modules/Returns/Views/Exchange.cshtml", page with
        {
            Preview = preview,
            Error = error,
            NewItems = preview?.NewItems ?? page.NewItems
        });
    }
}

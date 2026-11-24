
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConvenienceStore.Web.Modules.Returns.Controllers;

[Authorize(Policy = MaQuyen.DoiTra, Roles = BangQuyen.ThuNgan)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Returns")]
public sealed class ReturnOrdersController(
    ReturnOrderService service,
    ExchangeService exchange) : Controller
{
    // =========================================================
    // 1. XỬ LÝ LỖI NGHIỆP VỤ
    // =========================================================

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is LoiNghiepVu e && e.MaHttp < 500)
        {
            context.ExceptionHandled = true;

            context.Result = e.MaHttp switch
            {
                401 => Challenge(),
                403 => Forbid(),

                _ => new ContentResult
                {
                    StatusCode = e.MaHttp,
                    Content = e.Message,
                    ContentType = "text/plain; charset=utf-8"
                }
            };
        }

        base.OnActionExecuted(context);
    }

    // =========================================================
    // 2. DT-01 / DT-02: TRẢ HÀNG
    // =========================================================

    [HttpGet("New")]
    public async Task<IActionResult> New(
        long invoiceId,
        CancellationToken ct)
    {
        var form = await service.FormAsync(invoiceId, User, ct);

        return View(
            "~/Modules/Returns/Views/New.cshtml",
            new ReturnOrderPage
            {
                Invoice = form,

                Request = new ReturnOrderRequest
                {
                    InvoiceId = invoiceId,

                    Lines = form.Lines
                        .Select(l => new ReturnLineRequest
                        {
                            ProductId = l.ProductId
                        })
                        .ToList()
                }
            });
    }

    // ---------------------------------------------------------
    // PREVIEW TRẢ HÀNG
    // ---------------------------------------------------------

    [HttpPost("Preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(
        [Bind(Prefix = "Request")]
        ReturnOrderRequest request,
        CancellationToken ct)
    {
        // Kiểm tra dữ liệu đầu vào
        if (!ModelState.IsValid)
        {
            return await Error(
                request,
                "Dữ liệu nhập không hợp lệ. " +
                "Vui lòng kiểm tra số lượng và các trường bắt buộc.",
                422,
                ct);
        }

        try
        {
            var preview = await service.PreviewAsync(
                request,
                User,
                ct);

            request.ConfirmationToken =
                preview.ConfirmationToken;

            return View(
                "~/Modules/Returns/Views/Preview.cshtml",
                new ReturnOrderPage
                {
                    Request = request,
                    Preview = preview
                });
        }
        catch (LoiNghiepVu e)
            when (e.MaHttp is 409 or 422)
        {
            return await Error(
                request,
                e.Message,
                e.MaHttp,
                ct);
        }
    }

    // ---------------------------------------------------------
    // CREATE TRẢ HÀNG
    // ---------------------------------------------------------

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Request")]
        ReturnOrderRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return await Error(
                request,
                "Dữ liệu xác nhận không hợp lệ.",
                422,
                ct);
        }

        try
        {
            var id = await service.CreateAsync(
                request,
                User,
                ct);

            return RedirectToAction(
                nameof(Receipt),
                new { id });
        }
        catch (LoiNghiepVu e)
            when (e.MaHttp is 409 or 422)
        {
            return await Error(
                request,
                e.Message,
                e.MaHttp,
                ct);
        }
    }

    // ---------------------------------------------------------
    // HIỂN THỊ LỖI TRẢ HÀNG
    // ---------------------------------------------------------

    private async Task<IActionResult> Error(
        ReturnOrderRequest request,
        string message,
        int status,
        CancellationToken ct)
    {
        var form = await service.FormAsync(
            request.InvoiceId,
            User,
            ct);

        Response.StatusCode = status;

        return View(
            "~/Modules/Returns/Views/New.cshtml",
            new ReturnOrderPage
            {
                Invoice = form,
                Request = request,
                Error = message
            });
    }

    // ---------------------------------------------------------
    // RECEIPT TRẢ HÀNG
    // ---------------------------------------------------------

    [HttpGet("Receipts/{id:long}")]
    public async Task<IActionResult> Receipt(
        long id,
        CancellationToken ct)
    {
        var receipt = await service.ReceiptAsync(
            id,
            User,
            ct);

        return View(
            "~/Modules/Returns/Views/Receipt.cshtml",
            receipt);
    }

    // =========================================================
    // 3. DT-03: ĐỔI HÀNG
    // =========================================================

    // ---------------------------------------------------------
    // MỞ FORM ĐỔI HÀNG
    // ---------------------------------------------------------

    [HttpGet("Exchange")]
    public async Task<IActionResult> Exchange(
        long invoiceId,
        CancellationToken ct)
    {
        var page = await exchange.FormAsync(
            invoiceId,
            User,
            ct);

        return View(
            "~/Modules/Returns/Views/Exchange.cshtml",
            page);
    }

    // ---------------------------------------------------------
    // PREVIEW ĐỔI HÀNG
    // ---------------------------------------------------------

    [HttpPost("Exchange/Preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExchangePreview(
        [Bind(Prefix = "Request")]
        ExchangeRequest request,
        CancellationToken ct)
    {
        // BƯỚC 1: Kiểm tra dữ liệu đầu vào
        if (!ModelState.IsValid)
        {
            return await ExchangeError(
                request,
                "Dữ liệu đổi hàng không hợp lệ. " +
                "Vui lòng kiểm tra số lượng hàng trả, " +
                "sản phẩm mới và các trường bắt buộc.",
                422,
                ct);
        }

        try
        {
            // BƯỚC 2: Tính toán Preview
            var preview = await exchange.PreviewAsync(
                request,
                User,
                ct);

            // BƯỚC 3: Lưu ConfirmationToken
            request.ConfirmationToken =
                preview.ConfirmationToken;

            // BƯỚC 4: Load lại form
            var page = await exchange.FormAsync(
                request.InvoiceId,
                User,
                ct);

            // BƯỚC 5: Giữ dữ liệu request
            page.Request = request;

            // BƯỚC 6: Gắn dữ liệu Preview
            page.Preview = preview;

            page.NewItems = preview.NewItems;

            // BƯỚC 7: Hiển thị trang xác nhận
            return View(
                "~/Modules/Returns/Views/Exchange.cshtml",
                page);
        }
        catch (LoiNghiepVu e)
            when (e.MaHttp is 409 or 422)
        {
            return await ExchangeError(
                request,
                e.Message,
                e.MaHttp,
                ct);
        }
    }

    // ---------------------------------------------------------
    // CREATE ĐỔI HÀNG
    // ---------------------------------------------------------

    [HttpPost("Exchange/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExchangeCreate(
        [Bind(Prefix = "Request")]
        ExchangeRequest request,
        CancellationToken ct)
    {
        // BƯỚC 1: Kiểm tra dữ liệu
        if (!ModelState.IsValid)
        {
            return await ExchangeError(
                request,
                "Dữ liệu xác nhận đổi hàng không hợp lệ.",
                422,
                ct);
        }

        try
        {
            // BƯỚC 2: Thực hiện giao dịch đổi hàng
            var id = await exchange.CreateAsync(
                request,
                User,
                ct);

            // BƯỚC 3: Chuyển đến biên nhận
            return RedirectToAction(
                nameof(ExchangeReceipt),
                new { id });
        }
        catch (LoiNghiepVu e)
            when (e.MaHttp is 409 or 422)
        {
            return await ExchangeError(
                request,
                e.Message,
                e.MaHttp,
                ct);
        }
    }

    // ---------------------------------------------------------
    // HIỂN THỊ LỖI ĐỔI HÀNG
    // ---------------------------------------------------------

    private async Task<IActionResult> ExchangeError(
        ExchangeRequest request,
        string message,
        int status,
        CancellationToken ct)
    {
        var page = await exchange.FormAsync(
            request.InvoiceId,
            User,
            ct);

        // Giữ lại dữ liệu người dùng đã nhập
        page.Request = request;

        // Hiển thị thông báo lỗi
        page.Error = message;

        // Trả đúng HTTP Status Code
        Response.StatusCode = status;

        return View(
            "~/Modules/Returns/Views/Exchange.cshtml",
            page);
    }

    // ---------------------------------------------------------
    // RECEIPT ĐỔI HÀNG
    // ---------------------------------------------------------

    [HttpGet("Exchange/Receipts/{id:long}")]
    public async Task<IActionResult> ExchangeReceipt(
        long id,
        CancellationToken ct)
    {
        var receipt = await exchange.ReceiptAsync(
            id,
            User,
            ct);

        return View(
            "~/Modules/Returns/Views/Exchange.cshtml",
            new ExchangePage
            {
                Receipt = receipt
            });
    }
}

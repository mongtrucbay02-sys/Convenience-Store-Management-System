using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Returns.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Returns/Approvals")]
public sealed class ReturnApprovalsController(ReturnApprovalService approvals) : Controller
{
    private const string AdminView = "~/Modules/Returns/Views/ApprovalDetails.cshtml";
    private const string CashierView = "~/Modules/Returns/Views/ApprovedExchange.cshtml";
    private const string ListView = "~/Modules/Returns/Views/ApprovalList.cshtml";

    [HttpGet("")]
    [Authorize(Roles = BangQuyen.Admin, Policy = MaQuyen.DoiTraDuyetCauHinh)]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(ListView, await approvals.PendingAsync(ct));

    [HttpGet("{id:long}")]
    [Authorize(Roles = BangQuyen.Admin, Policy = MaQuyen.DoiTraDuyetCauHinh)]
    public Task<IActionResult> Details(long id, CancellationToken ct) => RenderAsync(AdminView, id, null, ct);

    [HttpPost("{id:long}"), ValidateAntiForgeryToken]
    [Authorize(Roles = BangQuyen.Admin, Policy = MaQuyen.DoiTraDuyetCauHinh)]
    public async Task<IActionResult> Decide(long id, string? decision, string? note, bool refundConfirmed, CancellationToken ct)
    {
        if (!ModelState.IsValid || decision is not ("approve" or "reject"))
            return await RenderAsync(AdminView, id, null, ct, "Chọn duyệt hoặc từ chối và kiểm tra thông tin.", 422);

        try
        {
            await approvals.DecideAsync(id, User.LayTaiKhoanId(), decision == "approve", note, refundConfirmed, ct);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (LoiNghiepVu error) when (error.MaHttp is 404 or 409 or 422)
        {
            return await RenderAsync(AdminView, id, null, ct, error.Message, error.MaHttp);
        }
    }

    [HttpGet("ApprovedExchanges")]
    [Authorize(Roles = BangQuyen.ThuNgan, Policy = MaQuyen.DoiTra)]
    public async Task<IActionResult> ApprovedExchanges(CancellationToken ct)
    {
        ViewData["ForCashier"] = true;
        return View(ListView, await approvals.ApprovedExchangesAsync(User.LayTaiKhoanId(), ct));
    }

    [HttpGet("ApprovedExchanges/{id:long}")]
    [Authorize(Roles = BangQuyen.ThuNgan, Policy = MaQuyen.DoiTra)]
    public Task<IActionResult> ApprovedExchange(long id, CancellationToken ct) =>
        RenderAsync(CashierView, id, User.LayTaiKhoanId(), ct);

    [HttpPost("ApprovedExchanges/{id:long}"), ValidateAntiForgeryToken]
    [Authorize(Roles = BangQuyen.ThuNgan, Policy = MaQuyen.DoiTra)]
    public async Task<IActionResult> CompleteExchange(long id, long cashReceived, bool cashConfirmed, CancellationToken ct)
    {
        int cashier = User.LayTaiKhoanId();
        if (!ModelState.IsValid || cashReceived < 0)
            return await RenderAsync(CashierView, id, cashier, ct, "Tiền mặt không hợp lệ.", 422);

        try
        {
            await approvals.CompleteExchangeAsync(id, cashier, cashReceived, cashConfirmed, ct);
            return RedirectToAction("ExchangeReceipt", "ReturnOrders", new { id });
        }
        catch (LoiNghiepVu error) when (error.MaHttp is 404 or 409 or 422)
        {
            return await RenderAsync(CashierView, id, cashier, ct, error.Message, error.MaHttp);
        }
    }

    private async Task<IActionResult> RenderAsync(string view, long id, int? cashier, CancellationToken ct,
        string? message = null, int status = 200)
    {
        try
        {
            var model = await approvals.DetailsAsync(id, cashier, ct);
            ViewData["Error"] = message;
            Response.StatusCode = status;
            return View(view, model);
        }
        catch (LoiNghiepVu error) when (error.MaHttp == 404)
        {
            return NotFound(error.Message);
        }
    }
}

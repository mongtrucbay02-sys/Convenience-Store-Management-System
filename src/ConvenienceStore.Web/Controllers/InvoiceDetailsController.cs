
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

[Authorize(Policy = PosInvoiceAccess.PolicyXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InvoiceDetailsController(
    PosInvoiceService invoices,
    PosInvoiceCancellationService cancellation) : Controller
{
    [HttpGet("Invoices/{id:long}")]
    public async Task<IActionResult> Details(
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest("Mã hóa đơn không hợp lệ.");

        try
        {
            var invoice = await invoices.GetDetailsAsync(
                id,
                PosInvoiceAccess.FromUser(User),
                cancellationToken);

            if (invoice is null)
                return NotFound("Không tìm thấy hóa đơn.");

            bool isAdmin = User.IsInRole("AD");
            bool isCashier = User.IsInRole("TN");
            bool isPaid = invoice.Status == "DA_THANH_TOAN";

            ViewData["CanCancelInvoice"] = isAdmin && isPaid;
            ViewData["ShowPos12Alternative"] = isCashier && isPaid;

            return View("~/Views/Invoices/Details.cshtml", invoice);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // POS-12: Chỉ Admin được hủy hóa đơn đã thanh toán.
    [HttpPost("/Invoices/{id:long}/cancel")]
    [Authorize(Roles = "AD")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        long id,
        [FromForm, Required, StringLength(255)] string? reason,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest("Mã hóa đơn không hợp lệ.");

        reason = reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 255)
        {
            TempData["InvoiceCancelError"] =
                "Vui lòng nhập lý do hủy hóa đơn (tối đa 255 ký tự).";

            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var result = await cancellation.CancelAsync(
                id,
                reason,
                User,
                cancellationToken);

            TempData["InvoiceCancelSuccess"] =
                $"Đã hủy hóa đơn {result.InvoiceCode}. " +
                $"Tiền mặt ghi nhận hoàn: {result.CashRefund:N0} đ.";

            if (result.PointsNotReversed > 0)
            {
                TempData["InvoiceCancelWarning"] =
                    $"Không thể thu hồi {result.PointsNotReversed} điểm " +
                    "do khách đã sử dụng hoặc điều chỉnh. " +
                    "Số dư điểm không bị âm.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (LoiNghiepVu ex)
        {
            TempData["InvoiceCancelError"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

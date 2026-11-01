
using System;
using System.Threading;
using System.Threading.Tasks;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

/// <summary>
/// POS-10: Tra cứu danh sách hóa đơn.
/// BR-POS-11:
/// - Thu ngân chỉ xem hóa đơn do mình lập.
/// - Admin có quyền xem tất cả hóa đơn.
/// </summary>
[Authorize(Policy = PosInvoiceAccess.PolicyXem)]
[Route("Invoices")]
public sealed class InvoicesController : Controller
{
    private readonly PosInvoiceListService _service;

    public InvoicesController(PosInvoiceListService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /Invoices
    /// Hiển thị và lọc danh sách hóa đơn.
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        [FromQuery(Name = "fromDate")] DateOnly? fromDate,
        [FromQuery(Name = "toDate")] DateOnly? toDate,
        [FromQuery(Name = "Status")] string? status,
        [FromQuery(Name = "CashierId")] int? cashierId,
        [FromQuery(Name = "Code")] string? code,
        [FromQuery(Name = "Customer")] string? customer,
        [FromQuery(Name = "page")] int page = 1,
        CancellationToken cancellationToken = default)
    {
        // Ngày hiện tại theo giờ Việt Nam.
        var today = DateOnly.FromDateTime(
            DateTime.UtcNow + GioVietNam.DoLech
        );

        // Thông tin người đăng nhập.
        // Service phải giới hạn dữ liệu theo quyền thực tế.
        var viewer = PosInvoiceAccess.FromUser(User);

        // Kiểm tra khoảng ngày.
        if (fromDate.HasValue &&
            toDate.HasValue &&
            fromDate.Value > toDate.Value)
        {
            ViewData["DateError"] =
                "Ngày bắt đầu không được lớn hơn ngày kết thúc.";

            // Không tự đảo ngày vì sẽ thay đổi bộ lọc của người dùng.
            // Service cần trả kết quả rỗng hoặc xử lý bộ lọc sai.
        }

        // Tránh trang âm hoặc bằng 0.
        if (page < 1)
        {
            page = 1;
        }

        var request = new PosInvoiceListRequest(
            fromDate,
            toDate,
            status,
            cashierId,
            code,
            page,
            customer
        );

        try
        {
            var model = await _service.GetListAsync(
                request,
                viewer,
                today,
                cancellationToken
            );

            if (!ModelState.IsValid)
            {
                model.Items = [];
                model.TotalCount = 0;
                model.PaidRevenue = 0;
                model.CancelledCount = 0;
                model.Error = "Bộ lọc không hợp lệ. Vui lòng kiểm tra ngày, thu ngân và số trang.";
            }

            return View("Index", model);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

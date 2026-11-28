using ConvenienceStore.Web.Modules.Customers.Models;
using ConvenienceStore.Web.Modules.Customers.Services;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Customers.Controllers;

[Authorize(Roles = CustomerRoles.AdminOrCashier)]
public sealed class CustomersController(CustomerService customers, PosCartService cart) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool attachToCart, CancellationToken ct)
    {
        ViewData["Search"] = search;
        ViewData["AttachToCart"] = attachToCart;
        return View("~/Modules/Customers/Views/Customers/Index.cshtml", await customers.SearchAsync(search, User.IsInRole(CustomerRoles.Admin), ct));
    }

    [HttpGet]
    public IActionResult Create(bool attachToCart = false, string? phone = null) =>
        View("~/Modules/Customers/Views/Customers/Edit.cshtml", new CustomerInput { Phone = phone ?? "", AttachToCart = attachToCart });

    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        ViewData["Id"] = id;
        return View("~/Modules/Customers/Views/Customers/Edit.cshtml", await customers.GetInputAsync(id, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(long? id, CustomerInput input, CancellationToken ct)
    {
        ViewData["Id"] = id;
        if (input.AttachToCart && !User.IsInRole(CustomerRoles.Cashier)) return Forbid();
        if (!ModelState.IsValid) return View("~/Modules/Customers/Views/Customers/Edit.cshtml", input);
        try
        {
            // Creation and attachment are separate: if attachment fails, the saved customer is offered for selection.
            var customerId = await customers.SaveAsync(id, input, User.LayTaiKhoanId(), ct);
            if (input.AttachToCart)
            {
                var error = await cart.SetCustomerAsync(customerId);
                if (error != null)
                {
                    TempData["Error"] = "Khách đã được lưu. " + error;
                    return RedirectToAction(nameof(Index), new { search = input.Phone, attachToCart = true });
                }
                return RedirectToAction("Index", "Pos");
            }
            return RedirectToAction(nameof(Index));
        }
        catch (LoiNghiepVu ex)
        {
            if (ex.Ma == MaLoiChung.TrungDuLieu)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { search = input.Phone, attachToCart = input.AttachToCart });
            }
            ModelState.AddModelError("", ex.Message);
            return View("~/Modules/Customers/Views/Customers/Edit.cshtml", input);
        }
    }

    [Authorize(Roles = CustomerRoles.Cashier), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Attach(long? customerId)
    {
        var error = await cart.SetCustomerAsync(customerId);
        TempData[error == null ? "Success" : "Error"] = error ?? "Đã cập nhật khách và khuyến mãi của đơn.";
        return RedirectToAction("Index", "Pos");
    }

    [HttpGet]
    public async Task<IActionResult> History(long id, CancellationToken ct) =>
        View("~/Modules/Customers/Views/Customers/History.cshtml", await customers.HistoryAsync(id, User.IsInRole(CustomerRoles.Admin), ct));

    [Authorize(Roles = CustomerRoles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        try { await customers.DeleteAsync(id, ct); TempData["Success"] = "Đã xóa/ẩn khách hàng và thông tin cá nhân."; }
        catch (LoiNghiepVu ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = CustomerRoles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(long id, PointAdjustment input, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid) throw LoiNghiepVu.DuLieuKhongHopLe("points", "Kiểm tra số điểm, mã hóa đơn và lý do.");
            await customers.AdjustAsync(id, input, User.LayTaiKhoanId(), ct);
            TempData["Success"] = "Đã điều chỉnh điểm và ghi lịch sử.";
        }
        catch (LoiNghiepVu ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(History), new { id });
    }
}

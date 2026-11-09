using ConvenienceStore.Web.Modules.Customers;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

[Authorize(Roles = "AD,TN")]
public sealed class CustomersController(CustomerService customers, PosCartService cart) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool attachToCart, CancellationToken ct)
    {
        ViewData["Search"] = search;
        ViewData["AttachToCart"] = attachToCart;
        return View(await customers.SearchAsync(search, User.IsInRole("AD"), ct));
    }

    [HttpGet]
    public IActionResult Create(bool attachToCart = false, string? phone = null) =>
        View("Edit", new CustomerInput { Phone = phone ?? "", AttachToCart = attachToCart });

    [HttpGet]
    public async Task<IActionResult> Edit(uint id, CancellationToken ct)
    {
        ViewData["Id"] = id;
        return View(await customers.GetInputAsync(id, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(uint? id, CustomerInput input, CancellationToken ct)
    {
        ViewData["Id"] = id;
        if (input.AttachToCart && !User.IsInRole("TN")) return Forbid();
        if (!ModelState.IsValid) return View("Edit", input);
        try
        {
            // Creation and attachment are separate: if attachment fails, the saved customer is offered for selection.
            var customerId = await customers.SaveAsync(id, input, User.LayTaiKhoanId(), ct);
            if (input.AttachToCart)
            {
                var error = await cart.SetCustomerAsync(checked((int)customerId));
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
            return View("Edit", input);
        }
    }

    [Authorize(Roles = "TN"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Attach(int? customerId)
    {
        var error = await cart.SetCustomerAsync(customerId);
        TempData[error == null ? "Success" : "Error"] = error ?? "Đã cập nhật khách và khuyến mãi của đơn.";
        return RedirectToAction("Index", "Pos");
    }

    [HttpGet]
    public async Task<IActionResult> History(uint id, CancellationToken ct) =>
        View(await customers.HistoryAsync(id, User.IsInRole("AD"), ct));

    [Authorize(Roles = "AD"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(uint id, CancellationToken ct)
    {
        try { await customers.DeleteAsync(id, ct); TempData["Success"] = "Đã xóa/ẩn khách hàng và thông tin cá nhân."; }
        catch (LoiNghiepVu ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "AD"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(uint id, PointAdjustment input, CancellationToken ct)
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

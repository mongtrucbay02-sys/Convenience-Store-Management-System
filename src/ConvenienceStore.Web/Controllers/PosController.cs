using System.Globalization;
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

// POS-01: Actor Thu ngân.
// Quyền được kiểm tra ở máy chủ: chỉ tài khoản có quyền BanHang mới vào được.
// Giỏ hàng gắn với tài khoản đăng nhập (BR-POS-03), xem PosCartService.
[Authorize(Policy = MaQuyen.BanHang)]
public sealed class PosController : Controller
{
    private readonly MySqlPosCatalog _catalog;
    private readonly PosCartService _cart;
    private readonly PosPaymentService _posPayment;

    public PosController(
        MySqlPosCatalog catalog,
        PosCartService cart,
        PosPaymentService posPayment)
    {
        _catalog = catalog;
        _cart = cart;
        _posPayment = posPayment;
    }

    // ==========================================
    // POS-01: HIỂN THỊ SẢN PHẨM VÀ GIỎ HÀNG
    // Mở màn hình bán hàng: hệ thống lấy đơn đang bán, chưa có thì tạo đơn trống.
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        if (categoryId.HasValue && categoryId.Value <= 0)
        {
            categoryId = null;
        }

        var model = new PosPageViewModel
        {
            Products = await _catalog.SearchAsync(search, categoryId),
            Categories = await _catalog.GetCategoriesAsync(),
            CategoryId = categoryId,
            Cart = await _cart.GetCartAsync(),
            Search = search ?? ""
        };

        return View(model);
    }

    // ==========================================
    // POS-01: THÊM SẢN PHẨM VÀO GIỎ
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId)
    {
        if (productId <= 0)
        {
            TempData["Error"] = "Sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        // PosCartService kiểm tra sản phẩm, tồn kho và số lượng trong giỏ.
        var error = await _cart.AddAsync(productId);

        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // POS-02: CẬP NHẬT SỐ LƯỢNG
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, string? quantity)
    {
        if (productId <= 0)
        {
            TempData["Error"] = "Sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        // Chỉ chấp nhận số nguyên không âm.
        if (!int.TryParse(
                quantity,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int newQuantity))
        {
            TempData["Error"] =
                "Số lượng không hợp lệ. Vui lòng nhập số nguyên không âm.";

            return RedirectToAction(nameof(Index));
        }

        var error = await _cart.UpdateAsync(productId, newQuantity);

        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = newQuantity == 0
                ? "Đã xóa sản phẩm khỏi giỏ."
                : "Đã cập nhật số lượng sản phẩm.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // POS-02: XÓA SẢN PHẨM KHỎI GIỎ
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        if (productId <= 0)
        {
            TempData["Error"] = "Sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var error = await _cart.UpdateAsync(productId, 0);

        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // TÍNH TIỀN KHÁCH ĐƯA
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(long cashReceived)
    {
        var cart = await _cart.GetCartAsync();

        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        if (cashReceived < 0)
        {
            TempData["Error"] = "Tiền khách đưa không được âm.";
            return RedirectToAction(nameof(Index));
        }

        if (cashReceived < cart.Total)
        {
            TempData["Error"] =
                $"Khách đưa chưa đủ. Cần thêm {cart.Total - cashReceived:N0} đ.";

            return RedirectToAction(nameof(Index));
        }

        var model = new PosPageViewModel
        {
            Products = await _catalog.SearchAsync(null),
            Categories = await _catalog.GetCategoriesAsync(),
            Cart = cart,
            CashReceived = cashReceived,
            Change = cashReceived - cart.Total
        };

        return View("Index", model);
    }

    // ==========================================
    // POS-05: MỞ TRANG THANH TOÁN
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await _cart.GetCartAsync();

        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        var model = new PosCheckoutViewModel
        {
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            Total = cart.Total
        };

        return View("Checkout", model);
    }

    // ==========================================
    // POS-05: MỞ TRANG THANH TOÁN TIỀN MẶT
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Cash()
    {
        var cart = await _cart.GetCartAsync();

        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        var model = new PosCheckoutViewModel
        {
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            Total = cart.Total
        };

        return View("Cash", model);
    }

    // ==========================================
    // POS-05: XÁC NHẬN TIỀN MẶT
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCash(string? cashReceived)
    {
        var cart = await _cart.GetCartAsync();

        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        var model = new PosCheckoutViewModel
        {
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            Total = cart.Total,
            CashInput = cashReceived ?? ""
        };

        if (!long.TryParse(
                cashReceived,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long cash))
        {
            model.Error =
                "Tiền khách đưa không hợp lệ. Vui lòng nhập số nguyên không âm.";

            return View("Cash", model);
        }

        model.CashReceived = cash;

        if (cash < cart.Total)
        {
            model.Error =
                $"Khách đưa chưa đủ. Còn thiếu {cart.Total - cash:N0} đ.";

            return View("Cash", model);
        }

        model.Change = cash - cart.Total;

        // POS-07 tạm thời: chưa lưu hóa đơn, chưa trừ tồn kho, chưa xóa giỏ hàng.
        model.Message = await _posPayment.PrepareAsync(
            cart,
            cash,
            model.Change.Value);

        return View("Cash", model);
    }

    // ==========================================
    // POS-09: HỦY ĐƠN ĐANG BÁN
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel()
    {
        await _cart.CancelAsync();

        TempData["Success"] = "Đã hủy đơn hàng đang bán.";

        return RedirectToAction(nameof(Index));
    }
}
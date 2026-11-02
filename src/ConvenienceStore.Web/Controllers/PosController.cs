using System.Globalization;
using System.Security.Claims;
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

[Authorize(Policy = MaQuyen.BanHang)]
public sealed class PosController : Controller
{
    private readonly MySqlPosCatalog _catalog;
    private readonly PosCartService _cart;
    private readonly PosPaymentService _posPayment;
    private readonly PosCancellationService _cancellation;
    private readonly ILogger<PosController> _logger;

    public PosController(
        MySqlPosCatalog catalog,
        PosCartService cart,
        PosPaymentService posPayment,
        PosCancellationService cancellation,
        ILogger<PosController> logger)
    {
        _catalog = catalog;
        _cart = cart;
        _posPayment = posPayment;
        _cancellation = cancellation;
        _logger = logger;
    }

    // ==========================================
    // POS-01: HIỂN THỊ SẢN PHẨM VÀ GIỎ HÀNG
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId)
    {
        var products = await _catalog.SearchAsync(
            search,
            categoryId);

        return View(new PosPageViewModel
        {
            Products = products,
            Categories = await _catalog.GetCategoriesAsync(),
            CategoryId = categoryId,
            Cart = _cart.GetCart(),
            Search = search ?? ""
        });
    }

    // ==========================================
    // POS-01: THÊM SẢN PHẨM VÀO GIỎ
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId)
    {
        if (!ModelState.IsValid || productId <= 0)
        {
            TempData["Error"] = "Mã sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var error = await _cart.AddAsync(productId);

        TempData[error == null ? "Success" : "Error"] =
            error ?? "Đã thêm sản phẩm vào giỏ.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // POS-02: CHỈNH SỬA SỐ LƯỢNG
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int productId,
        string? quantity)
    {
        if (!ModelState.IsValid || productId <= 0)
        {
            TempData["Error"] = "Mã sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (!int.TryParse(
                quantity,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var newQuantity)
            || newQuantity < 0)
        {
            TempData["Error"] =
                "Số lượng không hợp lệ. " +
                "Vui lòng nhập số nguyên không âm.";

            return RedirectToAction(nameof(Index));
        }

        var error = await _cart.UpdateAsync(
            productId,
            newQuantity);

        TempData[error == null ? "Success" : "Error"] =
            error ?? (
                newQuantity == 0
                    ? "Đã xóa sản phẩm khỏi giỏ."
                    : "Đã cập nhật số lượng sản phẩm.");

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // POS-02: XÓA SẢN PHẨM KHỎI GIỎ
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        if (!ModelState.IsValid || productId <= 0)
        {
            TempData["Error"] = "Mã sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var error = await _cart.UpdateAsync(productId, 0);

        TempData[error == null ? "Success" : "Error"] =
            error ?? "Đã xóa sản phẩm khỏi giỏ.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // TÍNH TIỀN VÀ TIỀN THỪA
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(long cashReceived)
    {
        if (!ModelState.IsValid || cashReceived < 0)
        {
            TempData["Error"] =
                "Tiền khách đưa không hợp lệ.";

            return RedirectToAction(nameof(Index));
        }

        var cart = _cart.GetCart();
        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        if (cashReceived < cart.Total)
        {
            TempData["Error"] =
                $"Khách đưa chưa đủ. " +
                $"Cần thêm {cart.Total - cashReceived:N0} đ.";

            return RedirectToAction(nameof(Index));
        }

        return View("Index", new PosPageViewModel
        {
            Products = await _catalog.SearchAsync(null),
            Categories = await _catalog.GetCategoriesAsync(),
            Cart = cart,
            Search = "",
            CashReceived = cashReceived,
            Change = cashReceived - cart.Total
        });
    }

    // ==========================================
    // POS-05: CHỌN PHƯƠNG THỨC THANH TOÁN
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = _cart.GetCart();
        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        return View("Checkout", new PosCheckoutViewModel
        {
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            Total = cart.Total
        });
    }

    // ==========================================
    // POS-05: MỞ TRANG THANH TOÁN TIỀN MẶT
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Cash()
    {
        var cart = _cart.GetCart();
        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        return View("Cash", new PosCheckoutViewModel
        {
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            Total = cart.Total
        });
    }

    // ==========================================
    // POS-05: XÁC NHẬN SỐ TIỀN KHÁCH ĐƯA
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCash(
        string? cashReceived)
    {
        var cart = _cart.GetCart();
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

        if (!ModelState.IsValid
            || !long.TryParse(
                cashReceived?.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var cash))
        {
            model.Error =
                "Tiền khách đưa không hợp lệ. " +
                "Vui lòng nhập số nguyên không âm.";

            return View("Cash", model);
        }

        model.CashReceived = cash;

        if (cash < cart.Total)
        {
            model.Error =
                $"Khách đưa chưa đủ. " +
                $"Còn thiếu {cart.Total - cash:N0} đ.";

            return View("Cash", model);
        }

        var change = cash - cart.Total;
        model.Change = change;

        // Giữ lời gọi service thanh toán hiện có.
        // Việc lưu hóa đơn và hoàn tất đơn thuộc POS-07.
        model.Message = await _posPayment.PrepareAsync(
            cart,
            cash,
            change);

        return View("Cash", model);
    }

    // ==========================================
    // POS-09: HỦY ĐƠN ĐANG BÁN
    // BR-POS-10: GHI NHẬN LỊCH SỬ HỦY
    // ==========================================

    // Kết hợp với policy BAN_HANG trên controller:
    // người gọi phải có quyền bán hàng và vai trò TN.
    [Authorize(Roles = "TN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel()
    {
        // ID lấy từ phiên đăng nhập, không nhận từ form.
        var claimValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                claimValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var cashierId)
            || cashierId <= 0)
        {
            _logger.LogWarning(
                "POS-09: Từ chối hủy đơn vì phiên đăng nhập " +
                "thiếu ID thu ngân hợp lệ.");

            return Forbid();
        }

        var cart = _cart.GetCart();

        if (cart.Items == null || !cart.Items.Any())
        {
            TempData["Error"] =
                "Giỏ hàng đang trống, không có đơn để hủy.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Giữ đúng chữ ký service bạn đang sử dụng:
            // CancelCurrentAsync(string cashierId, string source).
            var cancelled =
                await _cancellation.CancelCurrentAsync(
                    cashierId.ToString(
                        CultureInfo.InvariantCulture),
                    "POS");

            if (!cancelled)
            {
                TempData["Error"] =
                    "Không tìm thấy đơn đang bán để hủy.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Đã hủy đơn hàng thành công.";

            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException)
            when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "POS-09: Hủy đơn thất bại. " +
                "Thu ngân: {CashierId}.",
                cashierId);

            TempData["Error"] =
                "Không thể hủy đơn lúc này. " +
                "Vui lòng tải lại giỏ hàng và kiểm tra trước khi thử lại.";

            return RedirectToAction(nameof(Index));
        }
    }
}
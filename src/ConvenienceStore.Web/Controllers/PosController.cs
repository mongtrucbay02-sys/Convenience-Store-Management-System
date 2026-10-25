
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Services.Pos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConvenienceStore.Web.Controllers;

public sealed class PosController : Controller
{
    private readonly MySqlPosCatalog _catalog;
    private readonly PosCartService _cart;
    private readonly PosPaymentService _posPayment;
    private readonly PosCancellationService _cancellation;

    public PosController(
        MySqlPosCatalog catalog,
        PosCartService cart,
        PosPaymentService posPayment,
        PosCancellationService cancellation)
    {
        _catalog = catalog;
        _cart = cart;
        _posPayment = posPayment;
        _cancellation = cancellation;
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
            search, categoryId);

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
        if (!int.TryParse(quantity, out int newQuantity)
            || newQuantity < 0)
        {
            TempData["Error"] =
                "Số lượng không hợp lệ. " +
                "Vui lòng nhập số nguyên không âm.";

            return RedirectToAction(nameof(Index));
        }

        var error = await _cart.UpdateAsync(
            productId, newQuantity);

        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] =
                newQuantity == 0
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
    public async Task<IActionResult> Calculate(
        long cashReceived)
    {
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
                $"Khách đưa chưa đủ. Cần thêm " +
                $"{cart.Total - cashReceived:N0} đ.";

            return RedirectToAction(nameof(Index));
        }

        return View("Index", new PosPageViewModel
        {
            Products = await _catalog.SearchAsync(null),
            Categories = await _catalog.GetCategoriesAsync(),
            Cart = cart,
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
    // POS-05: XÁC NHẬN THANH TOÁN TIỀN MẶT
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

        if (!long.TryParse(
                cashReceived,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out long cash))
        {
            model.Error =
                "Tiền khách đưa không hợp lệ. " +
                "Vui lòng nhập số nguyên không âm.";

            return View("Cash", model);
        }

        model.CashReceived = cash;

        // E1: Khách đưa không đủ tiền
        if (cash < cart.Total)
        {
            model.Error =
                $"Khách đưa chưa đủ. Còn thiếu " +
                $"{cart.Total - cash:N0} đ.";

            return View("Cash", model);
        }

        // Tính tiền thừa
        model.Change = cash - cart.Total;

        // Gọi POS-07 tạm thời
        // Chưa lưu hóa đơn, chưa trừ kho
        model.Message = await _posPayment.PrepareAsync(
            cart,
            cash,
            model.Change.Value);

        return View("Cash", model);
    }

    // ==========================================

    // ==========================================
    // POS-09: HỦY ĐƠN HÀNG ĐANG BÁN
    // BR-POS-10: GHI LỊCH SỬ HỦY ĐƠN
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel()
    {
        // BƯỚC 1: Kiểm tra đăng nhập
        if (User.Identity?.IsAuthenticated != true)
        {
            // Chuyển đến trang đăng nhập
            // Yêu cầu nhóm đã triển khai Account/Login
            return Challenge();
        }

        // BƯỚC 2: Kiểm tra quyền Thu ngân
        if (!User.IsInRole("Cashier"))
        {
            return Forbid();
        }

        // BƯỚC 3: Xác định người hủy
        string? cashierId = User.FindFirst(
            ClaimTypes.NameIdentifier
        )?.Value;

        if (string.IsNullOrWhiteSpace(cashierId))
        {
            TempData["Error"] =
                "Không xác định được tài khoản thu ngân.";

            return RedirectToAction(nameof(Index));
        }

        // BƯỚC 4: Kiểm tra giỏ hàng
        var cart = _cart.GetCart();

        if (cart.Items == null || !cart.Items.Any())
        {
            TempData["Error"] =
                "Giỏ hàng đang trống, không có đơn để hủy.";

            return RedirectToAction(nameof(Index));
        }

        // BƯỚC 5: Ghi lịch sử và hủy đơn
        try
        {
            bool cancelled =
                await _cancellation.CancelCurrentAsync(
                    cashierId,
                    "POS"
                );

            if (cancelled)
            {
                TempData["Success"] =
                    "Đã hủy đơn hàng thành công. " +
                    "Hệ thống đã ghi lịch sử hủy " +
                    "và tạo giỏ hàng trống mới.";
            }
            else
            {
                TempData["Error"] =
                    "Không tìm thấy đơn hàng để hủy.";
            }
        }
        catch (Exception)
        {
            // Không xóa giỏ nếu chưa ghi MySQL
            TempData["Error"] =
                "Không thể ghi lịch sử hủy đơn. " +
                "Vui lòng thử lại.";
        }

        // BƯỚC 6: Quay về trang POS
        return RedirectToAction(nameof(Index));
    }
} 
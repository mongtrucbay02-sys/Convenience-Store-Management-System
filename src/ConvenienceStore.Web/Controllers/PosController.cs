
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Services.Pos;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

public sealed class PosController : Controller
{
    private readonly MySqlPosCatalog _catalog;
    private readonly PosCartService _cart;

    // POS-05: Service thanh toán tiền mặt,
    // kết nối POS-07 tạm thời
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
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var products = await _catalog.SearchAsync(search, categoryId);

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
    // POS-02: CHỈNH SỬA SỐ LƯỢNG SẢN PHẨM
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int productId,
        string? quantity)
    {
        // E1: Chặn chữ, số thập phân, giá trị rỗng
        // và số âm ở phía máy chủ
        if (!int.TryParse(quantity, out int newQuantity)
            || newQuantity < 0)
        {
            TempData["Error"] =
                "Số lượng không hợp lệ. Vui lòng nhập số nguyên không âm.";

            return RedirectToAction(nameof(Index));
        }

        // Gọi service cập nhật giỏ hàng
        var error = await _cart.UpdateAsync(
            productId,
            newQuantity);

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
    // GIỮ NGUYÊN HÀM CALCULATE CỦA BẠN
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(long cashReceived)
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
    // POS-05: MỞ TRANG THANH TOÁN TIỀN MẶT
    // ==========================================

    // ==========================================
    // POS-05: BƯỚC 1 - CHỌN PHƯƠNG THỨC THANH TOÁN
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = _cart.GetCart();

        // Kiểm tra giỏ hàng trước khi thanh toán
        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        // Mở trang chọn phương thức thanh toán
        return View("Checkout", new PosCheckoutViewModel
        {
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            Total = cart.Total
        });
    }

    // ==========================================
    // POS-05: BƯỚC 2 - CHỌN THANH TOÁN TIỀN MẶT
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Cash()
    {
        var cart = _cart.GetCart();

        // Kiểm tra giỏ hàng và tồn kho
        var error = await _cart.ValidateCartAsync(cart);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        // Mở trang nhập tiền khách đưa
        return View("Cash", new PosCheckoutViewModel
        {
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            Total = cart.Total
        });
    }

    // ==========================================
    // POS-05: BƯỚC 3, 4, 5
    // NHẬP TIỀN - TÍNH TIỀN THỪA - XÁC NHẬN
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCash(
        string? cashReceived)
    {
        var cart = _cart.GetCart();

        // Kiểm tra giỏ hàng trước khi xác nhận
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

        // Kiểm tra số tiền nhập vào:
        // Không cho nhập chữ, số âm, số thập phân
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

        // ======================================
        // NGOẠI LỆ E1: KHÁCH ĐƯA KHÔNG ĐỦ TIỀN
        // ======================================

        if (cash < cart.Total)
        {
            model.Error =
                $"Khách đưa chưa đủ. Còn thiếu " +
                $"{cart.Total - cash:N0} đ.";

            // Không gọi POS-07 khi thiếu tiền
            return View("Cash", model);
        }

        // ======================================
        // TÍNH TIỀN THỪA
        // ======================================

        model.Change = cash - cart.Total;

        // ======================================
        // BƯỚC 5: GỌI POS-07 TẠM THỜI
        // ======================================

        // Chưa lưu hóa đơn, chưa trừ kho
        // và chưa xóa giỏ hàng
        model.Message = await _posPayment.PrepareAsync(
            cart,
            cash,
            model.Change.Value);

        return View("Cash", model);
    }

    // ==========================================
    // POS-09: HỦY ĐƠN HÀNG ĐANG BÁN
    // GIỮ NGUYÊN CODE CỦA BẠN
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel()
    {
        _cart.Cancel();

        TempData["Success"] = "Đã hủy đơn hàng.";

        return RedirectToAction(nameof(Index));
    }
}

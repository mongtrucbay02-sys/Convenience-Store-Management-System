using System;
using System.Globalization;
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
    private readonly PosCheckoutService _checkoutService;
    private readonly PosInvoiceService _invoiceService;

    // ==========================================
    // CONSTRUCTOR - DEPENDENCY INJECTION
    // ==========================================

    public PosController(
        MySqlPosCatalog catalog,
        PosCartService cart,
        PosPaymentService posPayment,
        PosCheckoutService checkoutService,
        PosInvoiceService invoiceService)
    {
        _catalog = catalog;
        _cart = cart;
        _posPayment = posPayment;
        _checkoutService = checkoutService;
        _invoiceService = invoiceService;
    }

    // ==========================================
    // POS-01: HIỂN THỊ SẢN PHẨM VÀ GIỎ HÀNG
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId)
    {
        if (categoryId.HasValue && categoryId.Value <= 0)
        {
            categoryId = null;
        }

        var model = new PosPageViewModel
        {
            Products = await _catalog.SearchAsync(
                search, categoryId),

            Categories = await _catalog.GetCategoriesAsync(),

            CategoryId = categoryId,
            Cart = await _cart.GetCartAsync(),
            Search = search ?? ""
        };

        return View(model);
    }

    // ==========================================
    // POS-01: THÊM SẢN PHẨM
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

        var error = await _cart.AddAsync(productId);

        TempData[error == null ? "Success" : "Error"] =
            error ?? "Đã thêm sản phẩm vào giỏ.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // POS-02: CẬP NHẬT SỐ LƯỢNG
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int productId,
        string? quantity)
    {
        if (productId <= 0)
        {
            TempData["Error"] = "Sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (!int.TryParse(
                quantity,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int newQuantity))
        {
            TempData["Error"] =
                "Số lượng phải là số nguyên không âm.";

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
            TempData["Success"] = newQuantity == 0
                ? "Đã xóa sản phẩm khỏi giỏ."
                : "Đã cập nhật số lượng sản phẩm.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // POS-02: XÓA SẢN PHẨM
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

        TempData[error == null ? "Success" : "Error"] =
            error ?? "Đã xóa sản phẩm khỏi giỏ.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // TÍNH TIỀN KHÁCH ĐƯA
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(
        long cashReceived)
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
            TempData["Error"] =
                "Tiền khách đưa không được âm.";

            return RedirectToAction(nameof(Index));
        }

        if (cashReceived < cart.Total)
        {
            TempData["Error"] =
                $"Khách đưa chưa đủ. Cần thêm " +
                $"{cart.Total - cashReceived:N0} đ.";

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
    // POS-05: TRANG CHỌN THANH TOÁN
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
    // POS-05: TRANG THANH TOÁN TIỀN MẶT
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
            Total = cart.Total,

            // Lỗi từ CompleteOrder (thiếu tiền, hết tồn, khuyến mãi hết hạn...)
            Error = TempData["Error"] as string
        };

        return View("Cash", model);
    }

    // ==========================================
    // POS-05: XÁC NHẬN TIỀN KHÁCH ĐƯA
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCash(
        string? cashReceived)
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
                "Tiền khách đưa không hợp lệ.";

            return View("Cash", model);
        }

        model.CashReceived = cash;

        if (cash < cart.Total)
        {
            model.Error =
                $"Khách đưa chưa đủ. Còn thiếu " +
                $"{cart.Total - cash:N0} đ.";

            return View("Cash", model);
        }

        model.Change = cash - cart.Total;

        // POS-05 chỉ chuẩn bị thanh toán.
        // Chưa lưu hóa đơn và chưa trừ kho.
        model.Message = await _posPayment.PrepareAsync(
            cart,
            cash,
            model.Change.Value);

        return View("Cash", model);
    }

    // ==========================================
    // POS-07: HOÀN TẤT ĐƠN HÀNG
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteOrder(
        string? cashReceived,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(
                cashReceived,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long cash))
        {
            TempData["Error"] = "Tiền khách đưa không hợp lệ.";
            return RedirectToAction(nameof(Cash));
        }

        // Bước 1-6: sinh mã, lưu hóa đơn, ghi kho, tích điểm, khuyến mãi
        // trong MỘT giao tác. Giá, số lượng, tổng tiền do service đọc lại từ CSDL
        // dưới khóa, không dùng giỏ hàng lấy từ giao diện.
        PosCheckoutResult result;

        try
        {
            result = await _checkoutService.CompleteAsync(
                cash,
                User,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            TempData["Error"] =
                "Yêu cầu bị gián đoạn. " +
                "Hãy kiểm tra hóa đơn trước khi thử lại.";

            return RedirectToAction(nameof(Cash));
        }


        if (!result.Success ||
            result.InvoiceId is null ||
            result.InvoiceId <= 0)
        {
            string errorMessage = result.ErrorMessage
                ?? "Không thể hoàn tất đơn hàng.";

            // E2: Phát hiện thông báo khuyến mãi không còn hợp lệ.
            bool isPromotionError =
                errorMessage.IndexOf(
                    "khuyến mãi",
                    StringComparison.OrdinalIgnoreCase) >= 0
                || errorMessage.IndexOf(
                    "khuyen mai",
                    StringComparison.OrdinalIgnoreCase) >= 0;

            if (isPromotionError)
            {
                TempData["Error"] =
                    errorMessage +
                    " Vui lòng kiểm tra lại khuyến mãi, " +
                    "cập nhật giỏ hàng và xác nhận lại tổng tiền.";

                // Không hoàn tất thanh toán.
                // Không tự động trừ kho.
                // Quay lại giỏ hàng để thu ngân kiểm tra.
                return RedirectToAction(nameof(Index));
            }

            // Giữ nguyên cách xử lý các lỗi khác.
            TempData["Error"] = errorMessage;

            return RedirectToAction(nameof(Cash));
        }


        // Bước 8: tạo đơn trống mới cho thu ngân (đơn cũ đã DA_HOAN_TAT
        // nên không còn đơn DANG_BAN). Không làm hỏng thanh toán nếu lỗi:
        // lần mở màn hình bán hàng kế tiếp sẽ tự tạo lại.
        // KHÔNG gọi CancelAsync ở đây: nó sẽ hủy chính đơn trống vừa tạo.
        try
        {
            await _cart.GetCartAsync();
        }
        catch (Exception)
        {
            // Bỏ qua có chủ đích.
        }

        // Bước 7: trang hóa đơn hiện "Thanh toán thành công" và hỏi "In hóa đơn?"
        TempData["Success"] =
            $"Thanh toán thành công. " +
            $"Mã hóa đơn: {result.InvoiceCode}. " +
            $"Tiền thừa: {result.Change:N0} đ.";

        return RedirectToAction(
            nameof(Receipt),
            new { invoiceId = result.InvoiceId.Value });
    }

    // ==========================================
    // POS-07: XEM VÀ IN HÓA ĐƠN K80
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Receipt(
        long invoiceId,
        CancellationToken cancellationToken)
    {
        if (invoiceId <= 0)
        {
            return BadRequest(
                "Mã hóa đơn không hợp lệ.");
        }

        // Đọc hóa đơn đã lưu từ database
        var invoice = await _invoiceService.GetByIdAsync(
            invoiceId,
            cancellationToken);

        if (invoice == null)
        {
            return NotFound(
                "Không tìm thấy hóa đơn đã thanh toán.");
        }

        // Kiểm tra dữ liệu trước khi in
        if (!_invoiceService.ValidateForPrinting(
                invoice,
                out var error))
        {
            return BadRequest(error);
        }

        // TODO: Kiểm tra quyền truy cập hóa đơn
        // theo cửa hàng/ca bán nếu hệ thống
        // phân tách dữ liệu giữa các thu ngân.

        return View("Receipt", invoice);
    }

    // ==========================================
    // POS-09: HỦY ĐƠN ĐANG BÁN
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel()
    {
        await _cart.CancelAsync();

        TempData["Success"] =
            "Đã hủy đơn hàng đang bán.";

        return RedirectToAction(nameof(Index));
    }
}


using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Services.Pos;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

public sealed class PosController : Controller
{
    private readonly MySqlPosCatalog _catalog;
    private readonly PosCartService _cart;

    public PosController(
        MySqlPosCatalog catalog,
        PosCartService cart)
    {
        _catalog = catalog;
        _cart = cart;
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId)
    {
        var error = await _cart.AddAsync(productId);

        TempData[error == null ? "Success" : "Error"] =
            error ?? "Đã thêm sản phẩm vào giỏ.";

        return RedirectToAction(nameof(Index));
    }

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


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        var error = await _cart.UpdateAsync(productId, 0);

        TempData[error == null ? "Success" : "Error"] =
            error ?? "Đã xóa sản phẩm khỏi giỏ.";

        return RedirectToAction(nameof(Index));
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel()
    {
        _cart.Cancel();

        TempData["Success"] = "Đã hủy đơn hàng.";

        return RedirectToAction(nameof(Index));
    }
}

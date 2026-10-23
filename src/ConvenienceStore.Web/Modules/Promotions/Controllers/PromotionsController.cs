using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Promotions.Controllers;

[Area("Promotions")]
[Authorize(Roles = PromotionRoles.AdminOrCashier)]
public class PromotionsController : Controller
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    private readonly IPromotionService _promotionService;
    private readonly IProductLookup _products;

    public PromotionsController(IPromotionService promotionService, IProductLookup products)
    {
        _promotionService = promotionService;
        _products = products;
    }

    // KM-01: /Promotions/Promotions
    public async Task<IActionResult> Index(
        PromotionDisplayStatus? status,
        DateOnly? from,
        DateOnly? to,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = User.IsInRole(PromotionRoles.Admin);
        var result = await _promotionService.ListAsync(
            new PromotionQuery(status, from, to, page, 20), isAdmin, cancellationToken);

        ViewData["IsAdmin"] = isAdmin;
        ViewData["Status"] = status;
        ViewData["From"] = from;
        ViewData["To"] = to;
        return View(result);
    }

    // KM-02
    [HttpGet]
    [Authorize(Roles = PromotionRoles.Admin)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        await FormView(new PromotionFormModel(), isEdit: false, cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = PromotionRoles.Admin)]
    public async Task<IActionResult> Create(
        PromotionFormModel form, string? submitAction, CancellationToken cancellationToken)
    {
        var result = await _promotionService.CreateAsync(
            new CreatePromotionRequest(
                form.Name, form.Description, form.DiscountType, form.DiscountValue, form.MaxDiscountAmount,
                form.ProductIds, ToVietnamTime(form.StartAt), ToVietnamTime(form.EndAt),
                form.MembersOnly, Activate: submitAction == "activate", form.ImageUrl),
            cancellationToken);

        if (result.IsSuccess)
        {
            TempData["Message"] = $"Đã tạo chương trình {result.Value!.Code}.";
            return RedirectToAction(nameof(Index));
        }

        AddErrors(result.Error!);
        return await FormView(form, isEdit: false, cancellationToken);
    }

    // Chọn sản phẩm: tìm kiếm có phân trang (danh mục có thể có hàng nghìn sản phẩm).
    [HttpGet]
    [Authorize(Roles = PromotionRoles.Admin)]
    public async Task<IActionResult> ProductSearch(
        string? q, int page = 1, CancellationToken cancellationToken = default) =>
        Json(await _products.SearchAsync(q, page, 10, cancellationToken));

    private async Task<IActionResult> FormView(
        PromotionFormModel form, bool isEdit, CancellationToken cancellationToken)
    {
        ViewData["IsEdit"] = isEdit;
        ViewData["Selected"] = await _products.GetByIdsAsync(form.ProductIds, cancellationToken);
        return View("Form", form);
    }

    private void AddErrors(PromotionError error)
    {
        if (error.Fields is null)
        {
            ModelState.AddModelError(string.Empty, error.Title);
            return;
        }

        foreach (var (field, messages) in error.Fields)
        {
            var key = char.ToUpperInvariant(field[0]) + field[1..];
            foreach (var message in messages) ModelState.AddModelError(key, message);
        }
    }

    private static DateTimeOffset? ToVietnamTime(DateTime? local) =>
        local is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(local.Value, DateTimeKind.Unspecified), VietnamOffset);
}

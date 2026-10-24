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

    // KM-03
    [HttpGet]
    [Authorize(Roles = PromotionRoles.Admin)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var result = await _promotionService.GetAsync(id, isAdmin: true, cancellationToken);
        if (!result.IsSuccess) return NotFound();

        var p = result.Value!;
        if (p.DisplayStatus == PromotionDisplayStatus.Ended)
        {
            TempData["Error"] = "Chương trình đã kết thúc, không thể sửa.";
            return RedirectToAction(nameof(Index));
        }

        return await FormView(new PromotionFormModel
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            DiscountType = p.DiscountType,
            DiscountValue = p.DiscountValue,
            MaxDiscountAmount = p.MaxDiscountAmount,
            ProductIds = p.Products.Select(x => x.Id).ToList(),
            StartAt = p.StartAt.ToOffset(VietnamOffset).DateTime,
            EndAt = p.EndAt.ToOffset(VietnamOffset).DateTime,
            MembersOnly = p.MembersOnly,
            ImageUrl = p.ImageUrl,
            UsageCount = p.UsageCount,
        }, isEdit: true, cancellationToken);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = PromotionRoles.Admin)]
    public async Task<IActionResult> Edit(
        long id, PromotionFormModel form, CancellationToken cancellationToken)
    {
        // Form luôn gửi đủ các trường (trường bị khóa sau khi có lượt dùng gửi qua input ẩn).
        // BR-KM-03 được kiểm tra ở máy chủ bằng cách so với giá trị đang lưu.
        var result = await _promotionService.UpdateAsync(
            id,
            new UpdatePromotionRequest(
                form.Name ?? string.Empty, form.Description ?? string.Empty, form.DiscountType,
                form.DiscountValue, form.MaxDiscountAmount ?? 0,
                form.ProductIds.Count > 0 ? form.ProductIds : null,
                ToVietnamTime(form.StartAt), ToVietnamTime(form.EndAt),
                form.MembersOnly, form.ImageUrl ?? string.Empty),
            cancellationToken);

        if (result.IsSuccess)
        {
            TempData["Message"] = $"Đã cập nhật chương trình {result.Value!.Code}.";
            return RedirectToAction(nameof(Index));
        }

        if (result.Error!.Kind == PromotionErrorKind.NotFound) return NotFound();

        form.Id = id;
        AddErrors(result.Error!);
        return await FormView(form, isEdit: true, cancellationToken);
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

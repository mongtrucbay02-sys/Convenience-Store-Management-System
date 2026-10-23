using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Promotions.Controllers;

[Area("Promotions")]
[Authorize(Roles = PromotionRoles.AdminOrCashier)]
public class PromotionsController : Controller
{
    private readonly IPromotionService _promotionService;

    public PromotionsController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
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
}

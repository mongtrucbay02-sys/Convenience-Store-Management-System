using System.ComponentModel.DataAnnotations;
using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Promotions.Controllers;

/// <summary>KM-01. Route tạm theo hợp đồng nháp (chờ #4). AD xem tất cả, TN chỉ xem đang chạy.</summary>
[ApiController]
[Route("api/promotions")]
[Authorize(Roles = PromotionRoles.AdminOrCashier)]
public class PromotionsApiController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionsApiController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PromotionListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionListResponse>> List(
        [FromQuery] PromotionDisplayStatus? displayStatus,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (from is not null && to is not null && from > to)
        {
            ModelState.AddModelError(nameof(to), "`to` phải lớn hơn hoặc bằng `from`.");
            return ValidationProblem(ModelState);
        }

        var result = await _promotionService.ListAsync(
            new PromotionQuery(displayStatus, from, to, page, pageSize),
            User.IsInRole(PromotionRoles.Admin),
            cancellationToken);
        return Ok(result);
    }
}

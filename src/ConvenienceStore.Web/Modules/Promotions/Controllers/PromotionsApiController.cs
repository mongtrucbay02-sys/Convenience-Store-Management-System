using System.ComponentModel.DataAnnotations;
using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Promotions.Controllers;

/// <summary>
/// KM-01 → KM-04. Route tạm theo hợp đồng nháp (chờ #4).
/// Xem: Admin và Thu ngân (TN chỉ thấy chương trình đang chạy). Tạo, sửa, đổi trạng thái: chỉ Admin.
/// </summary>
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

    [HttpGet("{promotionId:long}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(long promotionId, CancellationToken cancellationToken) =>
        ToActionResult(await _promotionService.GetAsync(
            promotionId, User.IsInRole(PromotionRoles.Admin), cancellationToken));

    /// <summary>KM-02.</summary>
    [HttpPost]
    [Authorize(Roles = PromotionRoles.Admin)]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var result = await _promotionService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { promotionId = result.Value!.Id }, result.Value)
            : ToProblem(result.Error!);
    }

    /// <summary>KM-03.</summary>
    [HttpPatch("{promotionId:long}")]
    [Authorize(Roles = PromotionRoles.Admin)]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        long promotionId, [FromBody] UpdatePromotionRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _promotionService.UpdateAsync(promotionId, request, cancellationToken));

    private IActionResult ToActionResult(PromotionResult<PromotionDto> result) =>
        result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error!);

    // Lỗi theo RFC 7807 kèm trường `code` (và `errors` khi sai dữ liệu).
    private IActionResult ToProblem(PromotionError error)
    {
        var status = error.Kind switch
        {
            PromotionErrorKind.Validation => StatusCodes.Status400BadRequest,
            PromotionErrorKind.NotFound => StatusCodes.Status404NotFound,
            PromotionErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        ProblemDetails problem = error.Fields is null
            ? new ProblemDetails { Title = error.Title, Status = status }
            : new ValidationProblemDetails(error.Fields.ToDictionary(k => k.Key, k => k.Value))
            {
                Title = error.Title,
                Status = status,
            };
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = status };
    }
}

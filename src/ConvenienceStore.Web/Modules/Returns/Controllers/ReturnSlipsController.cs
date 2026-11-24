using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Returns.Controllers;

[Authorize(Roles = BangQuyen.Admin + "," + BangQuyen.ThuNgan)]
[RequirePasswordChanged]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Returns/Slips")]
public sealed class ReturnSlipsController(ReturnSlipLookupService lookup) : Controller
{
    private const string Views = "~/Modules/Returns/Views/";

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ReturnSlipQuery query, CancellationToken ct)
    {
        if (!TryGetScope(out var cashier))
            return Forbid();

        if (!ModelState.IsValid || query.From > query.To
            || (!string.IsNullOrEmpty(query.Reason) && !ReturnSlipQuery.Reasons.ContainsKey(query.Reason))
            || (!string.IsNullOrEmpty(query.Status) && !ReturnSlipQuery.Statuses.ContainsKey(query.Status)))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(Views + "SlipList.cshtml", new ReturnSlipList(query, [], false,
                "Bộ lọc không hợp lệ. Kiểm tra ngày, mã phiếu, mã hóa đơn, lý do và trạng thái."));
        }

        return View(Views + "SlipList.cshtml", await lookup.ListAsync(query, cashier, ct));
    }

    [HttpGet("{id:long}")]
    public Task<IActionResult> Details(long id, CancellationToken ct) => RenderSlipAsync(id, false, ct);

    [HttpGet("{id:long}/Print")]
    public Task<IActionResult> Print(long id, CancellationToken ct) => RenderSlipAsync(id, true, ct);

    private async Task<IActionResult> RenderSlipAsync(long id, bool print, CancellationToken ct)
    {
        if (!TryGetScope(out var cashier))
            return Forbid();

        var result = await lookup.FindAsync(id, cashier, ct);
        if (result is null)
            return NotFound("Không tìm thấy phiếu đổi trả.");

        ViewData["Print"] = print;
        return View(Views + "SlipDetails.cshtml", result);
    }

    private bool TryGetScope(out int? cashier)
    {
        cashier = null;
        if (User.IsInRole(BangQuyen.Admin) && User.HasClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.DoiTraDuyetCauHinh))
            return true;
        if (!User.IsInRole(BangQuyen.ThuNgan) || !User.HasClaim(ThongTinXacThuc.ClaimQuyen, MaQuyen.DoiTra))
            return false;

        cashier = User.LayTaiKhoanId();
        return cashier > 0;
    }
}

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Returns.Controllers;

/// <summary>DT-04: tra cứu biên nhận hoàn tiền mặt đã ghi sổ.</summary>
[Authorize(Policy = MaQuyen.DoiTra, Roles = BangQuyen.ThuNgan)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Returns/Refunds")]
public sealed class ReturnRefundController(CuaHangDbContext db, ReturnRefundService refunds) : Controller
{
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Receipt(long id, CancellationToken ct)
    {
        try
        {
            int cashier = User.LayTaiKhoanId();
            var model = await refunds.ReceiptAsync(db, id, cashier, ct);
            return View("~/Modules/Returns/Views/RefundReceipt.cshtml", model);
        }
        catch (LoiNghiepVu e) when (e.MaHttp is 403 or 404)
        {
            return e.MaHttp == 403 ? Forbid() : NotFound(e.Message);
        }
    }
}

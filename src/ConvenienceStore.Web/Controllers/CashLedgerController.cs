using System.ComponentModel.DataAnnotations;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

[Authorize(Roles = "AD")]
public sealed class CashLedgerController(CuaHangDbContext db, CashLedgerService ledger) : Controller
{
    public sealed class OpeningInput
    {
        [Range(0, long.MaxValue)] public long Counted { get; set; }
        [Range(0, long.MaxValue)] public long ExpectedBalance { get; set; }
        [Required, StringLength(500)] public string Reason { get; set; } = "";
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Entries"] = await ledger.GetEntriesAsync(db, ct);
        return View(await ledger.GetBalanceAsync(db, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmOpening(OpeningInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            TempData["Error"] = "Nhập số tiền thực đếm không âm và lý do (tối đa 500 ký tự).";
        else
        {
            try
            {
                await ledger.ConfirmOpeningAsync(db, User.LayTaiKhoanId(), input.Counted, input.ExpectedBalance, input.Reason, ct);
                TempData["Success"] = "Đã chốt số dư mở quỹ và lưu lịch sử.";
            }
            catch (LoiNghiepVu ex) { TempData["Error"] = ex.Message; }
        }
        return RedirectToAction(nameof(Index));
    }
}

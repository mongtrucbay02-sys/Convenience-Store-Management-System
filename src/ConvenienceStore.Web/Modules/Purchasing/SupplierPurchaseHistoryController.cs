using System.Globalization;
using ConvenienceStore.Web.Modules.Auth.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Purchasing;

[Authorize(Policy = MaQuyen.NhapHangLichSuNcc)]
[Route("NhapHang/LichSuNcc")]
public sealed class SupplierPurchaseHistoryController(SupplierPurchaseHistoryService history) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(long? supplierId = null, string? from = null, string? to = null,
        int page = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || supplierId is <= 0 || page is < 1 or > SupplierPurchaseHistory.MaxPage
            || !TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate)
            || (fromDate.HasValue && toDate.HasValue && fromDate > toDate))
            return BadRequest();

        var model = await history.LoadAsync(supplierId, fromDate, toDate, page, ct);
        return model is null ? NotFound() : View("~/Views/Purchasing/SupplierHistory.cshtml", model);
    }

    private static bool TryParseDate(string? value, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrEmpty(value)) return true;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed)) return false;
        date = parsed;
        return true;
    }
}

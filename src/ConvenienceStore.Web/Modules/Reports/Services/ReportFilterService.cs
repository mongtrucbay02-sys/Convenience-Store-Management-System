using System.Globalization;
using ConvenienceStore.Web.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Reports.Services;

public sealed class ReportFilterService(CuaHangDbContext db)
{
    // Include archived entries so administrators can filter historical transactions.
    public async Task<SelectListItem[]> GetCategoriesAsync(int? selected, CancellationToken ct)
    {
        var rows = await db.DanhMucs.AsNoTracking().OrderBy(x => x.Ten).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Ten }).ToListAsync(ct);
        return rows.Select(x => new SelectListItem(x.Ten,
            x.Id.ToString(CultureInfo.InvariantCulture), x.Id == selected)).ToArray();
    }

    public async Task<SelectListItem[]> GetSuppliersAsync(int? selected, CancellationToken ct)
    {
        var rows = await db.NhaCungCaps.AsNoTracking().OrderBy(x => x.Ten).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Ten }).ToListAsync(ct);
        return rows.Select(x => new SelectListItem(x.Ten,
            x.Id.ToString(CultureInfo.InvariantCulture), x.Id == selected)).ToArray();
    }
}

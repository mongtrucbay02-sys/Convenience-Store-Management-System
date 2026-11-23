using System.Globalization;
using ConvenienceStore.Web.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Reports.Services;

public sealed class ReportFilterService(CuaHangDbContext db)
{
    public async Task<SelectListItem[]> GetCashiersAsync(long? selected,CancellationToken ct)
    {
        var rows=await db.TaiKhoans.AsNoTracking().Where(x=>x.VaiTro.Ma=="TN").OrderBy(x=>x.HoTen)
            .Select(x=>new {x.Id,x.HoTen}).ToListAsync(ct);
        return rows.Select(x=>new SelectListItem(x.HoTen,x.Id.ToString(CultureInfo.InvariantCulture),x.Id==selected)).ToArray();
    }
    public async Task<SelectListItem[]> GetPromotionsAsync(long? selected,CancellationToken ct)
    {
        var rows=await db.ChuongTrinhKhuyenMais.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new {x.Id,x.Ma,x.Ten}).ToListAsync(ct);
        return rows.Select(x=>new SelectListItem(x.Ma+" · "+x.Ten,x.Id.ToString(CultureInfo.InvariantCulture),x.Id==selected)).ToArray();
    }
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

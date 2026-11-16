using ConvenienceStore.Web.Modules.Inventory.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Inventory.Controllers;

public sealed class StockAlertLinkViewComponent(IDichVuKho stock) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = await stock.DemSanPhamDuoiNguongAsync(HttpContext.RequestAborted);
        return View(count);
    }
}

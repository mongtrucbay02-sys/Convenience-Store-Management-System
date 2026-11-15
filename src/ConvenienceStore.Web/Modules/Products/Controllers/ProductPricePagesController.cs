using System.Data.Common;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductPricePagesController(ProductDetailService service, ILogger<ProductPricePagesController> logger) : Controller
{
    [HttpGet("/Products/{id}/Prices")]
    public async Task<IActionResult> Index(uint id, [FromQuery] ProductPricesQuery query, CancellationToken ct)
    {
        if (User.DangPhaiDoiMatKhau()) return Forbid();
        ViewData["ProductId"] = id; ViewData["ScheduledOnly"] = query.DaLenLich;
        try
        {
            if (!ModelState.IsValid) return Error(400, "Bộ lọc không đúng kiểu dữ liệu.");
            return View("~/Modules/Products/Views/Management/Prices.cshtml", await service.PricesAsync(id, query, User.LayMaVaiTro(), ct));
        }
        catch (LoiNghiepVu ex) { return Error(ex.MaHttp, ex.Message); }
        catch (DbException ex) { logger.LogError(ex, "Không tải được giá sản phẩm {ProductId}", id); return Error(503, "Chưa tải được giá. Vui lòng thử lại."); }
    }

    private IActionResult Error(int status, string message)
    {
        Response.StatusCode = status; ViewData["PriceError"] = message;
        return View("~/Modules/Products/Views/Management/Prices.cshtml", new PagedResultDto<ProductPriceDto>([], 0, 1, 20));
    }
}

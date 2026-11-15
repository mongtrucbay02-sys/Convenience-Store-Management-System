using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ApiController, Route("api/v1/products/{id}")]
public sealed class ProductDetailsController(ProductDetailService service) : ControllerBase
{
    /// <summary>SP-02 Product details; cost/history restricted to AD.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(uint id, CancellationToken ct) => Ok(await service.FindAsync(id, User.LayMaVaiTro(), ct));

    /// <summary>SP-02 Bounded scheduled prices, or AD-only price history.</summary>
    [HttpGet("prices")]
    public async Task<IActionResult> Prices(uint id, [FromQuery] ProductPricesQuery query, CancellationToken ct)
    {
        if (Request.Query.Keys.Any(x => !new[] { "page", "size", "daLenLich" }.Contains(x, StringComparer.OrdinalIgnoreCase)))
            throw LoiNghiepVu.DuLieuKhongHopLe("query", "Bộ lọc không được hỗ trợ.");
        return Ok(await service.PricesAsync(id, query, User.LayMaVaiTro(), ct));
    }
}

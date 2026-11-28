using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[ApiController, Route("api/v1/products/{id}/prices")]
[Tags("SP")]
[Authorize(Policy = MaQuyen.SanPhamGiaDanhMuc)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductPricingController(ProductPricingService service) : ControllerBase
{
    /// <summary>SP-05 Read one price and its product version for cancellation.</summary>
    [HttpGet("{priceId}")]
    public async Task<IActionResult> Get(long id, long priceId, CancellationToken ct) => Ok(await service.FindAsync(id, priceId, User.LayMaVaiTro(), ct));
    /// <summary>SP-05 Append an immediate or scheduled price with audit metadata.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(long id, ProductPriceChangeDto input, CancellationToken ct)
    {
        var row = await service.CreateAsync(id, input, checked((long)User.LayTaiKhoanId()), User.LayMaVaiTro(), ct);
        return Created($"/api/v1/products/{id}/prices/{row.Id}", row);
    }
    /// <summary>SP-05 Cancel an unexpired scheduled price, preserving history.</summary>
    [HttpPost("{priceId}/cancel")]
    public async Task<IActionResult> Cancel(long id, long priceId, ProductPriceCancelDto input, CancellationToken ct) => Ok(await service.CancelAsync(id, priceId, input, checked((long)User.LayTaiKhoanId()), User.LayMaVaiTro(), ct));
}

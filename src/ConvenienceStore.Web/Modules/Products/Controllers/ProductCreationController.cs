using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[ApiController, Route("api/v1/products")]
[Authorize(Policy = MaQuyen.SanPhamSua)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductCreationController(ProductCreationService service) : ControllerBase
{
    /// <summary>SP-03 Create a product with initial price and zero inventory atomically.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(ProductCreateDto input, CancellationToken ct)
    {
        var row = await service.CreateAsync(input, checked((uint)User.LayTaiKhoanId()), User.LayMaVaiTro(), ct);
        return Created("/api/v1/products/" + row.Id, row);
    }
}

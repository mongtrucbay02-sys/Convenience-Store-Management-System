using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[ApiController, Route("api/v1/products/{id}")]
[Authorize(Policy = MaQuyen.SanPhamSua)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductEditingController(ProductEditService service) : ControllerBase
{
    /// <summary>SP-04 Edit metadata/threshold with optimistic concurrency; price and code are immutable here.</summary>
    [HttpPut]
    public async Task<IActionResult> Update(uint id, ProductEditDto input, CancellationToken ct) => Ok(await service.UpdateAsync(id, input, User.LayMaVaiTro(), ct));
}

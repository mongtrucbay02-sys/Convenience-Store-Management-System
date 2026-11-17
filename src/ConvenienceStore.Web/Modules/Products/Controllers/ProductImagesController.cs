using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[ApiController, Route("api/v1/products/{id}/image")]
[Tags("SP")]
[Authorize(Policy = MaQuyen.SanPhamSua)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductImagesController(ProductImageService images) : ControllerBase
{
    /// <summary>SP-03/SP-04 Upload a JPG/PNG image with optimistic concurrency.</summary>
    [HttpPost, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Upload(uint id, [FromForm] IFormFile? image, [FromForm] uint phienBan, CancellationToken ct) =>
        Ok(await images.SetAsync(id, phienBan, image, User.LayMaVaiTro(), ct));
}

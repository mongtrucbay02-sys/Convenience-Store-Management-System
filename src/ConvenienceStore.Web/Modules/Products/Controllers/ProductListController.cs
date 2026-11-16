using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy = MaQuyen.SanPhamXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ApiController, Route("api/v1/products")]
[Tags("SP")]
public sealed class ProductListController(ProductReadService service) : ControllerBase
{
    /// <summary>SP-01 Management product list, search and filters.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ProductListQuery query, CancellationToken ct)
    {
        ValidateKeys(Request.Query.Keys);
        return Ok(await service.ListAsync(query, User.LayMaVaiTro(), ct));
    }

    internal static void ValidateKeys(IEnumerable<string> keys)
    {
        var invalid = keys.Where(x => !ProductListQuery.AllowedParameters.Contains(x)).Select(x => new LoiTruong(x, "Bộ lọc không được hỗ trợ.")).ToArray();
        if (invalid.Length > 0) throw LoiNghiepVu.DuLieuKhongHopLe(invalid);
    }
}

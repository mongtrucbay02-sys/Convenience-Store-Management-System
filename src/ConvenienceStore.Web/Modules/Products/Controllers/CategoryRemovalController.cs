using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[ApiController, Route("api/v1/categories/{id}")]
[Tags("SP")]
[Authorize(Policy = MaQuyen.SanPhamGiaDanhMuc)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoryRemovalController(CategoryRemovalService service) : ControllerBase
{
    /// <summary>SP-09 Preview product/document references before removal.</summary>
    [HttpGet("removal")]
    public async Task<IActionResult> Preview(uint id, CancellationToken ct) => Ok(await service.PreviewAsync(id, ct) ?? throw LoiNghiepVu.KhongTimThay("Danh mục không còn tồn tại."));
    /// <summary>SP-09 Delete an empty, unreferenced category after confirmation.</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(uint id, CategoryRemovalCommand input, CancellationToken ct) { Require(await service.DeleteAsync(id, input, ct), id); return NoContent(); }
    /// <summary>SP-09 Hide a category without deleting products or history.</summary>
    [HttpPost("hide")]
    public async Task<IActionResult> Hide(uint id, CategoryRemovalCommand input, CancellationToken ct) => Ok(Require(await service.HideAsync(id, input, ct), id));
    internal static CategoryRemovalPreviewDto? Require(CategoryRemovalResult result, uint id) => result.Status switch {
        CategoryRemovalStatus.Success => result.Preview,
        CategoryRemovalStatus.Invalid => throw LoiNghiepVu.DuLieuKhongHopLe("id", "ID và phiên bản phải lớn hơn 0."),
        CategoryRemovalStatus.Conflict => throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, ThongBaoLoi.XungDotDuLieu),
        CategoryRemovalStatus.Referenced => throw LoiNghiepVu.XungDot("DANH_MUC_DANG_SU_DUNG", "Danh mục còn tham chiếu. Hãy chuyển sản phẩm sang danh mục khác hoặc ẩn danh mục.", new { result.Preview?.SoSanPham, result.Preview?.SoSanPhamDangKinhDoanh, result.Preview?.CoPhieuKiemKe, chuyenSanPhamUrl = $"/Products?danhMucId={id}" }),
        _ => throw LoiNghiepVu.KhongTimThay("Danh mục không còn tồn tại.")
    };
}

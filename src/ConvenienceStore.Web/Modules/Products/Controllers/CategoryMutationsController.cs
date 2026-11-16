using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[ApiController,Route("api/v1/categories")]
[Tags("SP")]
[Authorize(Policy=MaQuyen.SanPhamGiaDanhMuc)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CategoryMutationsController(CategoryMutationService service):ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(uint id,CancellationToken ct) => Ok(await service.FindAsync(id,ct)??throw LoiNghiepVu.KhongTimThay("Danh mục không còn tồn tại."));
    [HttpPost]
    public async Task<IActionResult> Create(CategoryMutationDto input,CancellationToken ct)
    {var row=Require(await service.CreateAsync(input,ct));return CreatedAtAction(nameof(Get),new{id=row.Id},row);}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(uint id,CategoryUpdateDto input,CancellationToken ct) => Ok(Require(await service.UpdateAsync(id,input,ct)));
    internal static CategoryDetailsDto Require(CategoryMutationResult result) => result.Status switch
    {
        CategoryMutationStatus.Success=>result.Category!,
        CategoryMutationStatus.Invalid=>throw LoiNghiepVu.DuLieuKhongHopLe(result.Error?.StartsWith("MoTa")==true?"moTa":result.Error?.Contains("PhienBan")==true?"phienBan":"ten",result.Error??"Dữ liệu không hợp lệ."),
        CategoryMutationStatus.Duplicate=>throw LoiNghiepVu.XungDot(MaLoiChung.TrungDuLieu,"Tên danh mục đã tồn tại."),
        CategoryMutationStatus.Conflict=>throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu,ThongBaoLoi.XungDotDuLieu),
        _=>throw LoiNghiepVu.KhongTimThay("Danh mục không còn tồn tại.")
    };
}

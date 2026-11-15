using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Products.Models;
using ConvenienceStore.Web.Modules.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Authorize(Policy=MaQuyen.SanPhamGiaDanhMuc)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CategoryEditorController(CategoryMutationService service):Controller
{
    private const string Template="~/Modules/Products/Views/Categories/Edit.cshtml";
    [HttpGet("/Categories/Create")]
    public IActionResult Create()=>User.DangPhaiDoiMatKhau()?Forbid():View(Template,new CategoryEditModel());
    [HttpGet("/Categories/{id}/Edit")]
    public async Task<IActionResult> Edit(uint id,CancellationToken ct)
    {
        if(User.DangPhaiDoiMatKhau())return Forbid();var x=await service.FindAsync(id,ct);if(x is null)return NotFound();
        return View(Template,new CategoryEditModel{Id=x.Id,Ten=x.Ten,MoTa=x.MoTa,KhongDoiTra=x.KhongDoiTra,PhienBan=x.PhienBan});
    }
    [HttpPost("/Categories/Create"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Ten,MoTa,KhongDoiTra")]CategoryEditModel model,CancellationToken ct)
    {
        if(User.DangPhaiDoiMatKhau())return Forbid();if(!ModelState.IsValid){Response.StatusCode=422;return View(Template,model);}var r=await service.CreateAsync(new(model.Ten,model.MoTa,model.KhongDoiTra),ct);return Complete(model,r);
    }
    [HttpPost("/Categories/{id}/Edit"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(uint id,[Bind("Ten,MoTa,KhongDoiTra,PhienBan")]CategoryEditModel model,CancellationToken ct)
    {if(User.DangPhaiDoiMatKhau())return Forbid();model.Id=id;if(!ModelState.IsValid){Response.StatusCode=422;return View(Template,model);}var r=await service.UpdateAsync(id,new(model.Ten,model.MoTa,model.KhongDoiTra,model.PhienBan),ct);return Complete(model,r);}
    private IActionResult Complete(CategoryEditModel model,ConvenienceStore.Web.Modules.Products.DTOs.CategoryMutationResult r)
    {
        if(r.Status==ConvenienceStore.Web.Modules.Products.DTOs.CategoryMutationStatus.Success)return Redirect("/Categories");
        Response.StatusCode=r.Status==ConvenienceStore.Web.Modules.Products.DTOs.CategoryMutationStatus.NotFound?404:r.Status==ConvenienceStore.Web.Modules.Products.DTOs.CategoryMutationStatus.Invalid?422:409;
        ModelState.AddModelError(string.Empty,r.Error??(r.Status==ConvenienceStore.Web.Modules.Products.DTOs.CategoryMutationStatus.Conflict?"Dữ liệu đã thay đổi. Tải lại trước khi lưu.":"Tên trùng hoặc danh mục không tồn tại."));
        return View(Template,model);
    }
}

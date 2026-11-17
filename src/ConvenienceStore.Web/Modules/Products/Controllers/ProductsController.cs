using Microsoft.AspNetCore.Mvc;
using ConvenienceStore.Web.Modules.Auth.Security;
using Microsoft.AspNetCore.Authorization;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Area("Products")]
[Authorize(Policy = MaQuyen.SanPhamXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProductsController : Controller
{
    // Keep old module bookmarks on the authenticated management workflow.
    [HttpGet]
    public IActionResult Index() => User.DangPhaiDoiMatKhau() ? Forbid() : Redirect("/Products");
}

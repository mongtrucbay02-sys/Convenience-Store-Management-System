using Microsoft.AspNetCore.Mvc;
using ConvenienceStore.Web.Modules.Products.Services;

namespace ConvenienceStore.Web.Modules.Products.Controllers;

[Area("Products")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public IActionResult Index()
    {
        ViewData["Message"] = _productService.GetWelcomeMessage();
        return View();
    }
}

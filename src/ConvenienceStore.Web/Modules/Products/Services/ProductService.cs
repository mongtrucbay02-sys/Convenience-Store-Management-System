namespace ConvenienceStore.Web.Modules.Products.Services;

public class ProductService : IProductService
{
    public string GetWelcomeMessage()
    {
        return "Products module is running!";
    }
}

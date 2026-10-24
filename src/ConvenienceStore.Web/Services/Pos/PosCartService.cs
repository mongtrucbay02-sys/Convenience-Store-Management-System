
using System.Text.Json;
using ConvenienceStore.Web.Models.Pos;

namespace ConvenienceStore.Web.Services.Pos;

public sealed class PosCartService
{
    private const string Key = "SMARTMART_POS_CART";

    private readonly IHttpContextAccessor _http;
    private readonly MySqlPosCatalog _catalog;

    public PosCartService(
        IHttpContextAccessor http,
        MySqlPosCatalog catalog)
    {
        _http = http;
        _catalog = catalog;
    }

    private ISession Session =>
        _http.HttpContext?.Session
        ?? throw new InvalidOperationException(
            "Session chưa được bật.");

    public PosCart GetCart()
    {
        var json = Session.GetString(Key);

        return string.IsNullOrWhiteSpace(json)
            ? new PosCart()
            : JsonSerializer.Deserialize<PosCart>(json)
              ?? new PosCart();
    }

    private void Save(PosCart cart)
    {
        Session.SetString(Key, JsonSerializer.Serialize(cart));
    }

    public async Task<string?> AddAsync(int productId)
    {
        var product = await _catalog.FindAsync(productId);

        if (product == null)
            return "Không tìm thấy sản phẩm.";

        if (product.Price <= 0 || product.Price % 100 != 0)
            return "Giá bán sản phẩm không hợp lệ.";

        var cart = GetCart();

        var item = cart.Items.FirstOrDefault(
            x => x.ProductId == productId);

        var quantity = (item?.Quantity ?? 0) + 1;

        if (quantity > product.Stock)
            return $"Chỉ còn {product.Stock} sản phẩm.";

        if (item == null)
        {
            cart.Items.Add(new PosCartItem
            {
                ProductId = product.Id,
                Code = product.Code,
                Name = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });
        }
        else
        {
            item.Quantity++;
        }

        Save(cart);
        return null;
    }

    public async Task<string?> UpdateAsync(
     int productId, int quantity)
    {
        var cart = GetCart();

        var item = cart.Items.FirstOrDefault(
            x => x.ProductId == productId);

        if (item == null)
            return "Sản phẩm không có trong giỏ.";

        if (quantity < 0)
        {
            return "Số lượng không hợp lệ.";
        }

        if (quantity == 0)
        {
            cart.Items.Remove(item);
            Save(cart);
            return null;
        }

        var product = await _catalog.FindAsync(productId);

        if (product == null)
            return "Sản phẩm không còn kinh doanh.";

        if (quantity > product.Stock)
            return $"Chỉ còn {product.Stock} sản phẩm.";

        item.Quantity = quantity;

        Save(cart);
        return null;
    }

    public async Task<string?> ValidateCartAsync(PosCart cart)
    {
        if (cart.Items.Count == 0)
            return "Giỏ hàng đang trống.";

        foreach (var item in cart.Items)
        {
            var product = await _catalog.FindAsync(item.ProductId);

            if (product == null)
                return $"Sản phẩm {item.Name} không còn kinh doanh.";

            if (item.Quantity <= 0 || item.Quantity > product.Stock)
                return $"Chỉ còn {product.Stock} sản phẩm {item.Name}.";

            if (item.UnitPrice <= 0 || item.UnitPrice % 100 != 0)
                return $"Giá sản phẩm {item.Name} không hợp lệ.";
        }

        return null;
    }

    public void Cancel()
    {
        Session.Remove(Key);
    }
}

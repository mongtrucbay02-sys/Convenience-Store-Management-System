namespace ConvenienceStore.Web.Models.Pos;

public sealed class PosProduct
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public long Price { get; set; }
    public int Stock { get; set; }
}

public sealed class PosCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class PosCartItem
{
    public int ProductId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long Total => checked(UnitPrice * Quantity);
}


public sealed class PosCart
{
    public List<PosCartItem> Items { get; set; } = new();

    // Tổng tiền hàng trước giảm giá
    public long Subtotal => Items.Sum(x => x.Total);

    // Tuần 4 chưa tích hợp POS-04
    public long Discount => 0;

    // Tổng tiền khách phải trả
    public long Total => Subtotal - Discount;

    // Tổng số lượng sản phẩm trong giỏ
    public int TotalQuantity => Items.Sum(x => x.Quantity);
}


public sealed class PosPageViewModel
{
    public List<PosProduct> Products { get; set; } = new();
    public List<PosCategory> Categories { get; set; } = new();
    public PosCart Cart { get; set; } = new();
    public string Search { get; set; } = "";
    public int? CategoryId { get; set; }
    public long Subtotal => Cart.Subtotal;
    public long Discount => Cart.Discount;
    public long Total => Cart.Total;
    public long? CashReceived { get; set; }
    public long? Change { get; set; }
}

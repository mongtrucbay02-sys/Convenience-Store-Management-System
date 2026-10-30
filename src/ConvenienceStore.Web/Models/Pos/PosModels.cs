
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvenienceStore.Web.Models.Pos;

// ==========================================
// THÔNG TIN SẢN PHẨM
// ==========================================

public sealed class PosProduct
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public long Price { get; set; }
    public int Stock { get; set; }
}

// ==========================================
// DANH MỤC SẢN PHẨM
// ==========================================

public sealed class PosCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// ==========================================
// CHI TIẾT SẢN PHẨM TRONG GIỎ
// ==========================================

public sealed class PosCartItem
{
    public int ProductId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    // Giá bán một sản phẩm
    public long UnitPrice { get; set; }

    // Số lượng sản phẩm
    public int Quantity { get; set; }

    // Mã chương trình khuyến mãi
    public int? PromotionId { get; set; }

    // Số tiền giảm trên một sản phẩm
    public long UnitDiscount { get; set; }

    // Tổng tiền trước giảm giá
    public long Subtotal =>
        checked(UnitPrice * Quantity);

    // Tổng tiền giảm giá
    public long Discount =>
        checked(UnitDiscount * Quantity);

    // Tổng tiền sau giảm giá
    public long Total =>
        checked(Subtotal - Discount);
}

// ==========================================
// GIỎ HÀNG POS
// ==========================================

public sealed class PosCart
{
    public List<PosCartItem> Items { get; set; } = new();

    // Tổng tiền hàng trước giảm giá
    public long Subtotal =>
        checked(Items.Sum(x => x.Subtotal));

    // Tổng tiền khuyến mãi
    public long Discount =>
        checked(Items.Sum(x => x.Discount));

    // Tổng tiền khách phải thanh toán
    public long Total =>
        checked(Subtotal - Discount);

    // Tổng số lượng sản phẩm trong giỏ
    public int TotalQuantity =>
        checked(Items.Sum(x => x.Quantity));
}

// ==========================================
// MODEL TRANG BÁN HÀNG
// ==========================================

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

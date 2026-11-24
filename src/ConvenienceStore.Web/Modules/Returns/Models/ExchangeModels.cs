using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConvenienceStore.Web.Modules.Returns.Models;

// =====================================================
// REQUEST: Dữ liệu thu ngân gửi lên khi thực hiện đổi hàng
// =====================================================

public sealed class ExchangeLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public string Reason { get; set; } = "LOI_SAN_PHAM";

    [StringLength(255)]
    public string? Note { get; set; }

    [StringLength(255)]
    public string Condition { get; set; } = string.Empty;

    [Required]
    public string Handling { get; set; } = "NHAP_LAI";
}


public sealed class ExchangeRequest
{
    [Range(1, long.MaxValue)]
    public long InvoiceId { get; set; }

    public List<ExchangeLineRequest> Lines { get; set; } = new();

    [StringLength(128)]
    public string? ConfirmationToken { get; set; }

    [Range(0, long.MaxValue)]
    public long CashReceived { get; set; }

    public bool CashConfirmed { get; set; }
}


// =====================================================
// MODEL: Sản phẩm khách chọn mua mới trong phiếu đổi
// =====================================================

public sealed record ExchangeCartLine(
    int ProductId,
    string Name,
    string Unit,
    int Quantity,
    long UnitPrice,
    long Discount,
    string PromotionName,
    long LineTotal
);


// =====================================================
// MODEL: Chi tiết một dòng hàng khách trả
// =====================================================

public sealed record ExchangePreviewLine(
    int ProductId,
    string Name,
    int Quantity,
    long UnitPaid,
    string Reason,
    string? Note,
    string Condition,
    string Handling,
    bool Exception,
    decimal Cost
);


// =====================================================
// MODEL: Kết quả xem trước phiếu đổi hàng
// =====================================================

public sealed record ExchangePreview(
    long InvoiceId,
    string InvoiceCode,
    long ReturnValue,
    long NewInvoiceTotal,
    long ExchangeCredit,
    long CashDue,
    long CashReceived,
    long Change,
    bool Expired,
    bool Forbidden,
    bool OverThreshold,
    bool NeedsApproval,
    string ConfirmationToken,
    IReadOnlyList<ExchangePreviewLine> ReturnLines,
    IReadOnlyList<ExchangeCartLine> NewItems
);


// =====================================================
// MODEL: Phiếu đổi hàng sau khi hoàn tất
// =====================================================

public sealed record ExchangeReceipt(
    long Id,
    string Code,
    long OriginalInvoiceId,
    string OriginalInvoiceCode,
    long NewInvoiceId,
    string NewInvoiceCode,
    string Status,
    long ReturnValue,
    long NewInvoiceTotal,
    long ExchangeCredit,
    long CashReceived,
    long CashDue,
    long Change,
    int PointsRecovered,
    int PointsEarned,
    DateTimeOffset CreatedAt,
    string Cashier,
    IReadOnlyList<ExchangePreviewLine> ReturnLines
);


// =====================================================
// VIEW MODEL: Trang giao diện đổi hàng DT-03
// =====================================================

public sealed class ExchangePage
{
    public ReturnForm? Invoice { get; set; }

    public ExchangeRequest Request { get; set; } = new();

    public ExchangePreview? Preview { get; set; }

    public ExchangeReceipt? Receipt { get; set; }

    public string? Error { get; set; }

    public IReadOnlyList<ExchangeCartLine> NewItems { get; set; }
        = new List<ExchangeCartLine>();
}
using System.ComponentModel.DataAnnotations;

namespace ConvenienceStore.Web.Modules.Returns.Models;

public sealed class ReturnSlipQuery
{
    [StringLength(20)] public string? Code { get; init; }
    [StringLength(20)] public string? InvoiceCode { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? Reason { get; init; }
    public string? Status { get; init; }
    [Range(1, 1000000)] public int Page { get; init; } = 1;

    public static IReadOnlyDictionary<string, string> Reasons { get; } = new Dictionary<string, string>
    {
        ["LOI_SAN_PHAM"] = "Lỗi sản phẩm",
        ["HET_HAN"] = "Hết hạn",
        ["KHONG_DUNG_NHU_CAU"] = "Không đúng nhu cầu",
        ["KHAC"] = "Khác"
    };

    public static IReadOnlyDictionary<string, string> Statuses { get; } = new Dictionary<string, string>
    {
        ["CHO_DUYET"] = "Chờ duyệt",
        ["DA_DUYET"] = "Đã duyệt",
        ["HOAN_TAT"] = "Hoàn tất",
        ["BI_TU_CHOI"] = "Bị từ chối"
    };
}

public sealed record ReturnSlipRow(long Id, string Code, string Kind, string InvoiceCode,
    DateTimeOffset CreatedAt, string CashierName, long Refund, long CashDue, string Status)
{
    public string KindLabel => Kind == "DOI_HANG" ? "Đổi hàng" : "Trả hàng";
    public string StatusLabel => ReturnSlipQuery.Statuses.GetValueOrDefault(Status, Status);
}

public sealed record ReturnSlipLine(string ProductName, int Quantity, long UnitPaid, string Reason,
    string? Note, string? Condition, string Handling)
{
    public string ReasonLabel => ReturnSlipQuery.Reasons.GetValueOrDefault(Reason, Reason);
}

public sealed record ReturnSlipDetails(ReturnSlipRow Slip, string? NewInvoiceCode, long ReturnValue,
    int RecoveredPoints, string? ApproverName, DateTimeOffset? ApprovedAt, string? ApprovalNote,
    DateTimeOffset? CompletedAt, IReadOnlyList<ReturnSlipLine> Lines);

public sealed record ReturnSlipList(ReturnSlipQuery Query, IReadOnlyList<ReturnSlipRow> Items,
    bool HasNext, string? Error = null);

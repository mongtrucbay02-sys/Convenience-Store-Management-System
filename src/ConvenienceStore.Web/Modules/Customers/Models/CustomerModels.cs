using System.ComponentModel.DataAnnotations;

namespace ConvenienceStore.Web.Modules.Customers.Models;

public sealed class CustomerInput
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, RegularExpression("^0[0-9]{9}$")] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [DataType(DataType.Date)] public DateTime? BirthDate { get; set; }
    [RegularExpression("^(NAM|NU|KHAC)$")] public string? Gender { get; set; }
    public long Version { get; set; }
    public bool AttachToCart { get; set; }
}

public sealed record CustomerRow(long Id, string Name, string Phone, long Points,
    long Spending, DateTime? LastPurchase, long Version);
public sealed record CustomerInvoice(long Id, string Code, DateTime PaidAt, long Total, long Points);
public sealed record CustomerPoint(long Id, string Type, int Points, long Balance, string? Reason, DateTime At);
public sealed record CustomerHistory(CustomerRow Customer, List<CustomerInvoice> Invoices, List<CustomerPoint> Points);
public sealed class PointAdjustment
{
    [Range(int.MinValue, int.MaxValue)] public int Points { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
    public long? InvoiceId { get; set; }
}

public static class CustomerRules
{
    public static string MaskPhone(string? phone) => phone is { Length: 10 }
        ? phone[..3] + "****" + phone[7..] : "";
    public static int EarnedPoints(long paid, int? amountPerPoint, bool enabled = true)
    {
        if (paid < 0 || amountPerPoint is <= 0) throw new ArgumentOutOfRangeException(nameof(paid));
        return enabled ? checked((int)(paid / (amountPerPoint ?? 10000))) : 0;
    }
    public static long AdjustedBalance(long balance, int points)
    {
        if (balance < 0 || points == 0 || (points < 0 && balance < -(long)points)
            || (points > 0 && balance > long.MaxValue - points))
            throw new ArgumentOutOfRangeException(nameof(points), "Điểm điều chỉnh phải khác 0 và không làm số dư âm hoặc vượt giới hạn.");
        return balance + points;
    }
}

using System.ComponentModel.DataAnnotations;

namespace ConvenienceStore.Web.Modules.Customers;

public sealed class CustomerInput
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, RegularExpression("^0[0-9]{9}$")] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [DataType(DataType.Date)] public DateTime? BirthDate { get; set; }
    [RegularExpression("^(NAM|NU|KHAC)$")] public string? Gender { get; set; }
    public uint Version { get; set; }
    public bool AttachToCart { get; set; }
}

public sealed record CustomerRow(uint Id, string Name, string Phone, uint Points,
    ulong Spending, DateTime? LastPurchase, uint Version);
public sealed record CustomerInvoice(uint Id, string Code, DateTime PaidAt, ulong Total, uint Points);
public sealed record CustomerPoint(ulong Id, string Type, int Points, uint Balance, string? Reason, DateTime At);
public sealed record CustomerHistory(CustomerRow Customer, List<CustomerInvoice> Invoices, List<CustomerPoint> Points);
public sealed class PointAdjustment
{
    [Range(int.MinValue, int.MaxValue)] public int Points { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = "";
    public uint? InvoiceId { get; set; }
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
    public static uint AdjustedBalance(uint balance, int points)
    {
        var next = (long)balance + points;
        if (points == 0 || next < 0 || next > uint.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(points), "Điểm điều chỉnh phải khác 0 và không làm số dư âm hoặc vượt giới hạn.");
        return (uint)next;
    }
}

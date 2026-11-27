using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Returns.Services;

public sealed record ExchangePlanLine(long ProductId, string Name, int Quantity, long UnitPrice,
    long Discount, long? PromotionId);

public sealed record ExchangePlanVersion(int Revision, long CartId, long CartVersion,
    ExchangePlanLine[] Lines, long Total, long CashDue, long ProposedBy, DateTimeOffset ProposedAt,
    long? DecidedBy = null, DateTimeOffset? DecidedAt = null, string? Decision = null, string? Note = null);

public sealed record LegacyExchangeDecision(string Status, long? DecidedBy, DateTime? DecidedAt, string? Note);

public sealed record ExchangePlanHistory(int FormatVersion, ExchangePlanVersion[] Versions,
    LegacyExchangeDecision? LegacyDecision = null)
{
    [JsonIgnore] public ExchangePlanVersion Current => Versions[^1];
    public static ExchangePlanHistory? Read(string? json)
    {
        if (json is null) return null; // Legacy records are not backfilled as known history.
        var value = JsonSerializer.Deserialize<ExchangePlanHistory>(json);
        if (value is null || value.FormatVersion != 1 || value.Versions.Length == 0)
            throw Changed("Snapshot phương án đổi không hợp lệ. Cần kiểm tra dữ liệu.");
        return value;
    }

    public string Json() => JsonSerializer.Serialize(this);

    public ExchangePlanHistory Decide(long actor, DateTimeOffset at, bool approve, string note)
    {
        if (Current.Decision is not null) throw Changed("Phiên bản phương án đã được xử lý.");
        return this with { Versions = Versions[..^1].Append(Current with
        { DecidedBy = actor, DecidedAt = at, Decision = approve ? "APPROVED" : "REJECTED", Note = note }).ToArray() };
    }

    public static string Fingerprint(ExchangePlanVersion plan) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(new
        { plan.Revision, plan.CartId, plan.CartVersion, plan.Lines, plan.Total, plan.CashDue, plan.ProposedBy })));

    internal static LoiNghiepVu Changed(string message) =>
        LoiNghiepVu.XungDot("DT03_PHUONG_AN_THAY_DOI", message);

    internal static async Task RequireActorAsync(CuaHangDbContext db, long actor, string role, CancellationToken ct)
    {
        var id = await db.DocMotGiaTriAsync("""
            SELECT t.id FROM tai_khoan t JOIN vai_tro v ON v.id=t.vai_tro_id
            WHERE t.id=@id AND v.ma=@role AND t.trang_thai='HOAT_DONG'
              AND t.bat_buoc_doi_mk=FALSE AND (t.khoa_tam_den IS NULL OR t.khoa_tam_den<=NOW())
            FOR SHARE OF t
            """, ct, ("@id", actor), ("@role", role));
        if (id is null) throw new LoiNghiepVu("KHONG_DU_QUYEN", 403, "Tài khoản thực hiện không còn hợp lệ.");
    }
}

public sealed record ExchangePlanResubmission(ApprovalDetails Details, ExchangePlanVersion Proposal, string Token);

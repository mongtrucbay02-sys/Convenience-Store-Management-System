using System.Globalization;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Api;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Services;

public static class ProductPriceRules
{
    public static ProductPriceChangeDto Normalize(ProductPriceChangeDto input)
    {
        ArgumentNullException.ThrowIfNull(input); var errors = new List<LoiTruong>();
        if (input.GiaBan is null or <= 0 or > uint.MaxValue || input.GiaBan % 100 != 0) errors.Add(new("giaBan", "Giá phải là số nguyên dương, bội số 100 đồng trong phạm vi lưu trữ."));
        if (input.PhienBan <= 0) errors.Add(new("phienBan", "Cần phiên bản hiện tại."));
        if (!TryParse(input.HieuLucTu, out _)) errors.Add(new("hieuLucTu", "Thời điểm phải là ISO 8601 có múi giờ, trong phạm vi lưu trữ."));
        if (errors.Count > 0) throw LoiNghiepVu.DuLieuKhongHopLe(errors);
        return input;
    }
    public static DateTime ResolveEffectiveAt(string? requested, DateTime databaseNow)
    {
        if (!TryParse(requested, out var value)) throw LoiNghiepVu.DuLieuKhongHopLe("hieuLucTu", "Thời điểm không hợp lệ.");
        var resolved = value?.ToOffset(GioVietNam.DoLech).DateTime ?? databaseNow;
        resolved = new DateTime(resolved.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Unspecified);
        if (resolved < databaseNow) throw LoiNghiepVu.DuLieuKhongHopLe("hieuLucTu", "Thời điểm hiệu lực không được ở quá khứ.");
        return resolved;
    }
    public static void EnsureCostConfirmed(long price, decimal cost, bool confirmed)
    {
        if (price < cost && !confirmed) throw LoiNghiepVu.XungDot("CAN_XAC_NHAN_GIA_DUOI_VON", "Giá mới thấp hơn giá vốn. Vui lòng xác nhận lại.", new { giaBan = price, giaVon = cost });
    }
    private static bool TryParse(string? text, out DateTimeOffset? value)
    {
        value = null; if (string.IsNullOrWhiteSpace(text)) return true;
        if (text.Length > 40 || !Regex.IsMatch(text, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant)
            || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        try { _ = parsed.ToOffset(GioVietNam.DoLech); } catch (ArgumentOutOfRangeException) { return false; }
        value = parsed; return true;
    }
}

public sealed class ProductPricingService(IProductPricingRepository prices)
{
    private static void Admin(string role) { if (role != "AD") throw new LoiNghiepVu(MaLoiChung.KhongDuQuyen, 403, "Chỉ AD được đổi hoặc hủy giá bán."); }
    public async Task<ProductPriceResult> FindAsync(long id, long priceId, string role, CancellationToken ct = default)
    { Admin(role); return await prices.FindAsync(id, priceId, ct) ?? throw LoiNghiepVu.KhongTimThay("Mức giá không còn tồn tại."); }
    public Task<ProductPriceResult> CreateAsync(long id, ProductPriceChangeDto input, long actorId, string role, CancellationToken ct = default)
    { Admin(role); input = ProductPriceRules.Normalize(input); ct.ThrowIfCancellationRequested(); return prices.CreateAsync(id, input, actorId, ct); }
    public Task<ProductPriceResult> CancelAsync(long id, long priceId, ProductPriceCancelDto input, long actorId, string role, CancellationToken ct = default)
    { Admin(role); if (input.PhienBan <= 0) throw LoiNghiepVu.DuLieuKhongHopLe("phienBan", "Cần phiên bản hiện tại."); ct.ThrowIfCancellationRequested(); return prices.CancelAsync(id, priceId, input.PhienBan, actorId, ct); }
}

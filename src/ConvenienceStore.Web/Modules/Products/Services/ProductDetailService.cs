using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class ProductDetailService(IProductDetailRepository products, IDichVuKho inventory)
{
    public static ProductPricesQuery Normalize(ProductPricesQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Page <= 0 || query.Size <= 0 || ((long)query.Page - 1) * Math.Min(query.Size, 100) > int.MaxValue)
            throw LoiNghiepVu.DuLieuKhongHopLe("page", "Trang hoặc số dòng không hợp lệ.");
        return query with { Size = Math.Min(query.Size, 100) };
    }

    public async Task<ProductDetailsDto> FindAsync(long id, string role, CancellationToken ct = default)
    {
        CheckRole(role); ct.ThrowIfCancellationRequested();
        var row = await products.FindAsync(id, role == "TN", ct) ?? throw Missing();
        var stock = (await inventory.LayTonKhoAsync([checked((int)id)], ct)).SingleOrDefault();
        var scheduled = await products.PricesAsync(id, new(), ct);
        return row with {
            AnhUrl = SafeImageUrl(row.AnhUrl), TonKho = stock?.SoLuongTon ?? 0, NguongToiThieu = stock?.NguongToiThieu ?? 0,
            GiaVon = role == "AD" ? stock?.GiaVonBinhQuan : null,
            GiaDaLenLich = RestrictAudit(scheduled, role),
            LichSuGia = role == "AD" ? await products.PricesAsync(id, new() { DaLenLich = false }, ct) : null
        };
    }

    public async Task<PagedResultDto<ProductPriceDto>> PricesAsync(long id, ProductPricesQuery query, string role, CancellationToken ct = default)
    {
        CheckRole(role); query = Normalize(query); ct.ThrowIfCancellationRequested();
        if (!query.DaLenLich && role != "AD") throw new LoiNghiepVu(MaLoiChung.KhongDuQuyen, 403, "Chỉ AD được xem lịch sử giá.");
        if (await products.FindAsync(id, role == "TN", ct) is null) throw Missing();
        return RestrictAudit(await products.PricesAsync(id, query, ct), role);
    }

    public static string? SafeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Any(char.IsControl) || url.Contains('\\')) return null;
        if (url.StartsWith('/') && !url.StartsWith("//")) return url;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" ? url : null;
    }

    private static PagedResultDto<ProductPriceDto> RestrictAudit(PagedResultDto<ProductPriceDto> page, string role) =>
        role == "AD" ? page : page with { Items = page.Items.Select(x => x with { NguoiTaoId = null, NgayTao = null, NguoiHuyId = null, HuyLuc = null }).ToArray() };
    private static LoiNghiepVu Missing() => LoiNghiepVu.KhongTimThay("Sản phẩm không còn tồn tại");
    private static void CheckRole(string role) { if (role is not ("AD" or "NK" or "TN")) throw new UnauthorizedAccessException(); }
}

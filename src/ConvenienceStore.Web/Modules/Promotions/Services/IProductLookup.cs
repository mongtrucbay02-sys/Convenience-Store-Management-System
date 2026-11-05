using System.Globalization;
using System.Text;
using ConvenienceStore.Web.Modules.Promotions.Models;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

public sealed record ProductPage(
    IReadOnlyList<ProductRefDto> Items, int Page, int PageSize, int TotalItems, int TotalPages);

/// <summary>
/// Tra cứu sản phẩm cho khuyến mãi (chọn sản phẩm khi tạo/sửa). Danh mục có thể lên tới hàng nghìn sản phẩm
/// nên chỉ tìm kiếm có phân trang, không bao giờ tải toàn bộ. Hiện là dữ liệu mẫu vì SP-01 (#2) chưa có;
/// thay bằng hợp đồng với module Products khi có.
/// </summary>
public interface IProductLookup
{
    /// <summary>Tìm theo tên hoặc SKU (không phân biệt hoa thường và dấu). Từ khóa rỗng là lấy tất cả.</summary>
    Task<ProductPage> SearchAsync(
        string? keyword, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Các sản phẩm tồn tại trong danh sách id, theo thứ tự id được hỏi; id không có thì bỏ qua.</summary>
    Task<IReadOnlyList<ProductRefDto>> GetByIdsAsync(
        IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
}

public sealed class SampleProductLookup : IProductLookup
{
    /// <summary>5 sản phẩm thật (id 101-105) và các sản phẩm mẫu đến id 1100 (tổng 1.000).</summary>
    public static readonly IReadOnlyList<ProductRefDto> All = BuildAll();

    private static readonly Dictionary<long, ProductRefDto> ById = All.ToDictionary(p => p.Id);

    private static List<ProductRefDto> BuildAll()
    {
        var list = new List<ProductRefDto>
        {
            new(101, "SUA-1L", "Sữa tươi 1L"),
            new(102, "NUOC-500", "Nước suối 500ml"),
            new(103, "MI-GOI", "Mì gói"),
            new(104, "CF-LON", "Cà phê lon"),
            new(105, "BANH-MI", "Bánh mì sandwich"),
        };
        for (var id = 106; id <= 1100; id++)
            list.Add(new ProductRefDto(id, $"SP-{id:0000}", $"Sản phẩm mẫu {id}"));
        return list;
    }

    public Task<ProductPage> SearchAsync(
        string? keyword, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var key = Normalize(keyword);
        var matches = string.IsNullOrEmpty(key)
            ? All
            : All.Where(p => Normalize(p.Name).Contains(key) || Normalize(p.Sku).Contains(key)).ToList();

        var totalPages = (int)Math.Ceiling(matches.Count / (double)pageSize);
        var items = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new ProductPage(items, page, pageSize, matches.Count, totalPages));
    }

    public Task<IReadOnlyList<ProductRefDto>> GetByIdsAsync(
        IReadOnlyCollection<long> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProductRefDto>>(
            ids.Where(ById.ContainsKey).Select(id => ById[id]).ToList());

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var decomposed = text.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString();
    }
}

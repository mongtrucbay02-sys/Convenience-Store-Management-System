namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record ProductListQuery
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
    public string? Keyword { get; init; }
    public long? DanhMucId { get; init; }
    public string? TrangThai { get; init; }
    public long? GiaBanTu { get; init; }
    public long? GiaBanDen { get; init; }
    public string Sort { get; init; } = "tenSanPham";

    public static readonly IReadOnlySet<string> AllowedParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "page", "size", "keyword", "danhMucId", "trangThai", "giaBanTu", "giaBanDen", "sort" };
}

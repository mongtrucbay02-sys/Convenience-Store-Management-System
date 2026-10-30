
namespace ConvenienceStore.Web.Modules.Products.DTOs;

/// <summary>
/// Ket qua danh sach co phan trang theo quy uoc API Issue #4.
/// Tam thoi dat trong Products; se chuyen sang Shared
/// khi nhom phe duyet cau truc dung chung.
/// </summary>
public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int Size
);

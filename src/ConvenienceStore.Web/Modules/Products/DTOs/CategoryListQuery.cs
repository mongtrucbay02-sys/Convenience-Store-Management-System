
namespace ConvenienceStore.Web.Modules.Products.DTOs;

/// <summary>
/// Query parameters for SP-07.
/// Validation for non-positive values is pending team approval.
/// </summary>
public sealed record CategoryListQuery
{
    public int Page { get; init; } = 1;

    public int Size { get; init; } = 20;

    // Issue #4: maximum page size is 100.
    // Non-positive values are not normalized here.
    public int NormalizedSize => Math.Min(Size, 100);
}

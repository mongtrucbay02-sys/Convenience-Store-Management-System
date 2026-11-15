
namespace ConvenienceStore.Web.Modules.Products.DTOs;

/// <summary>
/// Query parameters for SP-07.
/// Page and Size must be positive; Size is capped at 100.
/// </summary>
public sealed record CategoryListQuery
{
    public int Page { get; init; } = 1;

    public int Size { get; init; } = 20;

    // Issue #4: maximum page size is 100.
    // Non-positive values are not normalized here.
    public int NormalizedSize => Math.Min(Size, 100);

    public bool IsValid => Page > 0 && Size > 0 && ((long)Page - 1) * NormalizedSize <= int.MaxValue;
}

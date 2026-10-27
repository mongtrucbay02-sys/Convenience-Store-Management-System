namespace ConvenienceStore.Web.Modules.Promotions.Services;

public enum PromotionErrorKind { Validation, NotFound, Conflict, Unprocessable }

/// <summary>Lỗi nghiệp vụ. Code là mã lỗi ổn định của hợp đồng API (ví dụ PROMOTION_ENDED).</summary>
public sealed record PromotionError(
    PromotionErrorKind Kind,
    string Code,
    string Title,
    IReadOnlyDictionary<string, string[]>? Fields = null);

public sealed class PromotionResult<T>
{
    private PromotionResult(T? value, PromotionError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public PromotionError? Error { get; }
    public bool IsSuccess => Error is null;

    public static PromotionResult<T> Ok(T value) => new(value, null);
    public static PromotionResult<T> Fail(PromotionError error) => new(default, error);
}

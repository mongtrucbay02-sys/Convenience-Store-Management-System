namespace ConvenienceStore.Web.Shared.Errors;

public sealed record PhanHoiLoi(string Code, string Message, object? Details, string TraceId);

namespace ConvenienceStore.Web.Shared.Api;

public sealed record TrangDuLieu<T>(IReadOnlyList<T> Items, int Total, int Page, int Size);

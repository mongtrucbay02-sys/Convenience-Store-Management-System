namespace ConvenienceStore.Web.Modules.Auth.Dtos;

public sealed record BoLocTaiKhoan(
    int? Page = null,
    int? Size = null,
    string? Keyword = null,
    string? VaiTro = null,
    string? TrangThai = null,
    string? Sort = null);

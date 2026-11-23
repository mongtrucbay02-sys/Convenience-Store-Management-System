namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record CategoryMutationDto(string? Ten, string? MoTa, bool KhongDoiTra);

public sealed record CategoryUpdateDto(string? Ten, string? MoTa, bool KhongDoiTra, long PhienBan);

public sealed record CategoryDetailsDto(long Id, string? Ten, string? MoTa,
    bool KhongDoiTra, string TrangThai, long PhienBan);

public enum CategoryMutationStatus { Success, Invalid, Duplicate, NotFound, Conflict }

public sealed record CategoryMutationResult(
    CategoryMutationStatus Status, CategoryDetailsDto? Category = null,
    string? Error = null);

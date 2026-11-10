namespace ConvenienceStore.Web.Modules.Purchasing.DTOs;

public sealed record SupplierMutationDto(string Ten, string? NguoiLienHe,
    string SoDienThoai, string? Email, string? DiaChi, string? MaSoThue,
    string? GhiChu);

public sealed record SupplierUpdateDto(string Ten, string? NguoiLienHe,
    string SoDienThoai, string? Email, string? DiaChi, string? MaSoThue,
    string? GhiChu, uint PhienBan);

public sealed record SupplierDetailsDto(uint Id, string Ma, string Ten,
    string? NguoiLienHe, string SoDienThoai, string? Email, string? DiaChi,
    string? MaSoThue, string? GhiChu, string TrangThai, uint PhienBan);

public enum SupplierMutationStatus { Success, Invalid, DuplicatePhone, DuplicateCode, NotFound, Conflict }

public sealed record SupplierMutationResult(SupplierMutationStatus Status,
    SupplierDetailsDto? Supplier = null, string? Error = null);

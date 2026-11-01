namespace ConvenienceStore.Web.Modules.Purchasing.DTOs;

public sealed record SupplierListQuery(string? Search = null, string? Status = null);

public sealed record SupplierListItemDto(uint Id, string Ma, string Ten,
    string? NguoiLienHe, string SoDienThoai, string? DiaChi,
    string TrangThai, DateOnly? LanNhapGanNhat);

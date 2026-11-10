using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Modules.Purchasing.DTOs;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

// No production allocator or HTTP write endpoint is registered until business/security approval.
public sealed class SupplierMutationService(
    ISupplierMutationRepository repository, ISupplierCodeAllocator codeAllocator)
{
    public async Task<SupplierMutationResult> CreateAsync(SupplierMutationDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var error = Validate(input.Ten, input.NguoiLienHe, input.SoDienThoai,
            input.Email, input.DiaChi, input.MaSoThue, input.GhiChu);
        if (error is not null)
            return new(SupplierMutationStatus.Invalid, Error: error);
        var code = await codeAllocator.AllocateAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(code) || code.Length > 20)
            throw new InvalidOperationException("Supplier code allocator returned an invalid code.");
        return await repository.CreateAsync(code, Normalize(input), cancellationToken);
    }

    public Task<SupplierMutationResult> UpdateAsync(uint id, SupplierUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var error = Validate(input.Ten, input.NguoiLienHe, input.SoDienThoai,
            input.Email, input.DiaChi, input.MaSoThue, input.GhiChu);
        if (id == 0 || input.PhienBan == 0)
            error = "Id and PhienBan must be positive.";
        if (error is not null)
            return Task.FromResult(new SupplierMutationResult(SupplierMutationStatus.Invalid, Error: error));
        var normalized = Normalize(new SupplierMutationDto(input.Ten, input.NguoiLienHe,
            input.SoDienThoai, input.Email, input.DiaChi, input.MaSoThue, input.GhiChu));
        return repository.UpdateAsync(id, new(normalized.Ten, normalized.NguoiLienHe,
            normalized.SoDienThoai, normalized.Email, normalized.DiaChi,
            normalized.MaSoThue, normalized.GhiChu, input.PhienBan), cancellationToken);
    }

    private static SupplierMutationDto Normalize(SupplierMutationDto x)
        => new(x.Ten.Trim(), x.NguoiLienHe?.Trim(), x.SoDienThoai.Trim(),
            x.Email?.Trim(), x.DiaChi?.Trim(), x.MaSoThue?.Trim(), x.GhiChu?.Trim());

    private static string? Validate(string? name, string? contact, string? phone,
        string? email, string? address, string? tax, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Ten is required.";
        if (name.Trim().Length > 200) return "Ten exceeds the mapped column length.";
        if (string.IsNullOrWhiteSpace(phone)) return "SoDienThoai is required.";
        if (!Regex.IsMatch(phone.Trim(), "^0[0-9]{9,10}$"))
            return "SoDienThoai has an invalid format.";
        if (contact?.Trim().Length > 100 || email?.Trim().Length > 100 ||
            address?.Trim().Length > 255 || tax?.Trim().Length > 14 ||
            notes?.Trim().Length > 500)
            return "A field exceeds its mapped column length.";
        if (!string.IsNullOrWhiteSpace(email) &&
            !new EmailAddressAttribute().IsValid(email.Trim()))
            return "Email has an invalid format.";
        return null;
    }
}

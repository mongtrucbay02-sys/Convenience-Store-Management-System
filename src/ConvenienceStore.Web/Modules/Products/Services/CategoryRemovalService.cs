using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Services;

// PR #202's positive ID/version validation and reference-result propagation are retained.
public sealed class CategoryRemovalService(ICategoryRemovalRepository repository)
{
    public Task<CategoryRemovalPreviewDto?> PreviewAsync(uint id, CancellationToken ct = default) => repository.PreviewAsync(id, ct);
    public Task<CategoryRemovalResult> DeleteAsync(uint id, uint version, CancellationToken ct = default)
    { if (id == 0 || version == 0) return Task.FromResult(new CategoryRemovalResult(CategoryRemovalStatus.Invalid)); ct.ThrowIfCancellationRequested(); return repository.DeleteAsync(id, version, ct); }
    public Task<CategoryRemovalResult> HideAsync(uint id, uint version, CancellationToken ct = default)
    { if (id == 0 || version == 0) return Task.FromResult(new CategoryRemovalResult(CategoryRemovalStatus.Invalid)); ct.ThrowIfCancellationRequested(); return repository.HideAsync(id, version, ct); }
    public Task<CategoryRemovalResult> DeleteAsync(uint id, CategoryRemovalCommand input, CancellationToken ct = default)
    { ValidateCommand(input); return DeleteAsync(id, input.PhienBan, ct); }
    public Task<CategoryRemovalResult> HideAsync(uint id, CategoryRemovalCommand input, CancellationToken ct = default)
    { ValidateCommand(input); return HideAsync(id, input.PhienBan, ct); }
    public static void ValidateCommand(CategoryRemovalCommand input)
    {
        ArgumentNullException.ThrowIfNull(input); var errors = new List<LoiTruong>();
        if (input.PhienBan == 0) errors.Add(new("phienBan", "Cần phiên bản hiện tại."));
        if (!input.XacNhan) errors.Add(new("xacNhan", "Cần xác nhận thao tác."));
        if (errors.Count > 0) throw LoiNghiepVu.DuLieuKhongHopLe(errors);
    }
}

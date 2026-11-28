using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

// Adapted from PR #196; runtime uses the existing DanhMuc version mechanism.
public sealed class CategoryMutationService
{
    private readonly ICategoryMutationRepository _repository;

    public CategoryMutationService(ICategoryMutationRepository repository)
        => _repository = repository;

    public Task<CategoryMutationResult> CreateAsync(CategoryMutationDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var error = Validate(input.Ten, input.MoTa);
        if (error is not null)
            return Task.FromResult(new CategoryMutationResult(CategoryMutationStatus.Invalid, Error: error));

        return _repository.CreateAsync(new CategoryMutationDto(
            input.Ten!.Trim(), input.MoTa?.Trim(), input.KhongDoiTra), cancellationToken);
    }

    public Task<CategoryMutationResult> UpdateAsync(long id, CategoryUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var error = Validate(input.Ten, input.MoTa);
        if (id <= 0 || input.PhienBan <= 0)
            error = "Id and PhienBan must be positive.";
        if (error is not null)
            return Task.FromResult(new CategoryMutationResult(CategoryMutationStatus.Invalid, Error: error));

        return _repository.UpdateAsync(id, input with
        {
            Ten = input.Ten!.Trim(),
            MoTa = input.MoTa?.Trim()
        }, cancellationToken);
    }

    public Task<CategoryDetailsDto?> FindAsync(long id, CancellationToken ct = default) => _repository.FindAsync(id, ct);

    private static string? Validate(string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Ten is required.";
        if (name.Trim().Length > 100) return "Ten exceeds the mapped column length.";
        if (description?.Trim().Length > 255) return "MoTa exceeds the mapped column length.";
        return null;
    }
}

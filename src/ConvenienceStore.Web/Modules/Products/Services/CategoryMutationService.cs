using ConvenienceStore.Web.Modules.Products.DTOs;

namespace ConvenienceStore.Web.Modules.Products.Services;

// HTTP exposure awaits the approved authentication and role contract.
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
            input.Ten.Trim(), input.MoTa?.Trim(), input.KhongDoiTra), cancellationToken);
    }

    public Task<CategoryMutationResult> UpdateAsync(uint id, CategoryUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var error = Validate(input.Ten, input.MoTa);
        if (id == 0 || input.PhienBan == 0)
            error = "Id and PhienBan must be positive.";
        if (error is not null)
            return Task.FromResult(new CategoryMutationResult(CategoryMutationStatus.Invalid, Error: error));

        return _repository.UpdateAsync(id, input with
        {
            Ten = input.Ten.Trim(),
            MoTa = input.MoTa?.Trim()
        }, cancellationToken);
    }

    private static string? Validate(string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Ten is required.";
        if (name.Trim().Length > 100) return "Ten exceeds the mapped column length.";
        if (description?.Trim().Length > 255) return "MoTa exceeds the mapped column length.";
        return null;
    }
}

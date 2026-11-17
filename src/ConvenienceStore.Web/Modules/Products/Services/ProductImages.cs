using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed record ValidatedProductImage(byte[] Content, string Extension);

public static class ProductImageValidation
{
    public const int MaximumBytes = 2 * 1024 * 1024;

    public static async Task<ValidatedProductImage> ReadAsync(IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length <= 0 || file.Length > MaximumBytes) throw Invalid();
        await using var source = file.OpenReadStream(); using var output = new MemoryStream(); var buffer = new byte[8192];
        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            if (output.Length + read > MaximumBytes) throw Invalid();
            output.Write(buffer, 0, read);
        }
        var bytes = output.ToArray();
        var png = bytes.Length >= 45 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)
            && bytes.AsSpan(bytes.Length - 12).SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 });
        var jpeg = bytes.Length >= 12 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217;
        if (png && file.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)) return new(bytes, ".png");
        if (jpeg && file.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)) return new(bytes, ".jpg");
        throw Invalid();
    }

    private static LoiNghiepVu Invalid() => LoiNghiepVu.DuLieuKhongHopLe("image", "Ảnh phải là JPG/PNG, dung lượng tối đa 2 MB.");
}

public interface IProductImageStore
{
    Task<string> SaveAsync(ValidatedProductImage image, CancellationToken ct = default);
    void Remove(string url);
}

public sealed class DiskProductImageStore(string directory) : IProductImageStore
{
    private const string Prefix = "/uploads/products/";
    public async Task<string> SaveAsync(ValidatedProductImage image, CancellationToken ct = default)
    {
        if (image.Extension is not (".png" or ".jpg")) throw new ArgumentException("Unsupported validated image extension.");
        ct.ThrowIfCancellationRequested(); Directory.CreateDirectory(directory);
        var name = Guid.NewGuid().ToString("N") + image.Extension; var path = Path.Combine(directory, name); var created = false;
        try
        {
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, true); created = true;
            await stream.WriteAsync(image.Content, ct);
        }
        catch { if (created && File.Exists(path)) File.Delete(path); throw; }
        return Prefix + name;
    }

    public void Remove(string url)
    {
        if (!url.StartsWith(Prefix, StringComparison.Ordinal)) throw new ArgumentException("Not a generated product image URL.");
        var name = url[Prefix.Length..]; var extension = Path.GetExtension(name);
        if (extension is not (".png" or ".jpg") || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _) || name != Path.GetFileName(name))
            throw new ArgumentException("Not a generated product image name.");
        // Only a random file created by this store can pass the exact-name validation above.
        File.Delete(Path.Combine(directory, name));
    }
}

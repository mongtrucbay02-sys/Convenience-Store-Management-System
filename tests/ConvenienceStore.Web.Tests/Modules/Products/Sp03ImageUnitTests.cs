using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp03ImageUnitTests
{
    // Real 1x1 JPEG fixture generated once; no imaging package is needed at test/runtime.
    private static byte[] Jpeg => Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDyOiiiuw5D/9k=");

    private static FormFile FileOf(byte[] bytes, string type) => new(new MemoryStream(bytes), 0, bytes.Length, "image", "../../unsafe.exe") { Headers = new HeaderDictionary(), ContentType = type };

    [Fact]
    public async Task ValidPngJpegAndExactTwoMegabyteBoundaryAreAccepted()
    {
        Assert.Equal(".png", (await ProductImageValidation.ReadAsync(FileOf(Sp03ImageHttpTests.Png, "image/png"))).Extension);
        Assert.Equal(".jpg", (await ProductImageValidation.ReadAsync(FileOf(Jpeg, "image/jpeg"))).Extension);
        using var large = new MemoryStream(); large.Write(Jpeg.AsSpan(0, Jpeg.Length - 2));
        var remaining = ProductImageValidation.MaximumBytes - Jpeg.Length;
        while (remaining > 0) {
            var segmentSize = Math.Min(65537, remaining); Assert.True(segmentSize >= 4);
            large.WriteByte(255); large.WriteByte(254); // Legal JPEG COM segment before EOI.
            var length = segmentSize - 2; large.WriteByte((byte)(length >> 8)); large.WriteByte((byte)length);
            large.Write(new byte[segmentSize - 4]); remaining -= segmentSize;
        }
        large.Write(Jpeg.AsSpan(Jpeg.Length - 2)); Assert.Equal(ProductImageValidation.MaximumBytes, large.Length);
        Assert.Equal(".jpg", (await ProductImageValidation.ReadAsync(FileOf(large.ToArray(), "image/jpeg"))).Extension);
        Assert.Equal(422, (await Assert.ThrowsAsync<LoiNghiepVu>(() => ProductImageValidation.ReadAsync(FileOf(new byte[ProductImageValidation.MaximumBytes + 1], "image/png")))).MaHttp);
    }

    [Fact]
    public async Task InvalidImageAndCancellationNeverBecomeStoredContent()
    {
        await Assert.ThrowsAsync<LoiNghiepVu>(() => ProductImageValidation.ReadAsync(null));
        await Assert.ThrowsAsync<LoiNghiepVu>(() => ProductImageValidation.ReadAsync(FileOf(Jpeg, "image/png")));
        await Assert.ThrowsAsync<LoiNghiepVu>(() => ProductImageValidation.ReadAsync(FileOf(Sp03ImageHttpTests.Png[..16], "image/png")));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ProductImageValidation.ReadAsync(FileOf(Jpeg, "image/jpeg"), cancel.Token));
    }

    [Fact]
    public async Task DiskStoreGeneratesNamesAndOnlyRemovesExactOwnedFile()
    {
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/sp-module/image-unit", Guid.NewGuid().ToString("N")));
        var store = new DiskProductImageStore(folder); var image = await ProductImageValidation.ReadAsync(FileOf(Jpeg, "image/jpeg"));
        var url = await store.SaveAsync(image); var file = Path.Combine(folder, Path.GetFileName(url));
        Assert.Equal(Jpeg, await File.ReadAllBytesAsync(file)); Assert.EndsWith(".jpg", url);
        var keep = Path.Combine(folder, "keep.txt"); await File.WriteAllTextAsync(keep, "unrelated");
        Assert.Throws<ArgumentException>(() => store.Remove("/uploads/products/../keep.txt"));
        Assert.Throws<ArgumentException>(() => store.Remove("/unrelated/" + Path.GetFileName(url)));
        store.Remove(url); Assert.False(File.Exists(file)); Assert.True(File.Exists(keep));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(image, cancel.Token));
        Assert.Single(Directory.GetFiles(folder));
    }
}

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Products.Services;

public interface IProductImageRepository
{
    Task SetAsync(uint id, uint version, string imageUrl, CancellationToken ct = default);
}

public sealed class EfProductImageRepository(CuaHangDbContext db) : IProductImageRepository
{
    public async Task SetAsync(uint id, uint version, string imageUrl, CancellationToken ct = default)
    {
        var changed = await db.ChayAsync("UPDATE san_pham SET anh_url=@image,phien_ban=phien_ban+1 WHERE id=@id AND phien_ban=@version", ct,
            ("@id", id), ("@version", version), ("@image", imageUrl));
        if (changed > 0) return;
        if (!await db.SanPhams.AnyAsync(x => x.Id == id, ct)) throw LoiNghiepVu.KhongTimThay("Sản phẩm không còn tồn tại");
        throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Dữ liệu đã thay đổi. Tải lại trước khi lưu.");
    }
}

public sealed class ProductImageService(IProductImageStore store, IProductImageRepository products, ProductDetailService details)
{
    public async Task<string> StoreAsync(IFormFile file, CancellationToken ct = default) => await store.SaveAsync(await ProductImageValidation.ReadAsync(file, ct), ct);
    public void Remove(string url) => store.Remove(url);

    public async Task<ProductDetailsDto> SetAsync(uint id, uint version, IFormFile? image, string role, CancellationToken ct = default)
    {
        if (role is not ("AD" or "NK")) throw new LoiNghiepVu(MaLoiChung.KhongDuQuyen, 403, "Chỉ AD và NK được sửa ảnh.");
        if (version == 0) throw LoiNghiepVu.DuLieuKhongHopLe("phienBan", "Cần phiên bản hiện tại.");
        var validated = await ProductImageValidation.ReadAsync(image, ct);
        var url = await store.SaveAsync(validated, ct);
        try { await products.SetAsync(id, version, url, ct); }
        catch (LoiNghiepVu) { store.Remove(url); throw; }
        // Do not remove a committed image if subsequent read-back fails or commit status is unknown.
        return await details.FindAsync(id, role, ct);
    }
}

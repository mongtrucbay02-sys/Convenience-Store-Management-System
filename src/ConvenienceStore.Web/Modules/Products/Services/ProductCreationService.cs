using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class ProductCreationService(IProductCreationRepository products, ProductDetailService details)
{
    public static ProductCreateDto Normalize(ProductCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var value = input with { Ma = input.Ma?.Trim(), TenSanPham = input.TenSanPham?.Trim(), DonViTinh = input.DonViTinh?.Trim(), MoTa = input.MoTa?.Trim() };
        var errors = new List<LoiTruong>();
        if (value.Ma?.Length > 20) errors.Add(new("ma", "Mã tối đa 20 ký tự."));
        if (string.IsNullOrEmpty(value.TenSanPham) || value.TenSanPham.Length > 200) errors.Add(new("tenSanPham", "Tên sản phẩm bắt buộc, tối đa 200 ký tự."));
        if (value.DanhMucId == 0) errors.Add(new("danhMucId", "Chọn danh mục đang hoạt động."));
        if (string.IsNullOrEmpty(value.DonViTinh) || value.DonViTinh.Length > 20) errors.Add(new("donViTinh", "Đơn vị tính bắt buộc, tối đa 20 ký tự."));
        if (value.GiaBan is null or <= 0 or > uint.MaxValue || value.GiaBan % 100 != 0) errors.Add(new("giaBan", "Giá bán phải là số nguyên dương, bội số 100 đồng trong phạm vi lưu trữ."));
        if (value.NguongToiThieu < 0) errors.Add(new("nguongToiThieu", "Ngưỡng tối thiểu không được âm."));
        if (value.MoTa?.Length > 1000) errors.Add(new("moTa", "Mô tả tối đa 1000 ký tự."));
        if (errors.Count > 0) throw LoiNghiepVu.DuLieuKhongHopLe(errors);
        return value with { Ma = string.IsNullOrEmpty(value.Ma) ? null : value.Ma, MoTa = string.IsNullOrEmpty(value.MoTa) ? null : value.MoTa };
    }

    public async Task<ProductDetailsDto> CreateAsync(ProductCreateDto input, uint actorId, string role, CancellationToken ct = default, string? imageUrl = null)
    {
        var id = await CreateIdAsync(input, actorId, role, ct, imageUrl);
        return await details.FindAsync(id, role, ct);
    }

    public async Task<uint> CreateIdAsync(ProductCreateDto input, uint actorId, string role, CancellationToken ct = default, string? imageUrl = null)
    {
        if (role is not ("AD" or "NK")) throw new LoiNghiepVu(MaLoiChung.KhongDuQuyen, 403, "Chỉ AD và NK được thêm sản phẩm.");
        input = Normalize(input); ct.ThrowIfCancellationRequested();
        input = input with { Ma = input.Ma ?? "SP" + Guid.NewGuid().ToString("N")[..18] };
        return await products.CreateAsync(input, actorId, imageUrl, ct);
    }
}

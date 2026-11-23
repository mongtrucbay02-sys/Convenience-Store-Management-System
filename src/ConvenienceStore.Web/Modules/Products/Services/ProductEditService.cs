using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class ProductEditService(IProductEditRepository products, ProductDetailService details)
{
    public static ProductEditDto Normalize(ProductEditDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var row = input with { TenSanPham = input.TenSanPham?.Trim(), DonViTinh = input.DonViTinh?.Trim(), MoTa = input.MoTa?.Trim() };
        var errors = new List<LoiTruong>();
        if (string.IsNullOrEmpty(row.TenSanPham) || row.TenSanPham.Length > 200) errors.Add(new("tenSanPham", "Tên bắt buộc, tối đa 200 ký tự."));
        if (row.DanhMucId <= 0) errors.Add(new("danhMucId", "Chọn danh mục hợp lệ."));
        if (string.IsNullOrEmpty(row.DonViTinh) || row.DonViTinh.Length > 20) errors.Add(new("donViTinh", "Đơn vị bắt buộc, tối đa 20 ký tự."));
        if (row.MoTa?.Length > 1000) errors.Add(new("moTa", "Mô tả tối đa 1000 ký tự."));
        if (row.NguongToiThieu < 0) errors.Add(new("nguongToiThieu", "Ngưỡng tối thiểu không được âm."));
        if (row.PhienBan <= 0) errors.Add(new("phienBan", "Cần phiên bản hiện tại."));
        if (errors.Count > 0) throw LoiNghiepVu.DuLieuKhongHopLe(errors);
        return row with { MoTa = string.IsNullOrEmpty(row.MoTa) ? null : row.MoTa };
    }

    public static void EnsureUnitChangeAllowed(string previous, string next, int stock, bool hasTransactions)
    {
        if (!string.Equals(previous, next, StringComparison.Ordinal) && (stock > 0 || hasTransactions))
            throw LoiNghiepVu.XungDot("KHONG_DOI_DON_VI_TINH", "Không đổi đơn vị tính khi sản phẩm đã có tồn hoặc giao dịch.");
    }

    public async Task ApplyAsync(long id, ProductEditDto input, string role, CancellationToken ct = default, string? imageUrl = null)
    {
        if (role is not ("AD" or "NK")) throw new LoiNghiepVu(MaLoiChung.KhongDuQuyen, 403, "Chỉ AD và NK được sửa thông tin sản phẩm.");
        input = Normalize(input); ct.ThrowIfCancellationRequested(); await products.UpdateAsync(id, input, imageUrl, ct);
    }

    public async Task<ProductDetailsDto> UpdateAsync(long id, ProductEditDto input, string role, CancellationToken ct = default)
    { await ApplyAsync(id, input, role, ct); return await details.FindAsync(id, role, ct); }
}

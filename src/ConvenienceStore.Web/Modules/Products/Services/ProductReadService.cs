using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Services;

public sealed class ProductReadService(IProductReadRepository products, IDichVuKho inventory)
{
    internal static readonly HashSet<string> SortFields = ["ma", "tenSanPham", "giaBan", "trangThai", "ngayTao"];

    public static ProductListQuery Normalize(ProductListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var errors = new List<LoiTruong>();
        var size = Math.Min(query.Size, 100);
        if (query.Page <= 0 || ((long)query.Page - 1) * size > int.MaxValue) errors.Add(new("page", "Trang nằm ngoài phạm vi cho phép."));
        if (size <= 0) errors.Add(new("size", "Số dòng phải lớn hơn 0."));
        if (query.Keyword?.Length > 200) errors.Add(new("keyword", "Từ khóa tối đa 200 ký tự."));
        if (query.DanhMucId <= 0) errors.Add(new("danhMucId", "ID danh mục phải lớn hơn 0."));
        if (query.TrangThai is not (null or "" or "DANG_KINH_DOANH" or "NGUNG_KINH_DOANH")) errors.Add(new("trangThai", "Trạng thái không hợp lệ."));
        if (query.GiaBanTu < 0 || query.GiaBanDen < 0 || query.GiaBanTu > uint.MaxValue || query.GiaBanDen > uint.MaxValue || query.GiaBanTu > query.GiaBanDen) errors.Add(new("giaBanTu", "Khoảng giá không hợp lệ."));
        var fields = (query.Sort ?? "").Split(',');
        if (fields.Length > SortFields.Count || fields.Any(x => !SortFields.Contains(x.TrimStart('-'))) || fields.Select(x => x.TrimStart('-')).Distinct().Count() != fields.Length)
            errors.Add(new("sort", "Trường sắp xếp không hợp lệ."));
        if (errors.Count > 0) throw LoiNghiepVu.DuLieuKhongHopLe(errors);
        return query with { Size = size, Keyword = query.Keyword?.Trim() };
    }

    public async Task<PagedResultDto<ProductListItemDto>> ListAsync(ProductListQuery query, string role, CancellationToken ct = default)
    {
        if (role is not ("AD" or "NK" or "TN")) throw new UnauthorizedAccessException();
        query = Normalize(query); ct.ThrowIfCancellationRequested();
        var page = await products.ListAsync(query, role == "TN", ct);
        var stock = (await inventory.LayTonKhoAsync(page.Items.Select(x => x.Id).ToArray(), ct)).ToDictionary(x => (long)x.SanPhamId);
        return page with { Items = page.Items.Select(x => x with {
            TonKho = stock.GetValueOrDefault(x.Id)?.SoLuongTon ?? 0,
            GiaVon = role == "AD" ? stock.GetValueOrDefault(x.Id)?.GiaVonBinhQuan : null
        }).ToArray() };
    }
}

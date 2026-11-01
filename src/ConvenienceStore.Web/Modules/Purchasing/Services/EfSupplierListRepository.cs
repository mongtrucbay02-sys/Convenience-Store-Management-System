using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

public sealed class EfSupplierListRepository(ApplicationDbContext db)
    : ISupplierListRepository
{
    public async Task<IReadOnlyList<SupplierListItemDto>> ListAsync(SupplierListQuery query,
        CancellationToken cancellationToken = default)
    {
        var suppliers = db.NhaCungCaps.AsNoTracking();
        if (query.Search is { Length: > 0 } search)
            suppliers = suppliers.Where(x => x.Ten.Contains(search) ||
                x.SoDienThoai.Contains(search));
        if (query.Status is { Length: > 0 } status)
            suppliers = suppliers.Where(x => x.TrangThai == status);
        return await suppliers.OrderBy(x => x.Id)
            .Select(x => new SupplierListItemDto(
                x.Id, x.Ma, x.Ten, x.NguoiLienHe, x.SoDienThoai,
                x.DiaChi, x.TrangThai, null))
            .ToListAsync(cancellationToken);
    }
}

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Purchasing.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing.Services;

public sealed class EfReceiptListRepository(ApplicationDbContext db) : IReceiptListRepository
{
    public async Task<IReadOnlyList<ReceiptListItemDto>> ListAsync(ReceiptListQuery query,
        CancellationToken cancellationToken = default)
    {
        var receipts = db.PhieuNhaps.AsNoTracking();
        if (query.Code is { Length: > 0 } code)
            receipts = receipts.Where(x => x.MaPhieu.Contains(code));
        if (query.SupplierId is uint supplierId)
            receipts = receipts.Where(x => x.NhaCungCapId == supplierId);
        if (query.From is DateOnly from)
            receipts = receipts.Where(x => x.NgayNhap >= from);
        if (query.To is DateOnly to)
            receipts = receipts.Where(x => x.NgayNhap <= to);
        if (query.Status is { Length: > 0 } status)
            receipts = receipts.Where(x => x.TrangThai == status);
        return await receipts.OrderByDescending(x => x.NgayNhap).ThenByDescending(x => x.Id)
            .Select(x => new ReceiptListItemDto(x.Id, x.MaPhieu, x.NgayNhap,
                x.NhaCungCapId, x.NhaCungCap.Ten, x.TongTien,
                x.TrangThai, x.NguoiLapId, x.NguoiLap.HoTen))
            .ToListAsync(cancellationToken);
    }
}
using System.Data;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing;

public sealed class SupplierPurchaseHistoryService(CuaHangDbContext db)
{
    private const string ConfirmedStatus = "DA_NHAP_KHO";

    public async Task<SupplierPurchaseHistory?> LoadAsync(long? supplierId, DateOnly? from, DateOnly? to,
        int page, CancellationToken ct)
    {
        var suppliers = await LoadSuppliersAsync(ct);
        if (supplierId is null)
            return new(suppliers, null, from, to, 0, 0, [], false, [], page);

        var supplier = suppliers.FirstOrDefault(row => row.Id == supplierId.Value);
        if (supplier is null) return null;

        await using var snapshot = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var receipts = FilterConfirmedReceipts(supplier.Id, from, to);
        var summary = await LoadReceiptSummaryAsync(receipts, ct);
        var rows = await LoadReceiptPageAsync(receipts, page, ct);
        var lines = await LoadProductLinesAsync(receipts, ct);
        var products = SummarizeProducts(lines);
        await snapshot.CommitAsync(ct);

        return new(suppliers, supplier, from, to, summary.Count, summary.Total,
            rows.Take(SupplierPurchaseHistory.ReceiptsPerPage).ToArray(),
            rows.Count > SupplierPurchaseHistory.ReceiptsPerPage, products, page);
    }

    private async Task<IReadOnlyList<SupplierPurchaseSupplier>> LoadSuppliersAsync(CancellationToken ct) =>
        await db.NhaCungCaps.AsNoTracking().OrderBy(row => row.Ten).ThenBy(row => row.Id)
            .Select(row => new SupplierPurchaseSupplier(row.Id, row.Ma, row.Ten)).ToListAsync(ct);

    private IQueryable<PhieuNhap> FilterConfirmedReceipts(long supplierId, DateOnly? from, DateOnly? to)
    {
        var receipts = db.PhieuNhaps.AsNoTracking()
            .Where(row => row.NhaCungCapId == supplierId && row.TrangThai == ConfirmedStatus);
        if (from.HasValue) receipts = receipts.Where(row => row.NgayNhap >= from.Value);
        if (to.HasValue) receipts = receipts.Where(row => row.NgayNhap <= to.Value);
        return receipts;
    }

    private static async Task<ReceiptSummary> LoadReceiptSummaryAsync(IQueryable<PhieuNhap> receipts,
        CancellationToken ct)
    {
        var totals = await receipts.Select(row => row.TongTien).ToListAsync(ct);
        return new(totals.Count, totals.Aggregate(0L, (total, value) => checked(total + value)));
    }

    private static Task<List<SupplierPurchaseReceiptRow>> LoadReceiptPageAsync(IQueryable<PhieuNhap> receipts,
        int page, CancellationToken ct) => receipts
        .OrderByDescending(row => row.NgayNhap).ThenByDescending(row => row.Id)
        .Skip(checked((page - 1) * SupplierPurchaseHistory.ReceiptsPerPage))
        .Take(SupplierPurchaseHistory.ReceiptsPerPage + 1)
        .Select(row => new SupplierPurchaseReceiptRow(row.Id, row.MaPhieu, row.NgayNhap,
            row.CtPhieuNhaps.Count, row.TongTien))
        .ToListAsync(ct);

    private Task<List<PurchaseLine>> LoadProductLinesAsync(IQueryable<PhieuNhap> receipts, CancellationToken ct) =>
        db.CtPhieuNhaps.AsNoTracking()
            .Where(line => receipts.Select(row => row.Id).Contains(line.PhieuNhapId))
            .Select(line => new PurchaseLine(line.SanPhamId, line.TenSanPham, line.DonViTinh,
                line.SoLuong, line.GiaNhap, line.PhieuNhapId, line.PhieuNhap.NgayNhap, line.PhieuNhap.XacNhanLuc))
            .ToListAsync(ct);

    private static IReadOnlyList<SupplierPurchaseProductRow> SummarizeProducts(IReadOnlyList<PurchaseLine> lines) =>
        lines.GroupBy(line => line.ProductId).Select(SummarizeProduct)
            .OrderByDescending(row => row.ReceiptCount)
            .ThenByDescending(row => row.TotalQuantity)
            .ThenBy(row => row.ProductName, StringComparer.Ordinal)
            .ThenBy(row => row.ProductId)
            .ToArray();

    private static SupplierPurchaseProductRow SummarizeProduct(IGrouping<long, PurchaseLine> lines)
    {
        var latest = lines.OrderByDescending(line => line.ReceivedOn)
            .ThenByDescending(line => line.ConfirmedAt).ThenByDescending(line => line.ReceiptId).First();
        var quantity = lines.Aggregate(0L, (total, line) => checked(total + line.Quantity));
        var value = lines.Aggregate(0L, (total, line) => checked(total + checked(line.Quantity * line.Price)));
        return new(lines.Key, latest.ProductName, latest.Unit,
            lines.Select(line => line.ReceiptId).Distinct().Count(), quantity, value,
            lines.Min(line => line.Price), lines.Max(line => line.Price), latest.Price, latest.ReceivedOn);
    }

    private sealed record ReceiptSummary(int Count, long Total);
    private sealed record PurchaseLine(long ProductId, string ProductName, string Unit, long Quantity,
        long Price, long ReceiptId, DateOnly ReceivedOn, DateTime? ConfirmedAt);
}

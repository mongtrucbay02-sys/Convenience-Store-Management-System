using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing;

/// <summary>
/// NH-09. Xem lịch sử nhập theo nhà cung cấp (Admin/Quản lý).
/// Chỉ tính phiếu DA_NHAP_KHO; phiếu nháp và phiếu đã hủy không ảnh hưởng số liệu.
/// </summary>
[Authorize(Policy = MaQuyen.NhapHangLichSuNcc)]
[Route("NhapHang/LichSuNcc")]
public sealed class SupplierPurchaseHistoryController(CuaHangDbContext db) : Controller
{
    private const string ConfirmedStatus = "DA_NHAP_KHO";

    [HttpGet("")]
    public async Task<IActionResult> Index(long? supplierId = null, DateOnly? from = null, DateOnly? to = null,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid || supplierId is <= 0 || (from.HasValue && to.HasValue && from > to))
            return BadRequest();

        var suppliers = await db.NhaCungCaps.AsNoTracking()
            .OrderBy(row => row.Ten).ThenBy(row => row.Id).ToListAsync(ct);
        if (supplierId is null)
            return Render(new(suppliers, null, from, to, 0, 0, [], false, []));

        var supplier = suppliers.FirstOrDefault(row => row.Id == supplierId.Value);
        if (supplier is null)
            return NotFound();

        var receipts = db.PhieuNhaps.AsNoTracking()
            .Where(row => row.NhaCungCapId == supplier.Id && row.TrangThai == ConfirmedStatus);
        if (from.HasValue) receipts = receipts.Where(row => row.NgayNhap >= from.Value);
        if (to.HasValue) receipts = receipts.Where(row => row.NgayNhap <= to.Value);

        var receiptCount = await receipts.CountAsync(ct);
        var totalValue = receiptCount == 0 ? 0 : await receipts.SumAsync(row => row.TongTien, ct);
        var receiptRows = await receipts
            .OrderByDescending(row => row.NgayNhap).ThenByDescending(row => row.Id)
            .Take(SupplierPurchaseHistory.MaxReceipts + 1)
            .Select(row => new SupplierPurchaseReceiptRow(row.Id, row.MaPhieu, row.NgayNhap,
                row.CtPhieuNhaps.Count, row.TongTien))
            .ToListAsync(ct);

        var lines = await db.CtPhieuNhaps.AsNoTracking()
            .Where(line => receipts.Select(row => row.Id).Contains(line.PhieuNhapId))
            .Select(line => new
            {
                line.SanPhamId,
                line.TenSanPham,
                line.DonViTinh,
                line.SoLuong,
                line.GiaNhap,
                line.PhieuNhapId,
                line.PhieuNhap.NgayNhap,
                line.PhieuNhap.XacNhanLuc
            })
            .ToListAsync(ct);

        var products = lines
            .GroupBy(line => line.SanPhamId)
            .Select(group =>
            {
                var latest = group
                    .OrderByDescending(line => line.NgayNhap)
                    .ThenByDescending(line => line.XacNhanLuc)
                    .ThenByDescending(line => line.PhieuNhapId)
                    .First();
                return new SupplierPurchaseProductRow(group.Key, latest.TenSanPham, latest.DonViTinh,
                    group.Select(line => line.PhieuNhapId).Distinct().Count(),
                    group.Sum(line => line.SoLuong),
                    group.Sum(line => checked(line.SoLuong * line.GiaNhap)),
                    group.Min(line => line.GiaNhap),
                    group.Max(line => line.GiaNhap),
                    latest.GiaNhap,
                    latest.NgayNhap);
            })
            .OrderByDescending(row => row.ReceiptCount)
            .ThenByDescending(row => row.TotalQuantity)
            .ThenBy(row => row.ProductName, StringComparer.Ordinal)
            .ThenBy(row => row.ProductId)
            .Take(SupplierPurchaseHistory.MaxProducts)
            .ToList();

        return Render(new(suppliers, supplier, from, to, receiptCount, totalValue,
            receiptRows.Take(SupplierPurchaseHistory.MaxReceipts).ToList(),
            receiptRows.Count > SupplierPurchaseHistory.MaxReceipts, products));
    }

    private ViewResult Render(SupplierPurchaseHistory model) =>
        View("~/Views/Purchasing/SupplierHistory.cshtml", model);
}

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Inventory.Controllers;

public sealed record StockOverviewRow(
    long Id, string Code, string Name, string Category, int Quantity, decimal? Cost, int? Threshold, string Status);

public sealed record StockMovementRow(DateTime Time, string Type, string Document, int Change, int Balance, string Actor);
public sealed record StockAlertRow(long Id, string Code, string Name, int Quantity, int Threshold, string? LatestSupplier);

[Authorize(Policy = MaQuyen.TonKhoXem)]
[AutoValidateAntiforgeryToken]
[Route("Kho")]
public sealed class StockOverviewController(CuaHangDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, long? categoryId, string? status, int page = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || page is < 1 or > 100000) return BadRequest();
        var showCost = User.IsInRole(BangQuyen.Admin);
        var showThreshold = showCost || User.IsInRole(BangQuyen.NhanVienKho);
        var term = q?.Trim();
        var query = db.SanPhams.AsNoTracking();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(product => product.TenSanPham.Contains(term) || product.Ma.Contains(term));
        if (categoryId.HasValue)
            query = query.Where(product => product.DanhMucId == categoryId.Value);
        query = status switch
        {
            "out" => query.Where(product => product.TonKho == null || product.TonKho.SoLuongTon == 0),
            "low" => query.Where(product => product.TonKho != null && product.TonKho.SoLuongTon > 0
                && product.TonKho.SoLuongTon <= product.TonKho.NguongToiThieu),
            "enough" => query.Where(product => product.TonKho != null && product.TonKho.SoLuongTon > 0
                && product.TonKho.SoLuongTon > product.TonKho.NguongToiThieu),
            _ => query
        };
        var rows = await query.OrderBy(product => product.TenSanPham).ThenBy(product => product.Id)
            .Skip((page - 1) * 50).Take(51).Select(product => new StockOverviewRow(
                product.Id, product.Ma, product.TenSanPham, product.DanhMuc.Ten,
                product.TonKho != null ? product.TonKho.SoLuongTon : 0,
                showCost && product.TonKho != null ? product.TonKho.GiaVonBinhQuan : null,
                showThreshold && product.TonKho != null ? product.TonKho.NguongToiThieu : null,
                product.TonKho == null || product.TonKho.SoLuongTon == 0 ? "Hết"
                    : product.TonKho.SoLuongTon <= product.TonKho.NguongToiThieu ? "Sắp hết" : "Đủ")).ToListAsync(ct);
        ViewBag.ShowCost = showCost;
        ViewBag.ShowThreshold = showThreshold;
        ViewBag.Categories = await db.DanhMucs.AsNoTracking().OrderBy(category => category.Ten).ToListAsync(ct);
        ViewBag.CategoryId = categoryId;
        ViewBag.Status = status;
        ViewBag.Term = term;
        ViewBag.Page = page;
        ViewBag.HasNext = rows.Count > 50;
        return View("~/Views/Inventory/Stock.cshtml", rows.Take(50).ToList());
    }

    [HttpGet("{id}/TheKho")]
    [Authorize(Policy = MaQuyen.TheKhoCanhBaoXem)]
    public async Task<IActionResult> History(long id, DateOnly? from, DateOnly? to, int page = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || page is < 1 or > 100000 ||
            from < new DateOnly(1000, 1, 1) || to == DateOnly.MaxValue || from > to)
            return BadRequest("Khoảng thời gian hoặc số trang không hợp lệ.");
        var product = await db.SanPhams.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, ct);
        if (product is null) return NotFound();

        var query = db.TheKhos.AsNoTracking().Where(row => row.SanPhamId == id);
        if (from is { } first)
        {
            var start = first.ToDateTime(TimeOnly.MinValue);
            query = query.Where(row => row.ThoiDiem >= start);
        }
        if (to is { } last)
        {
            var end = last.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(row => row.ThoiDiem < end);
        }
        var rows = await query.OrderByDescending(row => row.ThoiDiem).ThenByDescending(row => row.Id)
            .Skip((page - 1) * 50).Take(51).Select(row => new StockMovementRow(
                row.ThoiDiem, row.LoaiBienDong, row.MaChungTu, row.SoLuongThayDoi, row.TonSau,
                row.NguoiThucHien.HoTen)).ToListAsync(ct);
        ViewBag.Product = $"{product.Ma} · {product.TenSanPham}";
        ViewBag.ProductId = id;
        ViewBag.From = from?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        ViewBag.To = to?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        ViewBag.Page = page;
        ViewBag.HasNext = rows.Count > 50;
        return View("~/Views/Inventory/History.cshtml", rows.Take(50).ToList());
    }

    [HttpGet("CanhBao")]
    [Authorize(Policy = MaQuyen.TheKhoCanhBaoXem)]
    public async Task<IActionResult> Alerts(CancellationToken ct)
    {
        var rows = await db.TonKhos.AsNoTracking().Where(row => row.NguongToiThieu > 0 && row.SoLuongTon <= row.NguongToiThieu)
            .OrderByDescending(row => row.NguongToiThieu - row.SoLuongTon).ThenBy(row => row.SanPhamId)
            .Select(row => new StockAlertRow(row.SanPhamId, row.SanPham.Ma, row.SanPham.TenSanPham,
                row.SoLuongTon, row.NguongToiThieu, row.SanPham.CtPhieuNhaps
                    .Where(line => line.PhieuNhap.TrangThai == "DA_NHAP_KHO")
                    .OrderByDescending(line => line.PhieuNhap.XacNhanLuc).ThenByDescending(line => line.PhieuNhapId)
                    .Select(line => line.PhieuNhap.NhaCungCap.Ten).FirstOrDefault())).ToListAsync(ct);
        return View("~/Views/Inventory/Alerts.cshtml", rows);
    }

    [HttpGet("Nguong")]
    [Authorize(Policy = MaQuyen.NguongTonSua)]
    public async Task<IActionResult> Thresholds(CancellationToken ct)
    {
        ViewBag.Categories = await db.DanhMucs.AsNoTracking().OrderBy(row => row.Ten).ToListAsync(ct);
        var rows = await db.TonKhos.AsNoTracking().OrderBy(row => row.SanPham.TenSanPham)
            .Select(row => new StockAlertRow(row.SanPhamId, row.SanPham.Ma, row.SanPham.TenSanPham,
                row.SoLuongTon, row.NguongToiThieu, null)).ToListAsync(ct);
        return View("~/Views/Inventory/Thresholds.cshtml", rows);
    }

    [HttpPost("Nguong")]
    [Authorize(Policy = MaQuyen.NguongTonSua)]
    public async Task<IActionResult> SaveThresholds(long? categoryId, long[] productIds, int? threshold, CancellationToken ct)
    {
        if (!ModelState.IsValid || threshold is null or < 0 || (categoryId.HasValue == (productIds.Length > 0)))
        {
            TempData["Loi"] = "Chọn đúng một phạm vi và nhập ngưỡng là số nguyên không âm.";
            return RedirectToAction(nameof(Thresholds));
        }
        var query = db.TonKhos.AsQueryable();
        query = categoryId.HasValue ? query.Where(row => row.SanPham.DanhMucId == categoryId)
            : query.Where(row => productIds.Contains(row.SanPhamId));
        var count = await query.ExecuteUpdateAsync(update => update.SetProperty(row => row.NguongToiThieu, threshold.Value), ct);
        TempData["ThongBao"] = count == 0 ? "Không có dòng tồn kho được cập nhật." : $"Đã cập nhật ngưỡng cho {count} sản phẩm.";
        return RedirectToAction(nameof(Thresholds));
    }
}

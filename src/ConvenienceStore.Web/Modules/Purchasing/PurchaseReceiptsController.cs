using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Inventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing;

[Authorize]
[Route("NhapHang")]
[AutoValidateAntiforgeryToken]
public sealed class PurchaseReceiptsController(CuaHangDbContext db, PurchaseReceiptService receipts) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = MaQuyen.PhieuNhapXem)]
    public async Task<IActionResult> Index(string? q, string? status, int page = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || page is < 1 or > 100000) return BadRequest();
        var term = q?.Trim();
        var query = db.PhieuNhaps.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(row => row.MaPhieu.Contains(term) || row.NhaCungCap.Ten.Contains(term));
        if (status is "NHAP" or "DA_NHAP_KHO" or "DA_HUY")
            query = query.Where(row => row.TrangThai == status);
        var rows = await query.Include(row => row.NhaCungCap).Include(row => row.NguoiLap)
            .OrderByDescending(row => row.Id).Skip((page - 1) * 50).Take(51).ToListAsync(ct);
        ViewBag.Term = term;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.HasNext = rows.Count > 50;
        return View("~/Views/Purchasing/Index.cshtml", rows.Take(50).ToList());
    }

    [HttpGet("Tao")]
    [Authorize(Policy = MaQuyen.PhieuNhapLap)]
    public async Task<IActionResult> Create(CancellationToken ct) => await CreateView(new(), ct);

    [HttpGet("GiaNhapGanNhat")]
    [Authorize(Policy = MaQuyen.PhieuNhapLap)]
    public async Task<IActionResult> LastPrice(long productId, CancellationToken ct)
    {
        if (!ModelState.IsValid || productId <= 0) return BadRequest();
        var price = await db.CtPhieuNhaps.AsNoTracking()
            .Where(line => line.SanPhamId == productId && line.PhieuNhap.TrangThai == "DA_NHAP_KHO")
            .OrderByDescending(line => line.PhieuNhap.XacNhanLuc).ThenByDescending(line => line.PhieuNhapId)
            .Select(line => (long?)line.GiaNhap).FirstOrDefaultAsync(ct);
        return Json(new { price });
    }

    [HttpPost("TaoNhanhSanPham")]
    [Authorize(Policy = MaQuyen.PhieuNhapLap)]
    [Authorize(Policy = MaQuyen.SanPhamSua)]
    public async Task<IActionResult> QuickCreate(QuickPurchaseProduct model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Thông tin sản phẩm không hợp lệ." });
        try
        {
            var item = await receipts.CreateProductAsync(model, User.LayTaiKhoanId(), ct);
            return Json(new { id = item.Id, text = $"{item.Ma} · {item.TenSanPham}" });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("Tao")]
    [Authorize(Policy = MaQuyen.PhieuNhapLap)]
    public async Task<IActionResult> Create(PurchaseReceiptInput model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await receipts.CreateAsync(model, User.LayTaiKhoanId(), ct);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (OverflowException)
            {
                ModelState.AddModelError("", "Tổng tiền vượt giới hạn cho phép.");
            }
        }
        return await CreateView(model, ct);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = MaQuyen.PhieuNhapXem)]
    public async Task<IActionResult> Details(long id, CancellationToken ct)
    {
        var receipt = await db.PhieuNhaps.AsNoTracking().Include(row => row.NhaCungCap)
            .Include(row => row.NguoiLap).Include(row => row.NguoiXacNhan)
            .Include(row => row.CtPhieuNhaps).SingleOrDefaultAsync(row => row.Id == id, ct);
        return receipt is null ? NotFound() : View("~/Views/Purchasing/Details.cshtml", receipt);
    }

    [HttpPost("{id}/XacNhan")]
    [Authorize(Policy = MaQuyen.PhieuNhapXacNhan)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        try
        {
            var warnings = await receipts.ConfirmAsync(id, User.LayTaiKhoanId(), ct);
            TempData["ThongBao"] = warnings.Count == 0 ? "Đã xác nhận nhập kho."
                : $"Đã nhập kho. Giá vốn từ giá bán trở lên: {string.Join(", ", warnings)}.";
        }
        catch (LoiDichVuKho ex)
        {
            TempData["Loi"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("{id}/Sua")]
    [Authorize(Policy = MaQuyen.PhieuNhapLap)]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var receipt = await db.PhieuNhaps.AsNoTracking().Include(row => row.CtPhieuNhaps).SingleOrDefaultAsync(row => row.Id == id, ct);
        if (receipt is null) return NotFound();
        if (receipt.TrangThai != "NHAP") return Conflict("Chỉ sửa phiếu nháp.");
        return await CreateView(new PurchaseReceiptInput
        {
            SupplierId = receipt.NhaCungCapId, SupplierDocument = receipt.SoChungTuNcc, Version = receipt.PhienBan,
            Lines = receipt.CtPhieuNhaps.Select(line => new PurchaseLineInput(line.SanPhamId, checked((int)line.SoLuong), line.GiaNhap)).ToList()
        }, ct, id);
    }

    [HttpPost("{id}/Sua")]
    [Authorize(Policy = MaQuyen.PhieuNhapLap)]
    public async Task<IActionResult> Edit(long id, PurchaseReceiptInput model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await receipts.EditAsync(id, model, ct);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            catch (OverflowException) { ModelState.AddModelError("", "Số lượng hoặc tổng tiền vượt giới hạn."); }
        }
        return await CreateView(model, ct, id);
    }

    [HttpPost("{id}/Huy")]
    [Authorize(Policy = MaQuyen.PhieuNhapLap)]
    public async Task<IActionResult> Cancel(long id, string? reason, CancellationToken ct)
    {
        try { await receipts.CancelAsync(id, reason, User.LayTaiKhoanId(), ct); }
        catch (ArgumentException ex) { TempData["Loi"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Loi"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> CreateView(PurchaseReceiptInput model, CancellationToken ct, long? id = null)
    {
        ViewBag.ReceiptId = id;
        ViewBag.Suppliers = await db.NhaCungCaps.AsNoTracking().Where(row => row.TrangThai == "HOAT_DONG")
            .OrderBy(row => row.Ten).ToListAsync(ct);
        ViewBag.Products = await db.SanPhams.AsNoTracking().Where(row => row.TrangThai == "DANG_KINH_DOANH")
            .OrderBy(row => row.TenSanPham).ToListAsync(ct);
        ViewBag.Categories = await db.DanhMucs.AsNoTracking().Where(row => row.TrangThai == "HOAT_DONG")
            .OrderBy(row => row.Ten).ToListAsync(ct);
        return View("~/Views/Purchasing/Create.cshtml", model);
    }
}

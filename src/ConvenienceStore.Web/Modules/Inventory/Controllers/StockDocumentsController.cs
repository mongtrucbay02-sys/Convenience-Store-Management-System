using System.Data;
using System.Globalization;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Inventory.Controllers;

public sealed record CountInput(long ProductId, int? Quantity, string? Note);
public sealed record DisposalInput(long ProductId, int? Quantity, string? Reason, string? Note);
public sealed record DisposalProduct(long Id, string Name, int OnHand, decimal Cost);

[Authorize(Policy = MaQuyen.TheKhoCanhBaoXem)]
[AutoValidateAntiforgeryToken]
[Route("Kho/ChungTu")]
public sealed class StockDocumentsController(CuaHangDbContext db, IDichVuKho stock, TimeProvider clock) : Controller
{
    private DateTime Now => clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime;
    private long Actor => checked((long)User.LayTaiKhoanId());

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Counts = await db.PhieuKiemKes.AsNoTracking().OrderByDescending(row => row.Id).Take(100).ToListAsync(ct);
        ViewBag.Disposals = await db.PhieuXuatHuys.AsNoTracking().OrderByDescending(row => row.Id).Take(100).ToListAsync(ct);
        ViewBag.Categories = await db.DanhMucs.AsNoTracking().OrderBy(row => row.Ten).ToListAsync(ct);
        ViewBag.Products = await db.SanPhams.AsNoTracking().OrderBy(row => row.TenSanPham).ToListAsync(ct);
        return View("~/Views/Inventory/Documents.cshtml");
    }

    [HttpPost("KiemKe")]
    [Authorize(Policy = MaQuyen.KiemKeXuatHuyLap)]
    public async Task<IActionResult> CreateCount(string scope, long? categoryId, long[] productIds, CancellationToken ct)
    {
        if (!ModelState.IsValid || scope is not ("TOAN_BO" or "DANH_MUC" or "CHON_SAN_PHAM") ||
            (scope == "DANH_MUC" && categoryId is null) || (scope == "CHON_SAN_PHAM" && productIds.Length == 0))
            return Issue("Chọn phạm vi kiểm kê hợp lệ.");
        var query = db.TonKhos.AsNoTracking();
        if (scope == "DANH_MUC") query = query.Where(row => row.SanPham.DanhMucId == categoryId);
        if (scope == "CHON_SAN_PHAM") query = query.Where(row => productIds.Contains(row.SanPhamId));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var lines = await query.Select(row => new CtKiemKe { SanPhamId = row.SanPhamId, TonLucLap = row.SoLuongTon })
            .ToListAsync(ct);
        if (lines.Count == 0 || (scope == "CHON_SAN_PHAM" && lines.Count != productIds.Distinct().Count()))
            return Issue("Phạm vi kiểm kê có sản phẩm chưa khởi tạo tồn hoặc không có sản phẩm.");
        var receipt = new PhieuKiemKe
        {
            MaPhieu = await NextCodeAsync("KK", ct), PhamVi = scope, DanhMucId = scope == "DANH_MUC" ? categoryId : null,
            NguoiLapId = Actor, TrangThai = "NHAP", CtKiemKes = lines
        };
        db.PhieuKiemKes.Add(receipt);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RedirectToAction(nameof(Count), new { id = receipt.Id });
    }

    [HttpGet("KiemKe/{id}")]
    public async Task<IActionResult> Count(long id, CancellationToken ct)
    {
        var row = await db.PhieuKiemKes.AsNoTracking().Include(p => p.CtKiemKes).ThenInclude(line => line.SanPham)
            .ThenInclude(product => product.TonKho).SingleOrDefaultAsync(p => p.Id == id, ct);
        if (row is null) return NotFound();
        return View("~/Views/Inventory/Count.cshtml", row);
    }

    [HttpPost("KiemKe/{id}/Luu")]
    [Authorize(Policy = MaQuyen.KiemKeXuatHuyLap)]
    public async Task<IActionResult> SaveCount(long id, long version, List<CountInput> lines, bool submit, CancellationToken ct)
    {
        if (!ModelState.IsValid || lines.Any(line => line.Quantity is < 0 || line.Note?.Length > 255) ||
            lines.Select(line => line.ProductId).Distinct().Count() != lines.Count || (submit && lines.Any(line => line.Quantity is null)))
            return Issue("Kiểm tra số lượng và ghi chú. Điền đủ số lượng trước khi gửi duyệt.", nameof(Count), id);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var state = await LockCountAsync(id, ct);
        if (state is not ("NHAP" or "BI_TU_CHOI")) return Conflict("Phiếu không còn sửa được.");
        var row = await db.PhieuKiemKes.Include(p => p.CtKiemKes).SingleAsync(p => p.Id == id, ct);
        if (row.PhienBan != version) return Conflict("Phiếu đã thay đổi. Tải lại trước khi lưu.");
        var entries = lines.ToDictionary(line => line.ProductId);
        if (row.CtKiemKes.Count != entries.Count || row.CtKiemKes.Any(line => !entries.ContainsKey(line.SanPhamId)))
            return BadRequest("Các dòng không khớp với phiếu.");
        foreach (var line in row.CtKiemKes)
        {
            var entry = entries[line.SanPhamId];
            line.SoLuongThucTe = entry.Quantity;
            line.LyDoChenhLech = string.IsNullOrWhiteSpace(entry.Note) ? null : entry.Note.Trim();
        }
        row.TrangThai = submit ? "CHO_DUYET" : "NHAP";
        row.GuiDuyetLuc = submit ? Now : null;
        row.NguoiDuyetId = null;
        row.DuyetLuc = null;
        row.LyDoTuChoi = null;
        row.PhienBan++;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RedirectToAction(nameof(Count), new { id });
    }

    [HttpPost("KiemKe/{id}/Duyet")]
    [Authorize(Policy = MaQuyen.KiemKeXuatHuyDuyet)]
    public async Task<IActionResult> ReviewCount(long id, bool approve, string? reason, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!approve && InvalidReason(reason)) return Issue("Nhập lý do từ chối.", nameof(Count), id);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        if (await LockCountAsync(id, ct) != "CHO_DUYET") return Conflict("Phiếu không còn chờ duyệt.");
        var row = await db.PhieuKiemKes.Include(p => p.CtKiemKes).SingleAsync(p => p.Id == id, ct);
        if (approve)
        {
            if (row.CtKiemKes.Count == 0 || row.CtKiemKes.Any(line => line.SoLuongThucTe is null))
                return Conflict("Chưa có đủ số lượng đếm thực tế.");
            try
            {
                var result = await stock.GhiNhanBienDongAsync(new(LoaiBienDong.KIEM_KE, checked((int)id),
                    row.MaPhieu, User.LayTaiKhoanId(), row.CtKiemKes.Select(line => new DongBienDong(
                        checked((int)line.SanPhamId), SoLuongThucTe: line.SoLuongThucTe)).ToArray()), ct);
                var changes = result.Dong.ToDictionary(line => checked((long)line.SanPhamId));
                foreach (var line in row.CtKiemKes)
                {
                    var change = changes[line.SanPhamId];
                    line.ChenhLechLucDuyet = change.SoLuongThayDoi;
                    line.GiaVonLucDuyet = change.GiaVonTruoc;
                }
            }
            catch (LoiDichVuKho ex) { return Issue(ex.Message, nameof(Count), id); }
        }
        row.TrangThai = approve ? "DA_DIEU_CHINH" : "BI_TU_CHOI";
        row.NguoiDuyetId = Actor;
        row.DuyetLuc = Now;
        row.LyDoTuChoi = approve ? null : reason!.Trim();
        row.PhienBan++;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RedirectToAction(nameof(Count), new { id });
    }

    [HttpGet("XuatHuy/Tao")]
    [Authorize(Policy = MaQuyen.KiemKeXuatHuyLap)]
    public async Task<IActionResult> NewDisposal(CancellationToken ct)
    {
        var products = await db.TonKhos.AsNoTracking().Where(row => row.SoLuongTon > 0)
            .OrderBy(row => row.SanPham.TenSanPham).Select(row => new DisposalProduct(
                row.SanPhamId, row.SanPham.TenSanPham, row.SoLuongTon, row.GiaVonBinhQuan)).ToListAsync(ct);
        return View("~/Views/Inventory/NewDisposal.cshtml", products);
    }

    [HttpPost("XuatHuy/Tao")]
    [Authorize(Policy = MaQuyen.KiemKeXuatHuyLap)]
    public async Task<IActionResult> CreateDisposal(List<DisposalInput> lines, bool submit, CancellationToken ct)
    {
        var selected = lines.Where(line => line.Quantity is not null).ToArray();
        if (!ModelState.IsValid || selected.Length == 0 || selected.Any(line => line.Quantity <= 0 ||
            line.Reason is not ("HONG" or "HET_HAN" or "MAT" or "KHAC") || line.Note?.Length > 255 ||
            (line.Reason == "KHAC" && string.IsNullOrWhiteSpace(line.Note))) ||
            selected.Select(line => line.ProductId).Distinct().Count() != selected.Length)
            return Issue("Nhập số lượng dương và lý do hợp lệ cho từng sản phẩm.", nameof(NewDisposal));
        var ids = selected.Select(line => line.ProductId).ToArray();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var balances = await db.TonKhos.AsNoTracking().Where(row => ids.Contains(row.SanPhamId))
            .ToDictionaryAsync(row => row.SanPhamId, row => row.SoLuongTon, ct);
        if (selected.Any(line => !balances.TryGetValue(line.ProductId, out var quantity) || line.Quantity > quantity))
            return Issue("Số lượng xuất hủy vượt tồn hiện tại.", nameof(NewDisposal));
        var receipt = new PhieuXuatHuy
        {
            MaPhieu = await NextCodeAsync("XH", ct), NguoiLapId = Actor, TrangThai = submit ? "CHO_DUYET" : "NHAP",
            GuiDuyetLuc = submit ? Now : null,
            CtXuatHuys = selected.Select(line => new CtXuatHuy
            {
                SanPhamId = line.ProductId, SoLuong = checked((long)line.Quantity!.Value), LyDo = line.Reason!,
                GhiChuLyDo = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim()
            }).ToList()
        };
        db.PhieuXuatHuys.Add(receipt);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RedirectToAction(nameof(Disposal), new { id = receipt.Id });
    }

    [HttpGet("XuatHuy/{id}")]
    public async Task<IActionResult> Disposal(long id, CancellationToken ct)
    {
        var row = await db.PhieuXuatHuys.AsNoTracking().Include(p => p.CtXuatHuys).ThenInclude(line => line.SanPham)
            .ThenInclude(product => product.TonKho).Include(p => p.NguoiLap).SingleOrDefaultAsync(p => p.Id == id, ct);
        if (row is null) return NotFound();
        return View("~/Views/Inventory/Disposal.cshtml", row);
    }

    [HttpPost("XuatHuy/{id}/Gui")]
    [Authorize(Policy = MaQuyen.KiemKeXuatHuyLap)]
    public async Task<IActionResult> SubmitDisposal(long id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        if (await LockDisposalAsync(id, ct) != "NHAP") return Conflict("Phiếu không còn là nháp.");
        var row = await db.PhieuXuatHuys.SingleAsync(p => p.Id == id, ct);
        row.TrangThai = "CHO_DUYET";
        row.GuiDuyetLuc = Now;
        row.PhienBan++;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RedirectToAction(nameof(Disposal), new { id });
    }

    [HttpPost("XuatHuy/{id}/Duyet")]
    [Authorize(Policy = MaQuyen.KiemKeXuatHuyDuyet)]
    public async Task<IActionResult> ReviewDisposal(long id, bool approve, string? reason, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!approve && InvalidReason(reason)) return Issue("Nhập lý do từ chối.", nameof(Disposal), id);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        if (await LockDisposalAsync(id, ct) != "CHO_DUYET") return Conflict("Phiếu không còn chờ duyệt.");
        var row = await db.PhieuXuatHuys.Include(p => p.CtXuatHuys).SingleAsync(p => p.Id == id, ct);
        if (approve)
        {
            try
            {
                var result = await stock.GhiNhanBienDongAsync(new(LoaiBienDong.XUAT_HUY, checked((int)id),
                    row.MaPhieu, User.LayTaiKhoanId(), row.CtXuatHuys.Select(line => new DongBienDong(
                        checked((int)line.SanPhamId), SoLuong: checked((int)line.SoLuong))).ToArray()), ct);
                var changes = result.Dong.ToDictionary(line => checked((long)line.SanPhamId));
                foreach (var line in row.CtXuatHuys) line.GiaVonLucDuyet = changes[line.SanPhamId].GiaVonApDung;
            }
            catch (LoiDichVuKho ex) { return Issue(ex.Message, nameof(Disposal), id); }
        }
        row.TrangThai = approve ? "DA_XUAT_HUY" : "BI_TU_CHOI";
        row.NguoiDuyetId = Actor;
        row.DuyetLuc = Now;
        row.LyDoTuChoi = approve ? null : reason!.Trim();
        row.PhienBan++;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RedirectToAction(nameof(Disposal), new { id });
    }

    private async Task<string?> LockCountAsync(long id, CancellationToken ct) =>
        (string?)await db.DocMotGiaTriAsync("SELECT trang_thai FROM phieu_kiem_ke WHERE id=@id FOR UPDATE", ct, ("@id", id));

    private async Task<string?> LockDisposalAsync(long id, CancellationToken ct) =>
        (string?)await db.DocMotGiaTriAsync("SELECT trang_thai FROM phieu_xuat_huy WHERE id=@id FOR UPDATE", ct, ("@id", id));

    private async Task<string> NextCodeAsync(string prefix, CancellationToken ct)
    {
        var period = Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        await db.ChayAsync("""
            INSERT INTO bo_dem_chung_tu (tien_to,ky,so_cuoi) VALUES (@prefix,@period,1)
            ON DUPLICATE KEY UPDATE so_cuoi=so_cuoi+1
            """, ct, ("@prefix", prefix), ("@period", period));
        var number = Convert.ToInt64(await db.DocMotGiaTriAsync(
            "SELECT so_cuoi FROM bo_dem_chung_tu WHERE tien_to=@prefix AND ky=@period",
            ct, ("@prefix", prefix), ("@period", period)));
        return FormattableString.Invariant($"{prefix}-{period}-{number:D3}");
    }

    private IActionResult Issue(string message, string action = nameof(Index), long? id = null)
    {
        TempData["Loi"] = message;
        return RedirectToAction(action, new { id });
    }

    private static bool InvalidReason(string? reason) => string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 255;
}

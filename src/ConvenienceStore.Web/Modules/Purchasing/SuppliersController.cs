using System.Data;
using System.Data.Common;
using System.ComponentModel.DataAnnotations;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing;

public sealed class SupplierInput
{
    public long Id { get; set; }
    public long Version { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Required, RegularExpression("^0[0-9]{9,10}$")] public string Phone { get; set; } = "";
    [StringLength(100)] public string? Contact { get; set; }
    [StringLength(100), EmailAddress] public string? Email { get; set; }
    [StringLength(255)] public string? Address { get; set; }
    [StringLength(14)] public string? TaxId { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}

public sealed record SupplierListRow(NhaCungCap Supplier, DateTime? LastReceipt);

[Authorize(Policy = MaQuyen.NhaCungCapQuanLy)]
[AutoValidateAntiforgeryToken]
[Route("NhaCungCap")]
public sealed class SuppliersController(CuaHangDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? status, int page = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || page is < 1 or > 100000) return BadRequest();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (status is not null and not ("HOAT_DONG" or "NGUNG_HOP_TAC")) return BadRequest();
        var query = db.NhaCungCaps.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = VietnameseSearch.Fold(q.Trim());
            query = query.Where(row => VietnameseSearch.Fold(row.Ma).Contains(term) || VietnameseSearch.Fold(row.Ten).Contains(term) || row.SoDienThoai.Contains(term));
        }
        if (status is "HOAT_DONG" or "NGUNG_HOP_TAC")
            query = query.Where(row => row.TrangThai == status);
        var rows = await query.OrderBy(row => row.Ten).ThenBy(row => row.Id).Skip((page - 1) * 50).Take(51)
            .Select(row => new SupplierListRow(row, row.PhieuNhaps.Max(receipt => receipt.XacNhanLuc))).ToListAsync(ct);
        ViewBag.Page = page;
        ViewBag.HasNext = rows.Count > 50;
        ViewBag.Term = q?.Trim();
        ViewBag.Status = status;
        return View("~/Views/Purchasing/Suppliers.cshtml", rows.Take(50).ToList());
    }

    [HttpGet("Sua/{id?}")]
    public async Task<IActionResult> Edit(long? id, CancellationToken ct)
    {
        if (!id.HasValue) return Form(new());
        var row = await db.NhaCungCaps.AsNoTracking().SingleOrDefaultAsync(supplier => supplier.Id == id, ct);
        if (row is null) return NotFound();
        return Form(new SupplierInput
        {
            Id = row.Id, Version = row.PhienBan, Name = row.Ten, Phone = row.SoDienThoai,
            Contact = row.NguoiLienHe, Email = row.Email, Address = row.DiaChi, TaxId = row.MaSoThue, Notes = row.GhiChu
        });
    }

    [HttpPost("Sua")]
    public async Task<IActionResult> Save(SupplierInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return Form(input);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        NhaCungCap row;
        if (input.Id == 0)
        {
            await db.ChayAsync($"""
                INSERT INTO bo_dem_chung_tu (tien_to,ky,so_cuoi) VALUES ('NCC','',1)
                {db.CounterUpsert()}
                """, ct);
            var number = Convert.ToInt64(await db.DocMotGiaTriAsync(
                "SELECT so_cuoi FROM bo_dem_chung_tu WHERE tien_to='NCC' AND ky=''", ct));
            row = new NhaCungCap { Ma = FormattableString.Invariant($"NCC-{number:D6}"), TrangThai = "HOAT_DONG" };
            db.NhaCungCaps.Add(row);
        }
        else
        {
            var version = await db.DocMotGiaTriAsync(
                "SELECT phien_ban FROM nha_cung_cap WHERE id=@id FOR UPDATE", ct, ("@id", input.Id));
            if (version is null) return NotFound();
            if (Convert.ToInt64(version) != input.Version) return Conflict("Nhà cung cấp đã thay đổi. Tải lại trước khi lưu.");
            row = await db.NhaCungCaps.SingleAsync(supplier => supplier.Id == input.Id, ct);
            row.PhienBan++;
        }

        row.Ten = input.Name.Trim();
        row.SoDienThoai = input.Phone;
        row.NguoiLienHe = input.Contact?.Trim();
        row.Email = input.Email?.Trim();
        row.DiaChi = input.Address?.Trim();
        row.MaSoThue = input.TaxId?.Trim();
        row.GhiChu = input.Notes?.Trim();
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is DbException databaseError &&
            TruyVanCsdl.LaLoiTrungKhoa(databaseError, "uq_ncc_sdt"))
        {
            ModelState.AddModelError(nameof(input.Phone), "Số điện thoại đã thuộc nhà cung cấp khác.");
            return Form(input);
        }
        TempData["ThongBao"] = "Đã lưu nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id}/Ngung")]
    [Authorize(Policy = MaQuyen.NhaCungCapNgung)]
    public async Task<IActionResult> Stop(long id, bool delete, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var exists = await db.DocMotGiaTriAsync("SELECT id FROM nha_cung_cap WHERE id=@id FOR UPDATE", ct, ("@id", id));
        if (exists is null) return NotFound();
        var row = await db.NhaCungCaps.SingleAsync(supplier => supplier.Id == id, ct);
        var receipts = db.PhieuNhaps.Where(receipt => receipt.NhaCungCapId == id);
        if (await receipts.AnyAsync(receipt => receipt.TrangThai == "NHAP", ct))
            return Conflict("Xử lý các phiếu nhập nháp trước.");
        if (delete)
        {
            if (await receipts.AnyAsync(ct)) return Conflict("Nhà cung cấp đã có lịch sử phiếu nhập.");
            db.NhaCungCaps.Remove(row);
        }
        else
        {
            row.TrangThai = "NGUNG_HOP_TAC";
            row.PhienBan++;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return RedirectToAction(nameof(Index));
    }

    private ViewResult Form(SupplierInput input) => View("~/Views/Purchasing/SupplierForm.cshtml", input);
}

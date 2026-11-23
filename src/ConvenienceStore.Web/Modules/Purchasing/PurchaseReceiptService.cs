using System.Data;
using System.Globalization;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Purchasing;

public sealed record PurchaseLineInput(long ProductId, int Quantity, long UnitCost);
public sealed record QuickPurchaseProduct(string? Name, long CategoryId, string? Unit, long SalePrice);

public sealed class PurchaseReceiptInput
{
    public long Version { get; set; }
    public long SupplierId { get; set; }
    public string? SupplierDocument { get; set; }
    public List<PurchaseLineInput> Lines { get; set; } = [];
}

public sealed class PurchaseReceiptService(CuaHangDbContext db, IDichVuKho inventory, TimeProvider clock)
{
    public async Task<SanPham> CreateProductAsync(QuickPurchaseProduct input, int actorId, CancellationToken ct)
    {
        var name = input.Name?.Trim();
        var unit = input.Unit?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 200 || string.IsNullOrEmpty(unit) ||
            unit.Length > 20 || input.SalePrice <= 0 || input.SalePrice % 100 != 0)
            throw new ArgumentException("Nhập tên, đơn vị và giá bán dương chia hết cho 100.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var category = await db.DocMotGiaTriAsync(
            "SELECT id FROM danh_muc WHERE id=@id AND trang_thai='HOAT_DONG' FOR SHARE", ct, ("@id", input.CategoryId));
        if (category is null) throw new ArgumentException("Chọn danh mục đang hoạt động.");
        await db.ChayAsync("""
            INSERT INTO bo_dem_chung_tu (tien_to,ky,so_cuoi) VALUES ('SP','',1)
            ON DUPLICATE KEY UPDATE so_cuoi=so_cuoi+1
            """, ct);
        var number = Convert.ToInt64(await db.DocMotGiaTriAsync(
            "SELECT so_cuoi FROM bo_dem_chung_tu WHERE tien_to='SP' AND ky=''", ct));
        var product = new SanPham
        {
            Ma = FormattableString.Invariant($"SP-{number:D6}"), TenSanPham = name, DonViTinh = unit,
            DanhMucId = input.CategoryId, NguoiTaoId = checked((long)actorId), TrangThai = "DANG_KINH_DOANH"
        };
        product.LichSuGia.Add(new LichSuGium
        {
            GiaBan = input.SalePrice, NguoiTaoId = checked((long)actorId),
            HieuLucTu = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime
        });
        db.SanPhams.Add(product);
        await db.SaveChangesAsync(ct);
        await inventory.KhoiTaoTonKhoAsync(checked((int)product.Id), ct: ct);
        await transaction.CommitAsync(ct);
        return product;
    }

    public async Task<long> CreateAsync(PurchaseReceiptInput input, int actorId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var now = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime;
        var period = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        await db.ChayAsync("""
            INSERT INTO bo_dem_chung_tu (tien_to,ky,so_cuoi) VALUES ('PN',@ky,1)
            ON DUPLICATE KEY UPDATE so_cuoi=so_cuoi+1
            """, ct, ("@ky", period));
        var serial = Convert.ToInt64(await db.DocMotGiaTriAsync(
            "SELECT so_cuoi FROM bo_dem_chung_tu WHERE tien_to='PN' AND ky=@ky", ct, ("@ky", period)));
        var receipt = new PhieuNhap
        {
            MaPhieu = FormattableString.Invariant($"PN-{period}-{serial:D3}"),
            NgayNhap = DateOnly.FromDateTime(now),
            NguoiLapId = checked((long)actorId),
            TrangThai = "NHAP"
        };
        await ApplyDraftAsync(receipt, input, ct);
        db.PhieuNhaps.Add(receipt);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return receipt.Id;
    }

    public async Task EditAsync(long id, PurchaseReceiptInput input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var receipt = await LockDraftAsync(id, ct);
        if (receipt.PhienBan != input.Version) throw new InvalidOperationException("Phiếu đã thay đổi. Tải lại trước khi lưu.");
        await ApplyDraftAsync(receipt, input, ct);
        receipt.PhienBan++;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task CancelAsync(long id, string? reason, int actorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 255)
            throw new ArgumentException("Nhập lý do hủy, tối đa 255 ký tự.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var receipt = await LockDraftAsync(id, ct);
        receipt.TrangThai = "DA_HUY";
        receipt.LyDoHuy = reason.Trim();
        receipt.NguoiHuyId = checked((long)actorId);
        receipt.HuyLuc = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime;
        receipt.PhienBan++;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task ApplyDraftAsync(PhieuNhap receipt, PurchaseReceiptInput input, CancellationToken ct)
    {
        if (input.Lines.Count is < 1 or > 100 ||
            input.Lines.Any(line => line.Quantity <= 0 || line.UnitCost < 0 || line.ProductId <= 0 || line.ProductId > int.MaxValue))
            throw new ArgumentException("Nhập từ 1 đến 100 dòng sản phẩm, với số lượng dương.");
        if (input.SupplierDocument?.Trim().Length > 50)
            throw new ArgumentException("Số chứng từ nhà cung cấp tối đa 50 ký tự.");

        var groups = input.Lines.GroupBy(line => line.ProductId).ToArray();
        if (groups.Any(group => group.Select(line => line.UnitCost).Distinct().Count() > 1))
            throw new ArgumentException("Mỗi sản phẩm trong phiếu chỉ có một giá nhập.");
        var lines = groups.Select(group => new PurchaseLineInput(
            group.Key, group.Sum(line => line.Quantity), group.First().UnitCost)).ToArray();

        var supplier = await db.DocMotGiaTriAsync(
            "SELECT id FROM nha_cung_cap WHERE id=@id AND trang_thai='HOAT_DONG' FOR SHARE",
            ct, ("@id", input.SupplierId));
        if (supplier is null || supplier is DBNull)
            throw new ArgumentException("Chọn nhà cung cấp đang hoạt động.");

        var ids = lines.Select(line => line.ProductId).ToArray();
        var products = await db.SanPhams.Where(product => ids.Contains(product.Id) && product.TrangThai == "DANG_KINH_DOANH")
            .ToDictionaryAsync(product => product.Id, ct);
        if (products.Count != ids.Length)
            throw new ArgumentException("Có sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");

        receipt.NhaCungCapId = input.SupplierId;
        receipt.SoChungTuNcc = string.IsNullOrWhiteSpace(input.SupplierDocument) ? null : input.SupplierDocument.Trim();
        receipt.TongTien = lines.Aggregate(0L, (total, line) => checked(total + (long)line.Quantity * line.UnitCost));
        var existing = receipt.CtPhieuNhaps.ToDictionary(line => line.SanPhamId);
        db.CtPhieuNhaps.RemoveRange(existing.Values.Where(line => !products.ContainsKey(line.SanPhamId)));
        foreach (var line in lines)
        {
            if (!existing.TryGetValue(line.ProductId, out var detail))
            {
                detail = new CtPhieuNhap
                {
                    SanPhamId = line.ProductId, TenSanPham = products[line.ProductId].TenSanPham,
                    DonViTinh = products[line.ProductId].DonViTinh
                };
                receipt.CtPhieuNhaps.Add(detail);
            }
            detail.SoLuong = (long)line.Quantity;
            detail.GiaNhap = line.UnitCost;
        }
    }

    private async Task<PhieuNhap> LockDraftAsync(long id, CancellationToken ct)
    {
        var status = await db.DocMotGiaTriAsync(
            "SELECT trang_thai FROM phieu_nhap WHERE id=@id FOR UPDATE", ct, ("@id", id));
        if (status as string != "NHAP")
            throw new InvalidOperationException("Phiếu không tồn tại hoặc đã rời trạng thái nháp.");

        return await db.PhieuNhaps.Include(row => row.CtPhieuNhaps).SingleAsync(row => row.Id == id, ct);
    }

    public async Task<IReadOnlyList<string>> ConfirmAsync(long id, int actorId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var receipt = await LockDraftAsync(id, ct);
        if (receipt.CtPhieuNhaps.Count == 0)
            throw new InvalidOperationException("Phiếu nhập chưa có dòng sản phẩm.");

        var productIds = receipt.CtPhieuNhaps.Select(line => line.SanPhamId).ToArray();
        var changes = receipt.CtPhieuNhaps.Select(line => new DongBienDong(
            checked((int)line.SanPhamId), checked((int)line.SoLuong), GiaVon: line.GiaNhap)).ToArray();
        var (productParameters, values) = TruyVanCsdl.TaoThamSoIn("sp", changes.Select(change => change.SanPhamId));
        var products = await db.DocDanhSachAsync(
            $"SELECT trang_thai FROM san_pham WHERE id IN ({productParameters}) ORDER BY id FOR SHARE",
            reader => reader.GetString(0), ct, values);
        if (products.Any(status => status != "DANG_KINH_DOANH"))
            throw new InvalidOperationException("Phiếu có sản phẩm đã ngừng kinh doanh. Mở bán lại trước khi xác nhận.");

        var result = await inventory.GhiNhanBienDongAsync(
            new(LoaiBienDong.NHAP_HANG, checked((int)id), receipt.MaPhieu, actorId, changes), ct);

        receipt.TrangThai = "DA_NHAP_KHO";
        receipt.NguoiXacNhanId = checked((long)actorId);
        receipt.XacNhanLuc = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime;
        receipt.PhienBan = checked(receipt.PhienBan + 1);
        await db.SaveChangesAsync(ct);
        var prices = await db.VGiaBanHienHanhs.Where(price => price.SanPhamId.HasValue && price.GiaBan.HasValue && productIds.Contains(price.SanPhamId.Value))
            .ToDictionaryAsync(price => price.SanPhamId!.Value, price => price.GiaBan!.Value, ct);
        var warnings = receipt.CtPhieuNhaps.Where(line =>
            prices.TryGetValue(line.SanPhamId, out var price) &&
            result.Dong.Any(change => change.SanPhamId == line.SanPhamId && change.GiaVonSau >= price))
            .Select(line => line.TenSanPham).ToArray();
        await transaction.CommitAsync(ct);
        return warnings;
    }
}

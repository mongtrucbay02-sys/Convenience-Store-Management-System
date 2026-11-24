using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Products.Services;

public static class ProductLifecycleRules
{
    public static void Validate(ProductLifecycleCommand input)
    {
        ArgumentNullException.ThrowIfNull(input); var errors = new List<LoiTruong>();
        if (input.PhienBan <= 0) errors.Add(new("phienBan", "Cần phiên bản hiện tại."));
        if (!input.XacNhan) errors.Add(new("xacNhan", "Cần xác nhận thao tác sau khi xem tồn kho và các cảnh báo."));
        if (errors.Count > 0) throw LoiNghiepVu.DuLieuKhongHopLe(errors);
    }
    public static void EnsureOperation(ProductLifecyclePreview row, ProductLifecycleOperation operation, ProductLifecycleCommand input)
    {
        Validate(input);
        if (row.PhienBan != input.PhienBan) throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, ThongBaoLoi.XungDotDuLieu);
        if (!row.CoDuLieuTonKho) throw LoiNghiepVu.XungDot(MaLoiChung.XungDotDuLieu, "Thiếu dữ liệu tồn kho. Cần đối soát trước khi thay đổi.");
        if (operation == ProductLifecycleOperation.Delete) {
            if (row.CoThamChieu) throw LoiNghiepVu.XungDot("SAN_PHAM_DA_THAM_CHIEU", "Sản phẩm đã có giao dịch hoặc tham chiếu. Hãy ngừng kinh doanh thay vì xóa hẳn.");
            if (row.TonKho > 0) throw LoiNghiepVu.XungDot("SAN_PHAM_CON_TON", "Sản phẩm còn tồn kho. Hãy ngừng kinh doanh thay vì xóa hẳn.");
        }
        else {
            var target = operation == ProductLifecycleOperation.Discontinue ? "NGUNG_KINH_DOANH" : "DANG_KINH_DOANH";
            if (row.TrangThai == target) throw LoiNghiepVu.XungDot(MaLoiChung.DaDuocXuLy, "Sản phẩm đã ở trạng thái này.");
            if (operation == ProductLifecycleOperation.Discontinue && row.TonKho > 0 && !input.XacNhanTonKho)
                throw LoiNghiepVu.XungDot("CAN_XAC_NHAN_CON_TON", "Sản phẩm còn tồn kho, cần xác nhận thêm.", new { row.TonKho });
        }
    }
}

public sealed class ProductLifecycleService(IProductLifecycleRepository products, ILogger<ProductLifecycleService> logger)
{
    private static void Admin(string role) { if (role != "AD") throw new LoiNghiepVu(MaLoiChung.KhongDuQuyen, 403, "Chỉ AD được ngừng, mở bán lại hoặc xóa sản phẩm."); }
    public async Task<ProductLifecyclePreview> PreviewAsync(long id, string role, CancellationToken ct = default)
    { Admin(role); return await products.PreviewAsync(id, ct) ?? throw LoiNghiepVu.KhongTimThay("Sản phẩm không còn tồn tại"); }
    public async Task<ProductLifecyclePreview> ExecuteAsync(long id, ProductLifecycleOperation operation, ProductLifecycleCommand input, long actorId, string role, CancellationToken ct = default)
    {
        Admin(role); ProductLifecycleRules.Validate(input); ct.ThrowIfCancellationRequested();
        var result = await products.ExecuteAsync(id, operation, input, ct);
        logger.LogInformation("Product {ProductId}: {Operation} by account {ActorId}", id, operation, actorId); return result;
    }
}

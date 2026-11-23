namespace ConvenienceStore.Web.Modules.Products.DTOs;

public enum ProductLifecycleOperation { Discontinue, Reactivate, Delete }
public sealed record ProductLifecycleCommand
{
    public long PhienBan { get; init; }
    public bool XacNhan { get; init; }
    public bool XacNhanTonKho { get; init; }
}
public sealed record ProductLifecyclePreview(long Id, string TenSanPham, string TrangThai, long PhienBan,
    int TonKho, int DonTamGiu, int PhieuNhapNhap, bool CoThamChieu, string DanhMucTrangThai, bool CoDuLieuTonKho = true);

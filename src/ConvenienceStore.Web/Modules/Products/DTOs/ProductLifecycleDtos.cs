namespace ConvenienceStore.Web.Modules.Products.DTOs;

public enum ProductLifecycleOperation { Discontinue, Reactivate, Delete }
public sealed record ProductLifecycleCommand
{
    public uint PhienBan { get; init; }
    public bool XacNhan { get; init; }
    public bool XacNhanTonKho { get; init; }
}
public sealed record ProductLifecyclePreview(uint Id, string TenSanPham, string TrangThai, uint PhienBan,
    int TonKho, int DonTamGiu, int PhieuNhapNhap, bool CoThamChieu, string DanhMucTrangThai, bool CoDuLieuTonKho = true);

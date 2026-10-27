namespace ConvenienceStore.Web.Modules.Inventory.Models;

public sealed record TonKhoHienTai(int SanPhamId, int SoLuongTon, decimal GiaVonBinhQuan, int NguongToiThieu)
{
    public bool DuoiNguong => NguongToiThieu > 0 && SoLuongTon <= NguongToiThieu;
}

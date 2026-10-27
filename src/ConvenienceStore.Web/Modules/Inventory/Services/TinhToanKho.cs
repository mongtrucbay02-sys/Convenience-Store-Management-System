using ConvenienceStore.Web.Modules.Inventory.Models;

namespace ConvenienceStore.Web.Modules.Inventory.Services;

public static class TinhToanKho
{
    public static IReadOnlySet<LoaiBienDong> LoaiDangHoTro { get; } = new HashSet<LoaiBienDong>
    {
        LoaiBienDong.NHAP_HANG, LoaiBienDong.BAN_HANG, LoaiBienDong.KIEM_KE, LoaiBienDong.XUAT_HUY,
    };

    public static bool CanGiaVon(LoaiBienDong loai) =>
        loai is LoaiBienDong.NHAP_HANG or LoaiBienDong.TRA_HANG or LoaiBienDong.HUY_HOA_DON;

    public static int SoLuongThayDoi(LoaiBienDong loai, DongBienDong dong, int tonHienTai) => loai switch
    {
        LoaiBienDong.NHAP_HANG or LoaiBienDong.TRA_HANG or LoaiBienDong.HUY_HOA_DON => dong.SoLuong!.Value,
        LoaiBienDong.BAN_HANG or LoaiBienDong.XUAT_HUY => -dong.SoLuong!.Value,
        LoaiBienDong.KIEM_KE => dong.SoLuongThucTe!.Value - tonHienTai,
        _ => throw new ArgumentOutOfRangeException(nameof(loai), loai, null),
    };

    public static (decimal GiaVonSau, decimal GiaVonApDung) GiaVon(
        LoaiBienDong loai, int tonTruoc, decimal giaVonTruoc, int soLuongThayDoi, decimal? giaVonVao)
    {
        if (!CanGiaVon(loai))
            return (giaVonTruoc, giaVonTruoc);

        var giaVao = LamTron(giaVonVao!.Value);
        if (tonTruoc <= 0)
            return (giaVao, giaVao);

        var binhQuan = (tonTruoc * giaVonTruoc + soLuongThayDoi * giaVao) / (tonTruoc + soLuongThayDoi);
        return (LamTron(binhQuan), giaVao);
    }

    public static decimal LamTron(decimal giaTri) => Math.Round(giaTri, 2, MidpointRounding.AwayFromZero);
}

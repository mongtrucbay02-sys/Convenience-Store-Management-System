using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using Xunit;

namespace ConvenienceStore.Web.Tests.Inventory;

[Trait("Category", "DichVuKho")]
public sealed class TinhToanKhoTests
{
    [Fact]
    public void NhapHang_TonTruoc0_GiaVonBangGiaNhap() =>
        Assert.Equal((15000m, 15000m), TinhToanKho.GiaVon(LoaiBienDong.NHAP_HANG, 0, 0m, 12, 15000m));

    [Fact]
    public void NhapHang_BinhQuanGiaQuyen_Ut02() =>
        Assert.Equal((9000m, 9500m), TinhToanKho.GiaVon(LoaiBienDong.NHAP_HANG, 10, 8000m, 20, 9500m));

    [Fact]
    public void NhapHang_LamTronNuaLen2ChuSo() =>
        Assert.Equal(10285.71m, TinhToanKho.GiaVon(LoaiBienDong.NHAP_HANG, 4, 10000m, 3, 10666.66m).GiaVonSau);

    [Theory]
    [InlineData(LoaiBienDong.BAN_HANG)]
    [InlineData(LoaiBienDong.XUAT_HUY)]
    [InlineData(LoaiBienDong.KIEM_KE)]
    public void LoaiKhongDoiGiaVon_ApDungGiaVonHienTai(LoaiBienDong loai) =>
        Assert.Equal((9000m, 9000m), TinhToanKho.GiaVon(loai, 5, 9000m, -2, null));

    [Fact]
    public void SoLuongThayDoi_TheoLoai()
    {
        var dong = new DongBienDong(1, SoLuong: 3, SoLuongThucTe: 8);
        Assert.Equal(3, TinhToanKho.SoLuongThayDoi(LoaiBienDong.NHAP_HANG, dong, 10));
        Assert.Equal(-3, TinhToanKho.SoLuongThayDoi(LoaiBienDong.BAN_HANG, dong, 10));
        Assert.Equal(-3, TinhToanKho.SoLuongThayDoi(LoaiBienDong.XUAT_HUY, dong, 10));
        Assert.Equal(-2, TinhToanKho.SoLuongThayDoi(LoaiBienDong.KIEM_KE, dong, 10));
    }
}

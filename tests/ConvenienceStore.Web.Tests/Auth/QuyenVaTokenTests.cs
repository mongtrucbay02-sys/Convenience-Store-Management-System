using ConvenienceStore.Web.Modules.Auth.Security;
using Xunit;

namespace ConvenienceStore.Web.Tests.Auth;

[Trait("Category", "XacThuc")]
public sealed class QuyenVaTokenTests
{
    [Fact]
    public void MaQuyen_Du27Ma_KhongTrung()
    {
        Assert.Equal(27, MaQuyen.TatCa.Count);
        Assert.Equal(27, MaQuyen.TatCa.Distinct().Count());
    }

    [Fact]
    public void MoiMaQuyen_ThuocItNhatMotVaiTro()
    {
        var daGan = BangQuyen.CacVaiTro.SelectMany(BangQuyen.LayQuyen).ToHashSet();
        Assert.All(MaQuyen.TatCa, q => Assert.Contains(q, daGan));
    }

    [Theory]
    [InlineData("AD", MaQuyen.QuanTriTaiKhoan, true)]
    [InlineData("TN", MaQuyen.QuanTriTaiKhoan, false)]
    [InlineData("TN", MaQuyen.BanHang, true)]
    [InlineData("AD", MaQuyen.BanHang, false)]
    [InlineData("NK", MaQuyen.PhieuNhapLap, true)]
    [InlineData("AD", MaQuyen.PhieuNhapLap, false)]
    [InlineData("NK", MaQuyen.KiemKeXuatHuyDuyet, false)]
    [InlineData("TN", MaQuyen.TaiKhoanCaNhan, true)]
    public void BangQuyen_DungMaTranDacTa(string vaiTro, string quyen, bool coQuyen) =>
        Assert.Equal(coQuyen, BangQuyen.LayQuyen(vaiTro).Contains(quyen));

    [Theory]
    [InlineData("AD", "DASHBOARD")]
    [InlineData("TN", "POS")]
    [InlineData("NK", "KHO")]
    public void ManHinhChinh_TheoVaiTro(string vaiTro, string manHinh) =>
        Assert.Equal(manHinh, BangQuyen.ManHinhChinh(vaiTro));

    [Fact]
    public void TokenPhien_NgauNhien_BamDu64KyTu_XsrfKhop()
    {
        var token = TokenPhien.TaoToken();
        Assert.NotEqual(token, TokenPhien.TaoToken());
        Assert.Equal(64, TokenPhien.Bam(token).Length);
        Assert.True(TokenPhien.KhopXsrf(token, TokenPhien.TaoXsrf(token)));
        Assert.False(TokenPhien.KhopXsrf(token, TokenPhien.TaoXsrf(TokenPhien.TaoToken())));
        Assert.False(TokenPhien.KhopXsrf(token, null));
        Assert.NotEqual(TokenPhien.Bam(token), TokenPhien.TaoXsrf(token));
    }

    [Theory]
    [InlineData("MatKhau123", true)]
    [InlineData("matkhau1", true)]
    [InlineData("12345678", false)]
    [InlineData("MatKhauDai", false)]
    [InlineData("Mk1", false)]
    public void QuyTacMatKhau_BrAuth04(string matKhau, bool hopLe) =>
        Assert.Equal(hopLe, QuyTacTaiKhoan.MatKhauHopLe(matKhau));
}

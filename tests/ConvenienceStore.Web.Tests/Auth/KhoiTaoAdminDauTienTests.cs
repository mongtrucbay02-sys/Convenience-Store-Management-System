using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConvenienceStore.Web.Tests.Auth;

/// <summary>CSDL tạm riêng cho test T4-16 (không có dữ liệu nền, để tự kiểm soát tài khoản Admin).</summary>
[CollectionDefinition(Ten)]
public sealed class AdminDauTienCollection : ICollectionFixture<CsdlMySqlTam>
{
    public const string Ten = "AdminDauTien";
}

/// <summary>
/// T4-16: tài khoản Admin đầu tiên (auth-giai-thich.md mục 9.1). Chạy trên MySQL thật vì cần
/// CHECK, collation không phân biệt hoa thường và dữ liệu vai_tro do migration tạo.
/// </summary>
[Collection(AdminDauTienCollection.Ten)]
[Trait("Category", "AdminDauTien")]
public sealed class KhoiTaoAdminDauTienTests(CsdlMySqlTam csdl) : IAsyncLifetime
{
    private const string MatKhauHopLe = "MatKhau123";

    // Mỗi test bắt đầu với bảng tai_khoan trống (bảng này không có trigger chặn xóa).
    public Task InitializeAsync() => csdl.ChayAsync("DELETE FROM tai_khoan");

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ChuaCoAdmin_TaoDungMotAdmin_BatBuocDoiMatKhau()
    {
        var kq = await ChayAsync(new AdminDauTienOptions { TenDangNhap = "admin", MatKhauTam = MatKhauHopLe });

        Assert.Equal(KetQuaKhoiTaoAdmin.DaTao, kq);
        Assert.Equal(1, await DemAdminAsync());
        await using var ketNoi = await csdl.MoKetNoiAsync();
        await using var lenh = CsdlMySqlTam.TaoLenh(ketNoi,
            """
            SELECT t.ho_ten, t.trang_thai, t.bat_buoc_doi_mk, t.so_lan_sai, t.mat_khau_bam,
                   TIMESTAMPDIFF(SECOND, t.ngay_tao, NOW())
            FROM tai_khoan t JOIN vai_tro v ON v.id = t.vai_tro_id
            WHERE t.ten_dang_nhap = 'admin' AND v.ma = 'AD'
            """);
        await using var doc = await lenh.ExecuteReaderAsync();
        Assert.True(await doc.ReadAsync());
        Assert.Equal("Quản trị viên", doc.GetString(0));
        Assert.Equal("HOAT_DONG", doc.GetString(1));
        Assert.True(doc.GetBoolean(2));
        Assert.Equal(0, Convert.ToInt32(doc.GetValue(3)));

        var bam = doc.GetString(4);
        Assert.NotEqual(MatKhauHopLe, bam);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<object>().VerifyHashedPassword(new object(), bam, MatKhauHopLe));
        Assert.Equal(PasswordVerificationResult.Failed,
            new PasswordHasher<object>().VerifyHashedPassword(new object(), bam, "SaiMatKhau1"));

        // ngay_tao ghi theo giờ Việt Nam, cùng múi giờ với NOW() của phiên.
        Assert.InRange(Convert.ToInt64(doc.GetValue(5)), 0L, 300L);
    }

    [Fact]
    public async Task ChayLai_KhongTaoTrung()
    {
        var cauHinh = new AdminDauTienOptions { TenDangNhap = "admin", MatKhauTam = MatKhauHopLe };

        Assert.Equal(KetQuaKhoiTaoAdmin.DaTao, await ChayAsync(cauHinh));
        Assert.Equal(KetQuaKhoiTaoAdmin.DaCoAdmin, await ChayAsync(cauHinh));
        Assert.Equal(1, await DemAdminAsync());
    }

    [Fact]
    public async Task DaCoAdminKhac_DuDangBiKhoa_KhongTaoThem()
    {
        await csdl.ChayAsync(
            """
            INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id, trang_thai, ly_do_khoa)
            VALUES ('quanly', 'x', 'Quản lý', (SELECT id FROM vai_tro WHERE ma = 'AD'), 'DA_KHOA', 'Test')
            """);

        var kq = await ChayAsync(new AdminDauTienOptions { TenDangNhap = "admin", MatKhauTam = MatKhauHopLe });

        Assert.Equal(KetQuaKhoiTaoAdmin.DaCoAdmin, kq);
        Assert.Equal(1, await DemAdminAsync());
    }

    [Theory]
    [InlineData(null, MatKhauHopLe)]
    [InlineData("admin", null)]
    [InlineData("", MatKhauHopLe)]
    [InlineData("   ", MatKhauHopLe)]
    public async Task ThieuCauHinh_KhongTao(string? ten, string? matKhau)
    {
        var kq = await ChayAsync(new AdminDauTienOptions { TenDangNhap = ten, MatKhauTam = matKhau });

        Assert.Equal(KetQuaKhoiTaoAdmin.ThieuCauHinh, kq);
        Assert.Equal(0, await DemTaiKhoanAsync());
    }

    [Theory]
    [InlineData("admin", "abcdefgh")]   // không có chữ số (BR-AUTH-04)
    [InlineData("admin", "12345678")]   // không có chữ cái
    [InlineData("admin", "abc123")]     // ngắn hơn 8 ký tự
    [InlineData("ad min", MatKhauHopLe)] // có dấu cách
    [InlineData("quản_lý", MatKhauHopLe)] // có dấu tiếng Việt
    [InlineData("ad", MatKhauHopLe)]     // ngắn hơn 3 ký tự
    public async Task CauHinhSaiQuyTac_KhongTao(string ten, string matKhau)
    {
        var kq = await ChayAsync(new AdminDauTienOptions { TenDangNhap = ten, MatKhauTam = matKhau });

        Assert.Equal(KetQuaKhoiTaoAdmin.CauHinhKhongHopLe, kq);
        Assert.Equal(0, await DemTaiKhoanAsync());
    }

    [Fact]
    public async Task TenDaThuocThuNgan_KhongPhanBietHoaThuong_KhongTao()
    {
        await csdl.ChayAsync(
            """
            INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id)
            VALUES ('admin', 'x', 'Thu ngân', (SELECT id FROM vai_tro WHERE ma = 'TN'))
            """);

        var kq = await ChayAsync(new AdminDauTienOptions { TenDangNhap = "ADMIN", MatKhauTam = MatKhauHopLe });

        Assert.Equal(KetQuaKhoiTaoAdmin.TrungTenDangNhap, kq);
        Assert.Equal(0, await DemAdminAsync());
    }

    [Fact]
    public async Task HoTenTuCauHinh_DuocLuu()
    {
        var kq = await ChayAsync(new AdminDauTienOptions
        {
            TenDangNhap = "thang.pham", MatKhauTam = MatKhauHopLe, HoTen = "  Phạm Gia Thăng  ",
        });

        Assert.Equal(KetQuaKhoiTaoAdmin.DaTao, kq);
        Assert.Equal("Phạm Gia Thăng", await csdl.DocMotGiaTriAsync(
            "SELECT ho_ten FROM tai_khoan WHERE ten_dang_nhap = 'thang.pham'"));
    }

    private async Task<KetQuaKhoiTaoAdmin> ChayAsync(AdminDauTienOptions cauHinh)
    {
        var khoiTao = new KhoiTaoAdminDauTien(Options.Create(cauHinh), NullLogger<KhoiTaoAdminDauTien>.Instance);
        await using var ketNoi = await csdl.MoKetNoiAsync();
        return await khoiTao.ChayAsync(ketNoi);
    }

    private async Task<int> DemAdminAsync() => Convert.ToInt32(await csdl.DocMotGiaTriAsync(
        "SELECT COUNT(*) FROM tai_khoan t JOIN vai_tro v ON v.id = t.vai_tro_id WHERE v.ma = 'AD'"));

    private async Task<int> DemTaiKhoanAsync() =>
        Convert.ToInt32(await csdl.DocMotGiaTriAsync("SELECT COUNT(*) FROM tai_khoan"));
}

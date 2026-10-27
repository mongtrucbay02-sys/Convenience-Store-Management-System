using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Shared.Errors;
using Xunit;

namespace ConvenienceStore.Web.Tests.Auth;

[Collection(CsdlXacThucCollection.Ten)]
[Trait("Category", "XacThuc")]
public sealed class DichVuXacThucTests(CsdlXacThucFixture csdl) : IAsyncLifetime
{
    private readonly DuLieuTestXacThuc _duLieu = new(csdl);
    private readonly List<CuaHangDbContext> _dbDaTao = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var db in _dbDaTao)
            await db.DisposeAsync();
    }

    [Fact]
    public async Task DangNhap_DungMatKhau_TaoPhien_TraQuyenVaManHinh()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");

        var kq = await DangNhapAsync(tk.TenDangNhap, tk.MatKhau);

        Assert.Equal(tk.Id, kq.KetQua.NguoiDung.Id);
        Assert.Equal("TN", kq.KetQua.NguoiDung.VaiTro.Ma);
        Assert.Contains(MaQuyen.BanHang, kq.KetQua.NguoiDung.Quyen);
        Assert.Equal("POS", kq.KetQua.ManHinhChinh);
        Assert.False(kq.KetQua.BatBuocDoiMatKhau);
        Assert.Equal(1L, Convert.ToInt64(await csdl.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM phien_dang_nhap WHERE tai_khoan_id = @id AND token_bam = @bam AND huy_luc IS NULL",
            ("@id", tk.Id), ("@bam", TokenPhien.Bam(kq.TokenPhien)))));
        Assert.NotNull(await _duLieu.DocCotAsync(tk.Id, "lan_dang_nhap_cuoi"));
    }

    [Fact]
    public async Task DangNhap_TenDangNhapKhongPhanBietHoaThuong()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("NK");
        var kq = await DangNhapAsync(tk.TenDangNhap.ToUpperInvariant(), tk.MatKhau);
        Assert.Equal("KHO", kq.KetQua.ManHinhChinh);
    }

    [Fact]
    public async Task DangNhap_ThieuTruong_422()
    {
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaoDichVu().DangNhapAsync(new YeuCauDangNhap(" ", null)));
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, loi.Ma);
        Assert.Equal(422, loi.MaHttp);
        var chiTiet = Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(loi.ChiTiet);
        Assert.Equal(2, chiTiet.Count);
    }

    [Fact]
    public async Task DangNhap_VuotDoDaiToiDa_422()
    {
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaoDichVu().DangNhapAsync(new YeuCauDangNhap(new string('a', 51), new string('b', 129))));
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, loi.Ma);
        Assert.Equal(2, Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(loi.ChiTiet).Count);
    }

    [Fact]
    public async Task DangNhap_KhongCoTenDangNhap_401CungCauVoiSaiMatKhau()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var khongCo = await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync("khong.co.ai.ten.nay", "MatKhau123"));
        var saiMatKhau = await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync(tk.TenDangNhap, "SaiMatKhau1"));

        Assert.Equal(MaLoiAuth.SaiThongTinDangNhap, khongCo.Ma);
        Assert.Equal(401, khongCo.MaHttp);
        Assert.Equal(khongCo.Message, saiMatKhau.Message);
    }

    [Fact]
    public async Task DangNhap_Sai4Lan_401_Lan5_KhoaTam15Phut()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        for (var lan = 1; lan <= 4; lan++)
        {
            var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync(tk.TenDangNhap, "SaiMatKhau1"));
            Assert.Equal(MaLoiAuth.SaiThongTinDangNhap, loi.Ma);
            Assert.Equal(lan, Convert.ToInt32(await _duLieu.DocCotAsync(tk.Id, "so_lan_sai")));
        }

        var lan5 = await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync(tk.TenDangNhap, "SaiMatKhau1"));

        Assert.Equal(MaLoiAuth.TaiKhoanKhoaTam, lan5.Ma);
        Assert.Equal(423, lan5.MaHttp);
        Assert.Contains("15 phút", lan5.Message);
        var conLai = Convert.ToInt64(await csdl.DocMotGiaTriAsync(
            "SELECT TIMESTAMPDIFF(SECOND, NOW(), khoa_tam_den) FROM tai_khoan WHERE id = @id", ("@id", tk.Id)));
        Assert.InRange(conLai, 14 * 60L, 15 * 60L);
    }

    [Fact]
    public async Task DangNhap_DangKhoaTam_DungMatKhauVan423_KhongDoiBoDem()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        await csdl.ChayAsync(
            "UPDATE tai_khoan SET khoa_tam_den = NOW() + INTERVAL 10 MINUTE, so_lan_sai = 0 WHERE id = @id", ("@id", tk.Id));

        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync(tk.TenDangNhap, tk.MatKhau));
        await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync(tk.TenDangNhap, "SaiMatKhau1"));

        Assert.Equal(MaLoiAuth.TaiKhoanKhoaTam, loi.Ma);
        Assert.Contains("10 phút", loi.Message);
        Assert.Equal(0, Convert.ToInt32(await _duLieu.DocCotAsync(tk.Id, "so_lan_sai")));
    }

    [Fact]
    public async Task DangNhap_HetKhoaTam_DungMatKhau_ThanhCong_BoDemVe0()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        await csdl.ChayAsync(
            "UPDATE tai_khoan SET khoa_tam_den = NOW() - INTERVAL 1 MINUTE, so_lan_sai = 3 WHERE id = @id", ("@id", tk.Id));

        await DangNhapAsync(tk.TenDangNhap, tk.MatKhau);

        Assert.Equal(0, Convert.ToInt32(await _duLieu.DocCotAsync(tk.Id, "so_lan_sai")));
        Assert.Null(await _duLieu.DocCotAsync(tk.Id, "khoa_tam_den"));
    }

    [Fact]
    public async Task DangNhap_BiAdminKhoa_DungMatKhau423_SaiMatKhau401()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("NK");
        await csdl.ChayAsync(
            "UPDATE tai_khoan SET trang_thai = 'DA_KHOA', ly_do_khoa = 'Nghỉ việc' WHERE id = @id", ("@id", tk.Id));

        var dung = await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync(tk.TenDangNhap, tk.MatKhau));
        var sai = await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync(tk.TenDangNhap, "SaiMatKhau1"));

        Assert.Equal(MaLoiAuth.TaiKhoanBiKhoa, dung.Ma);
        Assert.Equal(423, dung.MaHttp);
        Assert.Equal(MaLoiAuth.SaiThongTinDangNhap, sai.Ma);
    }

    [Fact]
    public async Task DangNhap_TaiKhoanMoi_TraCoBatBuocDoiMatKhau()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("AD", batBuocDoiMatKhau: true);
        var kq = await DangNhapAsync(tk.TenDangNhap, tk.MatKhau);
        Assert.True(kq.KetQua.BatBuocDoiMatKhau);
        Assert.Equal("DASHBOARD", kq.KetQua.ManHinhChinh);
    }

    [Fact]
    public async Task XacThucPhien_HopLe_TraNguoiDungVaGiaHan()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN", batBuocDoiMatKhau: true);
        var token = (await DangNhapAsync(tk.TenDangNhap, tk.MatKhau)).TokenPhien;
        await csdl.ChayAsync(
            "UPDATE phien_dang_nhap SET hoat_dong_cuoi = NOW() - INTERVAL 20 MINUTE WHERE token_bam = @bam",
            ("@bam", TokenPhien.Bam(token)));

        var kq = await TaoDichVu().XacThucPhienAsync(token);

        Assert.False(kq.HetPhien);
        Assert.NotNull(kq.NguoiDung);
        Assert.Equal(tk.Id, kq.NguoiDung!.TaiKhoanId);
        Assert.Equal("TN", kq.NguoiDung.MaVaiTro);
        Assert.True(kq.NguoiDung.BatBuocDoiMatKhau);
        var giayTuLanCuoi = Convert.ToInt64(await csdl.DocMotGiaTriAsync(
            "SELECT TIMESTAMPDIFF(SECOND, hoat_dong_cuoi, NOW()) FROM phien_dang_nhap WHERE token_bam = @bam",
            ("@bam", TokenPhien.Bam(token))));
        Assert.InRange(giayTuLanCuoi, 0L, 60L);
    }

    [Fact]
    public async Task XacThucPhien_TokenLa_KhongPhaiHetPhien()
    {
        var kq = await TaoDichVu().XacThucPhienAsync(TokenPhien.TaoToken());
        Assert.Null(kq.NguoiDung);
        Assert.False(kq.HetPhien);
    }

    [Fact]
    public async Task XacThucPhien_Qua30PhutKhongThaoTac_HetPhien_VaHuyPhien()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var token = (await DangNhapAsync(tk.TenDangNhap, tk.MatKhau)).TokenPhien;
        await csdl.ChayAsync(
            "UPDATE phien_dang_nhap SET hoat_dong_cuoi = NOW() - INTERVAL 31 MINUTE WHERE token_bam = @bam",
            ("@bam", TokenPhien.Bam(token)));

        var kq = await TaoDichVu().XacThucPhienAsync(token);

        Assert.Null(kq.NguoiDung);
        Assert.True(kq.HetPhien);
        Assert.NotNull(await csdl.DocMotGiaTriAsync(
            "SELECT huy_luc FROM phien_dang_nhap WHERE token_bam = @bam", ("@bam", TokenPhien.Bam(token))));
    }

    [Fact]
    public async Task XacThucPhien_29Phut50GiayKhongThaoTac_VanConHieuLuc()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var token = (await DangNhapAsync(tk.TenDangNhap, tk.MatKhau)).TokenPhien;
        await csdl.ChayAsync(
            "UPDATE phien_dang_nhap SET hoat_dong_cuoi = NOW() - INTERVAL 1790 SECOND WHERE token_bam = @bam",
            ("@bam", TokenPhien.Bam(token)));

        var kq = await TaoDichVu().XacThucPhienAsync(token);

        Assert.NotNull(kq.NguoiDung);
        Assert.False(kq.HetPhien);
    }

    [Fact]
    public async Task XacThucPhien_TaiKhoanBiKhoaSauKhiDangNhap_HetPhien()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var token = (await DangNhapAsync(tk.TenDangNhap, tk.MatKhau)).TokenPhien;
        await csdl.ChayAsync(
            "UPDATE tai_khoan SET trang_thai = 'DA_KHOA', ly_do_khoa = 'Test' WHERE id = @id", ("@id", tk.Id));

        var kq = await TaoDichVu().XacThucPhienAsync(token);

        Assert.Null(kq.NguoiDung);
        Assert.True(kq.HetPhien);
    }

    [Fact]
    public async Task XacThucPhien_DocVaiTroMoiTuCsdl()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var token = (await DangNhapAsync(tk.TenDangNhap, tk.MatKhau)).TokenPhien;
        await csdl.ChayAsync(
            "UPDATE tai_khoan SET vai_tro_id = (SELECT id FROM vai_tro WHERE ma = 'NK') WHERE id = @id", ("@id", tk.Id));

        var kq = await TaoDichVu().XacThucPhienAsync(token);

        Assert.Equal("NK", kq.NguoiDung!.MaVaiTro);
    }

    [Fact]
    public async Task DangXuat_HuyPhien_KhongDungLaiDuoc()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var token = (await DangNhapAsync(tk.TenDangNhap, tk.MatKhau)).TokenPhien;
        var phienId = (await TaoDichVu().XacThucPhienAsync(token)).NguoiDung!.PhienId;

        await TaoDichVu().DangXuatAsync(phienId);
        var kq = await TaoDichVu().XacThucPhienAsync(token);

        Assert.Null(kq.NguoiDung);
        Assert.True(kq.HetPhien);
    }

    [Fact]
    public async Task DoiMatKhau_ThanhCong_MatKhauCuHetDung_TatCoBatBuoc()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");

        await TaoDichVu().DoiMatKhauAsync(tk.Id, new YeuCauDoiMatKhau(tk.MatKhau, "MatKhauMoi456", "MatKhauMoi456"));

        await Assert.ThrowsAsync<LoiNghiepVu>(() => DangNhapAsync(tk.TenDangNhap, tk.MatKhau));
        await DangNhapAsync(tk.TenDangNhap, "MatKhauMoi456");
        Assert.False(Convert.ToBoolean(await _duLieu.DocCotAsync(tk.Id, "bat_buoc_doi_mk")));
    }

    [Fact]
    public async Task DoiMatKhau_BatBuocDoi_KhongCanMatKhauHienTai()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN", batBuocDoiMatKhau: true);

        await TaoDichVu().DoiMatKhauAsync(tk.Id, new YeuCauDoiMatKhau(null, "MatKhauMoi456", "MatKhauMoi456"));

        Assert.False(Convert.ToBoolean(await _duLieu.DocCotAsync(tk.Id, "bat_buoc_doi_mk")));
        Assert.Equal(2, Convert.ToInt32(await _duLieu.DocCotAsync(tk.Id, "phien_ban")));
    }

    [Fact]
    public async Task DoiMatKhau_SaiMatKhauHienTai_422SaiMatKhauHienTai()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaoDichVu().DoiMatKhauAsync(tk.Id, new YeuCauDoiMatKhau("SaiMatKhau1", "MatKhauMoi456", "MatKhauMoi456")));
        Assert.Equal(MaLoiAuth.SaiMatKhauHienTai, loi.Ma);
        Assert.Equal(422, loi.MaHttp);
    }

    [Fact]
    public async Task DoiMatKhau_KhongDatChinhSach_VaKhongKhop_BaoTungTruong()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaoDichVu().DoiMatKhauAsync(tk.Id, new YeuCauDoiMatKhau(tk.MatKhau, "ngan1", "khac")));
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, loi.Ma);
        var truong = Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(loi.ChiTiet).Select(l => l.Truong).ToList();
        Assert.Equal(new[] { "matKhauMoi", "nhapLaiMatKhauMoi" }, truong);
    }

    [Fact]
    public async Task DoiMatKhau_TrungMatKhauHienTai_422()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaoDichVu().DoiMatKhauAsync(tk.Id, new YeuCauDoiMatKhau(tk.MatKhau, tk.MatKhau, tk.MatKhau)));
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, loi.Ma);
    }

    private DichVuXacThuc TaoDichVu()
    {
        var db = csdl.TaoDbContext();
        _dbDaTao.Add(db);
        return new DichVuXacThuc(db);
    }

    private Task<KetQuaDangNhapNoiBo> DangNhapAsync(string ten, string matKhau) =>
        TaoDichVu().DangNhapAsync(new YeuCauDangNhap(ten, matKhau));
}

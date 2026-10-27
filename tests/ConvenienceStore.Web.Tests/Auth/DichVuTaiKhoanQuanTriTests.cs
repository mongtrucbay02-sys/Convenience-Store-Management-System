using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Shared.Errors;
using Xunit;

namespace ConvenienceStore.Web.Tests.Auth;

[Collection(CsdlXacThucCollection.Ten)]
[Trait("Category", "TaiKhoan")]
public sealed class DichVuTaiKhoanQuanTriTests(CsdlXacThucFixture csdl) : IAsyncLifetime
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
    public async Task Sua_ThanhCong_TangPhienBan_KhongDoiTenVaVaiTro()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var sdt = DuLieuTestXacThuc.SoDienThoaiMoi();

        var sau = await TaiKhoan().SuaAsync(tk.Id, new YeuCauSuaTaiKhoan("Tên Mới", sdt, "moi@vidu.vn", 1));

        Assert.Equal("Tên Mới", sau.HoTen);
        Assert.Equal(sdt, sau.SoDienThoai);
        Assert.Equal("moi@vidu.vn", sau.Email);
        Assert.Equal(2, sau.PhienBan);
        Assert.Equal(tk.TenDangNhap, sau.TenDangNhap);
        Assert.Equal("TN", sau.VaiTro.Ma);
    }

    [Fact]
    public async Task Sua_PhienBanCu_409XungDot()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        await TaiKhoan().SuaAsync(tk.Id, new YeuCauSuaTaiKhoan("Lần 1", null, null, 1));

        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaiKhoan().SuaAsync(tk.Id, new YeuCauSuaTaiKhoan("Lần 2", null, null, 1)));

        Assert.Equal(MaLoiChung.XungDotDuLieu, loi.Ma);
        Assert.Equal("Lần 1", (await TaiKhoan().XemAsync(tk.Id)).HoTen);
    }

    [Fact]
    public async Task Sua_TrungSoDienThoaiTaiKhoanKhac_409_GiuSoCuaChinhMinh_DuocPhep()
    {
        var sdt = DuLieuTestXacThuc.SoDienThoaiMoi();
        var a = await _duLieu.TaoTaiKhoanAsync("TN");
        var b = await _duLieu.TaoTaiKhoanAsync("TN");
        await TaiKhoan().SuaAsync(a.Id, new YeuCauSuaTaiKhoan("A", sdt, null, 1));

        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaiKhoan().SuaAsync(b.Id, new YeuCauSuaTaiKhoan("B", sdt, null, 1)));
        var giuNguyen = await TaiKhoan().SuaAsync(a.Id, new YeuCauSuaTaiKhoan("A2", sdt, null, 2));

        Assert.Equal(MaLoiChung.TrungDuLieu, loi.Ma);
        Assert.Equal("A2", giuNguyen.HoTen);
    }

    [Fact]
    public async Task Sua_ThieuPhienBan_422()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaiKhoan().SuaAsync(tk.Id, new YeuCauSuaTaiKhoan("Tên", null, null, null)));
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, loi.Ma);
    }

    [Fact]
    public async Task Khoa_ThanhCong_LuuLyDo_HuyMoiPhien()
    {
        var admin = await _duLieu.TaoTaiKhoanAsync("AD");
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var token = (await XacThuc().DangNhapAsync(new YeuCauDangNhap(tk.TenDangNhap, tk.MatKhau))).TokenPhien;

        var sau = await TaiKhoan().KhoaAsync(tk.Id, new YeuCauKhoaTaiKhoan("Nghỉ việc từ 15/10/2026"), admin.Id);

        Assert.Equal("DA_KHOA", sau.TrangThai);
        Assert.Equal("Nghỉ việc từ 15/10/2026", sau.LyDoKhoa);
        var phien = await XacThuc().XacThucPhienAsync(token);
        Assert.Null(phien.NguoiDung);
        Assert.True(phien.HetPhien);
    }

    [Fact]
    public async Task Khoa_ChinhMinh_409()
    {
        var admin = await _duLieu.TaoTaiKhoanAsync("AD");
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaiKhoan().KhoaAsync(admin.Id, new YeuCauKhoaTaiKhoan("Thử"), admin.Id));
        Assert.Equal(MaLoiAuth.KhongThaoTacChinhMinh, loi.Ma);
    }

    [Fact]
    public async Task Khoa_AdminHoatDongCuoiCung_409()
    {
        await _duLieu.KhoaMoiAdminDangHoatDongAsync();
        var adminDuyNhat = await _duLieu.TaoTaiKhoanAsync("AD");
        var nguoiKhac = await _duLieu.TaoTaiKhoanAsync("TN");

        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaiKhoan().KhoaAsync(adminDuyNhat.Id, new YeuCauKhoaTaiKhoan("Thử"), nguoiKhac.Id));

        Assert.Equal(MaLoiAuth.MatAdminCuoiCung, loi.Ma);
    }

    [Fact]
    public async Task Khoa_DaKhoaRoi_409DaDuocXuLy_ThieuLyDo_422()
    {
        var admin = await _duLieu.TaoTaiKhoanAsync("AD");
        var tk = await _duLieu.TaoTaiKhoanAsync("NK");
        await TaiKhoan().KhoaAsync(tk.Id, new YeuCauKhoaTaiKhoan("Lần 1"), admin.Id);

        var daKhoa = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaiKhoan().KhoaAsync(tk.Id, new YeuCauKhoaTaiKhoan("Lần 2"), admin.Id));
        var thieuLyDo = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaiKhoan().KhoaAsync(tk.Id, new YeuCauKhoaTaiKhoan("  "), admin.Id));

        Assert.Equal(MaLoiChung.DaDuocXuLy, daKhoa.Ma);
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, thieuLyDo.Ma);
    }

    [Fact]
    public async Task MoKhoa_AdminKhoa_VeHoatDong_XoaLyDo()
    {
        var admin = await _duLieu.TaoTaiKhoanAsync("AD");
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        await TaiKhoan().KhoaAsync(tk.Id, new YeuCauKhoaTaiKhoan("Tạm nghỉ"), admin.Id);

        var sau = await TaiKhoan().MoKhoaAsync(tk.Id);

        Assert.Equal("HOAT_DONG", sau.TrangThai);
        Assert.Null(sau.LyDoKhoa);
        await XacThuc().DangNhapAsync(new YeuCauDangNhap(tk.TenDangNhap, tk.MatKhau));
    }

    [Fact]
    public async Task MoKhoa_KhoaTamDoSaiMatKhau_GoNgay_BoDemVe0()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        await csdl.ChayAsync(
            "UPDATE tai_khoan SET khoa_tam_den = NOW() + INTERVAL 15 MINUTE, so_lan_sai = 2 WHERE id = @id", ("@id", tk.Id));

        var sau = await TaiKhoan().MoKhoaAsync(tk.Id);

        Assert.Null(sau.KhoaTamDen);
        Assert.Equal(0, Convert.ToInt32(await _duLieu.DocCotAsync(tk.Id, "so_lan_sai")));
        await XacThuc().DangNhapAsync(new YeuCauDangNhap(tk.TenDangNhap, tk.MatKhau));
    }

    [Fact]
    public async Task MoKhoa_KhongBiKhoa_409()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaiKhoan().MoKhoaAsync(tk.Id));
        Assert.Equal(MaLoiChung.DaDuocXuLy, loi.Ma);
    }

    [Fact]
    public async Task DatLai_TuSinh_DatCoBatBuoc_HuyPhienCu_DangNhapDuocBangMatKhauTam()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var token = (await XacThuc().DangNhapAsync(new YeuCauDangNhap(tk.TenDangNhap, tk.MatKhau))).TokenPhien;

        var kq = await TaiKhoan().DatLaiMatKhauAsync(tk.Id, null);

        Assert.Equal(tk.TenDangNhap, kq.TenDangNhap);
        Assert.True(QuyTacTaiKhoan.MatKhauHopLe(kq.MatKhauTam));
        Assert.True((await XacThuc().XacThucPhienAsync(token)).HetPhien);
        var dangNhap = await XacThuc().DangNhapAsync(new YeuCauDangNhap(tk.TenDangNhap, kq.MatKhauTam));
        Assert.True(dangNhap.KetQua.BatBuocDoiMatKhau);
        await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            XacThuc().DangNhapAsync(new YeuCauDangNhap(tk.TenDangNhap, tk.MatKhau)));
    }

    [Fact]
    public async Task DatLai_AdminTuNhap_MatKhauYeu422()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("NK");

        var kq = await TaiKhoan().DatLaiMatKhauAsync(tk.Id, new YeuCauDatLaiMatKhau("TamThoi2026"));
        var yeu = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaiKhoan().DatLaiMatKhauAsync(tk.Id, new YeuCauDatLaiMatKhau("yeu")));

        Assert.Equal("TamThoi2026", kq.MatKhauTam);
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, yeu.Ma);
    }

    [Fact]
    public async Task DatLai_TaiKhoanDangBiAdminKhoa_409()
    {
        var admin = await _duLieu.TaoTaiKhoanAsync("AD");
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        await TaiKhoan().KhoaAsync(tk.Id, new YeuCauKhoaTaiKhoan("Tạm nghỉ"), admin.Id);

        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaiKhoan().DatLaiMatKhauAsync(tk.Id, null));

        Assert.Equal(MaLoiAuth.TaiKhoanDangKhoa, loi.Ma);
    }

    [Fact]
    public void MatKhauTamNgauNhien_LuonDatChinhSach_VaKhacNhau()
    {
        var danhSach = Enumerable.Range(0, 200).Select(_ => MatKhauTamNgauNhien.Sinh()).ToList();
        Assert.All(danhSach, mk => Assert.True(QuyTacTaiKhoan.MatKhauHopLe(mk)));
        Assert.True(danhSach.Distinct().Count() > 195);
    }

    private DichVuTaiKhoan TaiKhoan()
    {
        var db = csdl.TaoDbContext();
        _dbDaTao.Add(db);
        return new DichVuTaiKhoan(db);
    }

    private DichVuXacThuc XacThuc()
    {
        var db = csdl.TaoDbContext();
        _dbDaTao.Add(db);
        return new DichVuXacThuc(db);
    }
}

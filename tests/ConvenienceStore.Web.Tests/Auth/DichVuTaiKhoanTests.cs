using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Shared.Errors;
using Xunit;

namespace ConvenienceStore.Web.Tests.Auth;

[Collection(CsdlXacThucCollection.Ten)]
[Trait("Category", "TaiKhoan")]
public sealed class DichVuTaiKhoanTests(CsdlXacThucFixture csdl) : IAsyncLifetime
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
    public async Task Tao_ThanhCong_HoatDong_BatBuocDoiMatKhau_MatKhauDaBam()
    {
        var ten = DuLieuTestXacThuc.TenMoi("thungan");
        var sdt = DuLieuTestXacThuc.SoDienThoaiMoi();

        var tk = await TaoDichVu().TaoAsync(new YeuCauTaoTaiKhoan(ten, " Trần Thị Bình ", sdt, null, "TamThoi2026", "TN"));

        Assert.Equal(ten, tk.TenDangNhap);
        Assert.Equal("Trần Thị Bình", tk.HoTen);
        Assert.Equal(sdt, tk.SoDienThoai);
        Assert.Equal("TN", tk.VaiTro.Ma);
        Assert.Equal("HOAT_DONG", tk.TrangThai);
        Assert.True(tk.BatBuocDoiMatKhau);
        Assert.Equal(1, tk.PhienBan);
        Assert.Equal(TimeSpan.FromHours(7), tk.NgayTao.Offset);
        var bam = (string)(await _duLieu.DocCotAsync((int)tk.Id, "mat_khau_bam"))!;
        Assert.NotEqual("TamThoi2026", bam);
        Assert.True(BamMatKhau.Dung(bam, "TamThoi2026"));
    }

    [Fact]
    public async Task Tao_TrungTenDangNhap_KhongPhanBietHoaThuong_409()
    {
        var co = await _duLieu.TaoTaiKhoanAsync("TN");
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaoDichVu().TaoAsync(
            new YeuCauTaoTaiKhoan(co.TenDangNhap.ToUpperInvariant(), "Ai đó", null, null, "TamThoi2026", "TN")));
        Assert.Equal(MaLoiAuth.TrungTenDangNhap, loi.Ma);
        Assert.Equal(409, loi.MaHttp);
    }

    [Fact]
    public async Task Tao_TrungSoDienThoaiVaEmail_409TrungDuLieu()
    {
        var sdt = DuLieuTestXacThuc.SoDienThoaiMoi();
        var email = DuLieuTestXacThuc.TenMoi("mail") + "@vidu.vn";
        await TaoDichVu().TaoAsync(new YeuCauTaoTaiKhoan(DuLieuTestXacThuc.TenMoi(), "A", sdt, email, "TamThoi2026", "TN"));

        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaoDichVu().TaoAsync(
            new YeuCauTaoTaiKhoan(DuLieuTestXacThuc.TenMoi(), "B", sdt, email.ToUpperInvariant(), "TamThoi2026", "NK")));

        Assert.Equal(MaLoiChung.TrungDuLieu, loi.Ma);
        var truong = Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(loi.ChiTiet).Select(l => l.Truong).ToList();
        Assert.Equal(new[] { "soDienThoai", "email" }, truong);
    }

    [Fact]
    public async Task Tao_DuLieuSai_BaoDuCacTruong()
    {
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaoDichVu().TaoAsync(
            new YeuCauTaoTaiKhoan("thu ngân", "", "12345", "khong-phai-email", "yeu", "QL")));

        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, loi.Ma);
        var truong = Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(loi.ChiTiet).Select(l => l.Truong).ToList();
        Assert.Equal(new[] { "tenDangNhap", "hoTen", "soDienThoai", "email", "matKhauTam", "vaiTro" }, truong);
    }

    [Fact]
    public async Task XemDanhSach_TimKiemLocPhanTrangSapXep()
    {
        var tienTo = "loc" + Guid.NewGuid().ToString("N")[..6];
        foreach (var (hauTo, vaiTro) in new[] { ("a", "TN"), ("b", "TN"), ("c", "NK") })
            await _duLieu.TaoTaiKhoanAsync(vaiTro, tenDangNhap: $"{tienTo}.{hauTo}");

        var tatCa = await TaoDichVu().XemDanhSachAsync(new BoLocTaiKhoan(Keyword: tienTo.ToUpperInvariant()));
        var thuNgan = await TaoDichVu().XemDanhSachAsync(new BoLocTaiKhoan(Keyword: tienTo, VaiTro: "TN"));
        var trang2 = await TaoDichVu().XemDanhSachAsync(new BoLocTaiKhoan(Page: 2, Size: 2, Keyword: tienTo));
        var giamDan = await TaoDichVu().XemDanhSachAsync(new BoLocTaiKhoan(Keyword: tienTo, Sort: "-tenDangNhap"));

        Assert.Equal(3, tatCa.Total);
        Assert.Equal(2, thuNgan.Total);
        Assert.Equal(3, trang2.Total);
        Assert.Single(trang2.Items);
        Assert.Equal($"{tienTo}.c", trang2.Items[0].TenDangNhap);
        Assert.Equal($"{tienTo}.c", giamDan.Items[0].TenDangNhap);
    }

    [Fact]
    public async Task XemDanhSach_TimKhongDauKhongPhanBietHoaThuong_NfrSd02()
    {
        var hauTo = Guid.NewGuid().ToString("N")[..6];
        var tk = await TaoDichVu().TaoAsync(new YeuCauTaoTaiKhoan(
            DuLieuTestXacThuc.TenMoi(), $"Nguyễn Thị Hằng {hauTo}", null, null, "TamThoi2026", "TN"));

        var kq = await TaoDichVu().XemDanhSachAsync(new BoLocTaiKhoan(Keyword: $"nguyen thi HANG {hauTo}"));

        Assert.Equal(tk.Id, Assert.Single(kq.Items).Id);
    }

    [Fact]
    public async Task XemDanhSach_KyTuDacBietTrongTuKhoa_KhongLamHongTruyVan()
    {
        var kq = await TaoDichVu().XemDanhSachAsync(new BoLocTaiKhoan(Keyword: "%_\\'"));
        Assert.Equal(0, kq.Total);
        Assert.Empty(kq.Items);
    }

    [Fact]
    public async Task XemDanhSach_SizeQua100_HaXuong100()
    {
        var kq = await TaoDichVu().XemDanhSachAsync(new BoLocTaiKhoan(Size: 500));
        Assert.Equal(100, kq.Size);
    }

    [Theory]
    [InlineData("matKhau", null, null, "sort")]
    [InlineData(null, "QL", null, "vaiTro")]
    [InlineData(null, null, "KHOA_TAM", "trangThai")]
    public async Task XemDanhSach_ThamSoSai_422(string? sort, string? vaiTro, string? trangThai, string truongLoi)
    {
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaoDichVu().XemDanhSachAsync(
            new BoLocTaiKhoan(VaiTro: vaiTro, TrangThai: trangThai, Sort: sort)));
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, loi.Ma);
        Assert.Equal(truongLoi, Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(loi.ChiTiet)[0].Truong);
    }

    [Fact]
    public async Task Xem_KhongCo_404()
    {
        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaoDichVu().XemAsync(999_999_999));
        Assert.Equal(MaLoiChung.KhongTimThay, loi.Ma);
        Assert.Equal(404, loi.MaHttp);
    }

    [Fact]
    public async Task GanVaiTro_DoiVaiTro_TangPhienBan()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");

        var sau = await TaoDichVu().GanVaiTroAsync(tk.Id, new YeuCauGanVaiTro("NK"));

        Assert.Equal("NK", sau.VaiTro.Ma);
        Assert.Equal(2, sau.PhienBan);
    }

    [Fact]
    public async Task GanVaiTro_GiongVaiTroHienTai_KhongDoiGi()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("NK");
        var sau = await TaoDichVu().GanVaiTroAsync(tk.Id, new YeuCauGanVaiTro("NK"));
        Assert.Equal(1, sau.PhienBan);
    }

    [Fact]
    public async Task GanVaiTro_HaAdminCuoiCung_409()
    {
        await _duLieu.KhoaMoiAdminDangHoatDongAsync();
        var admin = await _duLieu.TaoTaiKhoanAsync("AD");

        var loi = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaoDichVu().GanVaiTroAsync(admin.Id, new YeuCauGanVaiTro("TN")));

        Assert.Equal(MaLoiAuth.MatAdminCuoiCung, loi.Ma);
        Assert.Equal("AD", (await TaoDichVu().XemAsync(admin.Id)).VaiTro.Ma);
    }

    [Fact]
    public async Task GanVaiTro_ConAdminKhac_HaDuoc()
    {
        await _duLieu.KhoaMoiAdminDangHoatDongAsync();
        var admin1 = await _duLieu.TaoTaiKhoanAsync("AD");
        await _duLieu.TaoTaiKhoanAsync("AD");

        var sau = await TaoDichVu().GanVaiTroAsync(admin1.Id, new YeuCauGanVaiTro("TN"));

        Assert.Equal("TN", sau.VaiTro.Ma);
    }

    [Fact]
    public async Task GanVaiTro_VaiTroSai_422_TaiKhoanKhongCo_404()
    {
        var tk = await _duLieu.TaoTaiKhoanAsync("TN");
        var sai = await Assert.ThrowsAsync<LoiNghiepVu>(() => TaoDichVu().GanVaiTroAsync(tk.Id, new YeuCauGanVaiTro("QL")));
        var khongCo = await Assert.ThrowsAsync<LoiNghiepVu>(() =>
            TaoDichVu().GanVaiTroAsync(999_999_999, new YeuCauGanVaiTro("TN")));
        Assert.Equal(MaLoiChung.DuLieuKhongHopLe, sai.Ma);
        Assert.Equal(MaLoiChung.KhongTimThay, khongCo.Ma);
    }

    [Fact]
    public async Task XemDanhSachVaiTro_Du3VaiTro_KemQuyen()
    {
        var kq = await TaoDichVu().XemDanhSachVaiTroAsync();

        Assert.Equal(3, kq.Total);
        Assert.Equal(new[] { "AD", "TN", "NK" }, kq.Items.Select(v => v.Ma).ToArray());
        Assert.Contains(MaQuyen.BanHang, kq.Items[1].Quyen);
        Assert.Equal("Thu ngân", kq.Items[1].Ten);
    }

    private DichVuTaiKhoan TaoDichVu()
    {
        var db = csdl.TaoDbContext();
        _dbDaTao.Add(db);
        return new DichVuTaiKhoan(db);
    }
}

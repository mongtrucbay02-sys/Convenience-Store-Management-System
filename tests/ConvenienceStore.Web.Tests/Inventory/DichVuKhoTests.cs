using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Inventory;

[Collection(CsdlKhoCollection.Ten)]
[Trait("Category", "DichVuKho")]
public sealed class DichVuKhoTests(CsdlKhoFixture csdl)
{
    private readonly DuLieuTestKho _duLieu = new(csdl);
    private readonly BoTroGoiDichVuKho _goi = new(csdl);

    [Fact]
    public async Task Ut01_Nhap12GiaMuoiLamNghinVaoTon0()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT01", 0, 0m);

        var kq = await _goi.GhiVaCommitAsync(LoaiBienDong.NHAP_HANG, new DongBienDong(sp, SoLuong: 12, GiaVon: 15000m));

        var ton = await _duLieu.DocTonKhoAsync(sp);
        Assert.Equal(12, ton!.SoLuongTon);
        Assert.Equal(15000m, ton.GiaVonBinhQuan);
        Assert.Equal(1, await _duLieu.DemTheKhoAsync(sp));
        var dong = Assert.Single(kq.Dong);
        Assert.Equal(12, dong.SoLuongThayDoi);
        Assert.Equal(15000m, dong.GiaVonApDung);
        Assert.NotNull(dong.TheKhoId);
    }

    [Fact]
    public async Task Ut02_Ton10GiaVon8000_Nhap20Gia9500_GiaVon9000()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT02", 10, 8000m);

        var kq = await _goi.GhiVaCommitAsync(LoaiBienDong.NHAP_HANG, new DongBienDong(sp, SoLuong: 20, GiaVon: 9500m));

        var ton = await _duLieu.DocTonKhoAsync(sp);
        Assert.Equal(30, ton!.SoLuongTon);
        Assert.Equal(9000m, ton.GiaVonBinhQuan);
        Assert.Equal(8000m, kq.Dong[0].GiaVonTruoc);
        Assert.Equal(9000m, kq.Dong[0].GiaVonSau);
        Assert.Equal(9500m, kq.Dong[0].GiaVonApDung);
    }

    [Fact]
    public async Task Ut03_Ban3KhiTon5_GiaVonGiuNguyen()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT03", 5, 8000m);

        var kq = await _goi.GhiVaCommitAsync(LoaiBienDong.BAN_HANG, new DongBienDong(sp, SoLuong: 3));

        var ton = await _duLieu.DocTonKhoAsync(sp);
        Assert.Equal(2, ton!.SoLuongTon);
        Assert.Equal(8000m, ton.GiaVonBinhQuan);
        Assert.Equal(-3, kq.Dong[0].SoLuongThayDoi);
        Assert.Equal(8000m, kq.Dong[0].GiaVonApDung);
        Assert.Equal(1, await _duLieu.DemTheKhoAsync(sp, "BAN_HANG"));
    }

    [Fact]
    public async Task Ut04_Ban6KhiTon5_TonKhongDu_KhongGhiTheKho()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT04", 5, 8000m);

        var loi = await _goi.GhiVaBatLoiAsync(LoaiBienDong.BAN_HANG, new DongBienDong(sp, SoLuong: 6));

        Assert.Equal(MaLoiKho.TonKhongDu, loi.Ma);
        Assert.Equal(409, loi.MaHttp);
        Assert.Contains("chỉ còn 5 sản phẩm", loi.Message);
        var thieu = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ThieuTon>>(loi.ChiTiet));
        Assert.Equal(new ThieuTon(sp, 5, 6), thieu);
        Assert.Equal(0, await _duLieu.DemTheKhoAsync(sp, "BAN_HANG"));
        Assert.Equal(5, (await _duLieu.DocTonKhoAsync(sp))!.SoLuongTon);
    }

    [Fact]
    public async Task Ut05_HoaDon3Dong_MotDongThieu_KhongGhiDongNao()
    {
        var sp1 = await _goi.TaoSanPhamCoTonAsync("UT05", 10, 5000m);
        var sp2 = await _goi.TaoSanPhamCoTonAsync("UT05", 1, 5000m);
        var sp3 = await _goi.TaoSanPhamCoTonAsync("UT05", 10, 5000m);

        var loi = await _goi.GhiVaBatLoiAsync(
            LoaiBienDong.BAN_HANG,
            new DongBienDong(sp1, SoLuong: 2),
            new DongBienDong(sp2, SoLuong: 3),
            new DongBienDong(sp3, SoLuong: 2));

        Assert.Equal(MaLoiKho.TonKhongDu, loi.Ma);
        var thieu = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ThieuTon>>(loi.ChiTiet));
        Assert.Equal(sp2, thieu.SanPhamId);
        foreach (var sp in new[] { sp1, sp2, sp3 })
            Assert.Equal(0, await _duLieu.DemTheKhoAsync(sp, "BAN_HANG"));
        Assert.Equal(10, (await _duLieu.DocTonKhoAsync(sp1))!.SoLuongTon);
    }

    [Fact]
    public async Task Ut06_HaiQuayCungBanSanPhamConTon1_ChiMotBenThanhCong()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT06", 1, 7000m);

        var ketQua = await Task.WhenAll(BanMotTrenKetNoiRiengAsync(sp), BanMotTrenKetNoiRiengAsync(sp));

        Assert.Equal(1, ketQua.Count(k => k == "THANH_CONG"));
        Assert.Equal(1, ketQua.Count(k => k == MaLoiKho.TonKhongDu));
        Assert.Equal(0, (await _duLieu.DocTonKhoAsync(sp))!.SoLuongTon);
        Assert.Equal(1, await _duLieu.DemTheKhoAsync(sp, "BAN_HANG"));
    }

    [Fact]
    public async Task Ut07_KiemKeSoThucTeBangTon_KhongGhiTheKho()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT07", 10, 6000m);

        var kq = await _goi.GhiVaCommitAsync(LoaiBienDong.KIEM_KE, new DongBienDong(sp, SoLuongThucTe: 10));

        var dong = Assert.Single(kq.Dong);
        Assert.Equal(0, dong.SoLuongThayDoi);
        Assert.Null(dong.TheKhoId);
        Assert.Equal(0, await _duLieu.DemTheKhoAsync(sp, "KIEM_KE"));
    }

    [Fact]
    public async Task Ut08_KiemKeLapKhiTon10Dem8_BanThem1_ChenhLechLucDuyetLaTru1()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT08", 10, 6000m);
        const int soDemDuocLucLapPhieu = 8;
        await _goi.GhiVaCommitAsync(LoaiBienDong.BAN_HANG, new DongBienDong(sp, SoLuong: 1));

        var kq = await _goi.GhiVaCommitAsync(
            LoaiBienDong.KIEM_KE, new DongBienDong(sp, SoLuongThucTe: soDemDuocLucLapPhieu));

        Assert.Equal(-1, kq.Dong[0].SoLuongThayDoi);
        Assert.Equal(9, kq.Dong[0].TonTruoc);
        Assert.Equal(8, (await _duLieu.DocTonKhoAsync(sp))!.SoLuongTon);
        Assert.Equal(1, await _duLieu.DemTheKhoAsync(sp, "KIEM_KE"));
    }

    [Fact]
    public async Task Ut09_XuatHuy2KhiTon5GiaVon9000()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT09", 5, 9000m);

        var kq = await _goi.GhiVaCommitAsync(LoaiBienDong.XUAT_HUY, new DongBienDong(sp, SoLuong: 2));

        var ton = await _duLieu.DocTonKhoAsync(sp);
        Assert.Equal(3, ton!.SoLuongTon);
        Assert.Equal(9000m, ton.GiaVonBinhQuan);
        Assert.Equal(9000m, kq.Dong[0].GiaVonApDung);
    }

    [Fact]
    public async Task Ut11_YeuCauSai_DuLieuKhongHopLe()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT11", 5, 9000m);
        var truongHop = new (LoaiBienDong Loai, DongBienDong[] Dong, string TruongLoi)[]
        {
            (LoaiBienDong.BAN_HANG, [], "dong"),
            (LoaiBienDong.BAN_HANG, [new DongBienDong(sp, SoLuong: 1), new DongBienDong(sp, SoLuong: 2)], "dong[1].sanPhamId"),
            (LoaiBienDong.BAN_HANG, [new DongBienDong(sp, SoLuong: 0)], "dong[0].soLuong"),
            (LoaiBienDong.NHAP_HANG, [new DongBienDong(sp, SoLuong: 3)], "dong[0].giaVon"),
            (LoaiBienDong.TRA_HANG, [new DongBienDong(sp, SoLuong: 1, GiaVon: 8000m)], "loai"),
        };

        foreach (var (loai, dong, truongLoi) in truongHop)
        {
            await using var db = csdl.TaoDbContext();
            await using var giaoTac = await db.Database.BeginTransactionAsync();
            var loi = await Assert.ThrowsAsync<LoiDichVuKho>(() =>
                BoTroGoiDichVuKho.TaoDichVu(db).GhiNhanBienDongAsync(_goi.TaoYeuCau(loai, dong)));
            Assert.Equal(MaLoiKho.DuLieuKhongHopLe, loi.Ma);
            Assert.Equal(422, loi.MaHttp);
            Assert.Contains(Assert.IsAssignableFrom<IReadOnlyList<LoiTruong>>(loi.ChiTiet), l => l.Truong == truongLoi);
        }

        Assert.Equal(1, await _duLieu.DemTheKhoAsync(sp));
    }

    [Fact]
    public async Task Ut12_Nguong5_BanLamTonTu6Xuong5_DemDuoiNguongTang1()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT12", 6, 5000m, nguong: 5);
        int truoc;
        await using (var db = csdl.TaoDbContext())
            truoc = await BoTroGoiDichVuKho.TaoDichVu(db).DemSanPhamDuoiNguongAsync();

        var kq = await _goi.GhiVaCommitAsync(LoaiBienDong.BAN_HANG, new DongBienDong(sp, SoLuong: 1));

        Assert.Equal(truoc + 1, kq.SoSanPhamDuoiNguong);
    }

    [Fact]
    public async Task Ut13_BenGoiRollback_TonVaTheKhoTroVeNhuTruoc()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("UT13", 5, 5000m);
        await using (var db = csdl.TaoDbContext())
        {
            await using var giaoTac = await db.Database.BeginTransactionAsync();
            var kq = await BoTroGoiDichVuKho.TaoDichVu(db).GhiNhanBienDongAsync(
                _goi.TaoYeuCau(LoaiBienDong.BAN_HANG, [new DongBienDong(sp, SoLuong: 2)]));
            Assert.Equal(3, kq.Dong[0].TonSau);
            await giaoTac.RollbackAsync();
        }

        Assert.Equal(5, (await _duLieu.DocTonKhoAsync(sp))!.SoLuongTon);
        Assert.Equal(0, await _duLieu.DemTheKhoAsync(sp, "BAN_HANG"));
    }

    [Fact]
    public async Task KhongCoGiaoTac_BaoLoiLapTrinh()
    {
        var sp = await _goi.TaoSanPhamCoTonAsync("KGT", 5, 5000m);
        await using var db = csdl.TaoDbContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BoTroGoiDichVuKho.TaoDichVu(db).GhiNhanBienDongAsync(
                _goi.TaoYeuCau(LoaiBienDong.BAN_HANG, [new DongBienDong(sp, SoLuong: 1)])));
    }

    [Fact]
    public async Task SanPhamChuaCoTonKho_SanPhamKhongTonTai()
    {
        var sp = await _duLieu.TaoSanPhamAsync("CHUATON");
        var loi = await _goi.GhiVaBatLoiAsync(LoaiBienDong.BAN_HANG, new DongBienDong(sp, SoLuong: 1));
        Assert.Equal(MaLoiKho.SanPhamKhongTonTai, loi.Ma);
        Assert.Equal(422, loi.MaHttp);
    }

    [Fact]
    public async Task KhoiTaoTonKho_MacDinhNguong5_KhongTaoTrung()
    {
        var sp = await _duLieu.TaoSanPhamAsync("KTTK");
        await using var db = csdl.TaoDbContext();
        var dichVu = BoTroGoiDichVuKho.TaoDichVu(db);

        await dichVu.KhoiTaoTonKhoAsync(sp);
        var loi = await Assert.ThrowsAsync<LoiDichVuKho>(() => dichVu.KhoiTaoTonKhoAsync(sp));
        var khongCo = await Assert.ThrowsAsync<LoiDichVuKho>(() => dichVu.KhoiTaoTonKhoAsync(999_999_999));

        Assert.Equal(new TonKhoDoc(0, 0m, 5), await _duLieu.DocTonKhoAsync(sp));
        Assert.Equal(MaLoiKho.DuLieuKhongHopLe, loi.Ma);
        Assert.Equal(MaLoiKho.SanPhamKhongTonTai, khongCo.Ma);
    }

    [Fact]
    public async Task LayTonKho_KhongKhoa_TraDungThuTuVaCoDuoiNguong()
    {
        var sp1 = await _goi.TaoSanPhamCoTonAsync("LTK", 3, 4000m, nguong: 5);
        var sp2 = await _goi.TaoSanPhamCoTonAsync("LTK", 9, 4000m, nguong: 5);
        await using var db = csdl.TaoDbContext();

        var ds = await BoTroGoiDichVuKho.TaoDichVu(db).LayTonKhoAsync([sp2, sp1, 999_999_999]);

        Assert.Equal(new[] { sp1, sp2 }, ds.Select(t => t.SanPhamId).ToArray());
        Assert.True(ds[0].DuoiNguong);
        Assert.False(ds[1].DuoiNguong);
    }

    private async Task<string> BanMotTrenKetNoiRiengAsync(int sanPhamId)
    {
        await using var db = csdl.TaoDbContext();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 10");
        await using var giaoTac = await db.Database.BeginTransactionAsync();
        try
        {
            await BoTroGoiDichVuKho.TaoDichVu(db).GhiNhanBienDongAsync(
                _goi.TaoYeuCau(LoaiBienDong.BAN_HANG, [new DongBienDong(sanPhamId, SoLuong: 1)]));
            await Task.Delay(200);
            await giaoTac.CommitAsync();
            return "THANH_CONG";
        }
        catch (LoiDichVuKho loi)
        {
            await giaoTac.RollbackAsync();
            return loi.Ma;
        }
    }
}

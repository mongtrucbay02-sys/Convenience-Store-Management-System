using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Xunit;

namespace ConvenienceStore.Web.Tests.Inventory;

/// <summary>
/// T4-17: kiểm tra hạ tầng test dịch vụ kho trước khi viết UT-01 → UT-13:
/// CSDL tạm dựng đúng bằng migration, múi giờ, CHECK, trigger, khóa dòng giữa hai kết nối, ROLLBACK.
/// </summary>
[Collection(CsdlKhoCollection.Ten)]
[Trait("Category", "DichVuKho")]
public sealed class HaTangTestKhoTests(CsdlKhoFixture csdl)
{
    private readonly DuLieuTestKho _duLieu = new(csdl);

    [Fact]
    public async Task Migration_TaoDu28Bang_3View_10Trigger()
    {
        var soBang = await DemAsync(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() " +
            "AND table_type = 'BASE TABLE' AND table_name <> '__EFMigrationsHistory'");
        var soView = await DemAsync(
            "SELECT COUNT(*) FROM information_schema.views WHERE table_schema = DATABASE()");
        var soTrigger = await DemAsync(
            "SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = DATABASE()");

        Assert.Equal(28, soBang);
        Assert.Equal(3, soView);
        Assert.Equal(10, soTrigger);
    }

    [Fact]
    public async Task Migration_DaGhiLichSuInitialCreate()
    {
        await using var db = csdl.TaoDbContext();
        var daChay = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(daChay, m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task KetNoiQuaDbContext_VaKetNoiTrucTiep_DeuDungGioVietNam()
    {
        await using (var db = csdl.TaoDbContext())
        {
            await db.Database.OpenConnectionAsync();
            await using var lenh = db.Database.GetDbConnection().CreateCommand();
            lenh.CommandText = "SELECT @@session.time_zone";
            Assert.Equal("+07:00", await lenh.ExecuteScalarAsync());
        }

        Assert.Equal("+07:00", await csdl.DocMotGiaTriAsync("SELECT @@session.time_zone"));
    }

    [Fact]
    public async Task DuLieuNen_Co3VaiTro_3TaiKhoan_1DanhMuc()
    {
        Assert.Equal(3, await DemAsync("SELECT COUNT(*) FROM vai_tro"));
        Assert.Equal("AD", await csdl.DocMotGiaTriAsync(
            "SELECT v.ma FROM tai_khoan t JOIN vai_tro v ON v.id = t.vai_tro_id WHERE t.id = @id",
            ("@id", csdl.TaiKhoanAdminId)));
        Assert.Equal("NK", await csdl.DocMotGiaTriAsync(
            "SELECT v.ma FROM tai_khoan t JOIN vai_tro v ON v.id = t.vai_tro_id WHERE t.id = @id",
            ("@id", csdl.TaiKhoanKhoId)));
        Assert.True(csdl.DanhMucId > 0);
    }

    [Fact]
    public async Task TaoSanPham_MoiLanMotMaRieng_ChuaCoTonKho()
    {
        var sp1 = await _duLieu.TaoSanPhamAsync("UT00");
        var sp2 = await _duLieu.TaoSanPhamAsync("UT00");

        Assert.NotEqual(sp1, sp2);
        Assert.Null(await _duLieu.DocTonKhoAsync(sp1));
        Assert.Equal(0, await _duLieu.DemTheKhoAsync(sp1));
        Assert.NotEqual(DuLieuTestKho.MaChungTuGia(), DuLieuTestKho.MaChungTuGia());
    }

    [Fact]
    public async Task Check_ChanTonAm()
    {
        var sp = await _duLieu.TaoSanPhamAsync("HTCHECK");
        await csdl.ChayAsync("INSERT INTO ton_kho (san_pham_id) VALUES (@sp)", ("@sp", sp));

        var loi = await Assert.ThrowsAsync<MySqlException>(() => csdl.ChayAsync(
            "UPDATE ton_kho SET so_luong_ton = -1 WHERE san_pham_id = @sp", ("@sp", sp)));
        Assert.Contains("ck_ton_khong_am", loi.Message);
    }

    [Fact]
    public async Task Trigger_ChanSuaVaXoaTheKho()
    {
        var sp = await _duLieu.TaoSanPhamAsync("HTTRIG");
        await csdl.ChayAsync(
            """
            INSERT INTO the_kho (san_pham_id, loai_bien_dong, ma_chung_tu, chung_tu_id, so_luong_thay_doi,
                                 ton_sau, gia_von_ap_dung, gia_von_sau, nguoi_thuc_hien_id)
            VALUES (@sp, 'NHAP_HANG', @ma, @ct, 5, 5, 10000, 10000, @nguoi)
            """,
            ("@sp", sp), ("@ma", DuLieuTestKho.MaChungTuGia()), ("@ct", DuLieuTestKho.IdChungTuGia()),
            ("@nguoi", csdl.TaiKhoanKhoId));

        await Assert.ThrowsAsync<MySqlException>(() => csdl.ChayAsync(
            "UPDATE the_kho SET ton_sau = 6 WHERE san_pham_id = @sp", ("@sp", sp)));
        await Assert.ThrowsAsync<MySqlException>(() => csdl.ChayAsync(
            "DELETE FROM the_kho WHERE san_pham_id = @sp", ("@sp", sp)));
        Assert.Equal(1, await _duLieu.DemTheKhoAsync(sp, "NHAP_HANG"));
    }

    [Fact]
    public async Task HaiKetNoi_ForUpdate_KetNoiSauPhaiCho()
    {
        // Nền cho UT-06: hai quầy dùng hai kết nối riêng; dòng tồn đã bị khóa thì bên kia phải chờ.
        var sp = await _duLieu.TaoSanPhamAsync("HTKHOA");
        await csdl.ChayAsync("INSERT INTO ton_kho (san_pham_id) VALUES (@sp)", ("@sp", sp));
        const string khoaDong = "SELECT so_luong_ton FROM ton_kho WHERE san_pham_id = @sp FOR UPDATE";

        await using var quay1 = await csdl.MoKetNoiAsync();
        await using var quay2 = await csdl.MoKetNoiAsync();
        await using (var lenh = CsdlKhoFixture.TaoLenh(quay2, "SET SESSION innodb_lock_wait_timeout = 1"))
            await lenh.ExecuteNonQueryAsync();

        await using var giaoTac1 = await quay1.BeginTransactionAsync();
        await using (var lenh = CsdlKhoFixture.TaoLenh(quay1, khoaDong, ("@sp", sp)))
        {
            lenh.Transaction = giaoTac1;
            await lenh.ExecuteScalarAsync();
        }

        await using var giaoTac2 = await quay2.BeginTransactionAsync();
        await using (var lenh = CsdlKhoFixture.TaoLenh(quay2, khoaDong, ("@sp", sp)))
        {
            lenh.Transaction = giaoTac2;
            var loi = await Assert.ThrowsAsync<MySqlException>(() => lenh.ExecuteScalarAsync());
            Assert.Equal(1205, loi.Number); // Lock wait timeout: dịch vụ kho trả LOI_GHI_DU_LIEU
        }

        await giaoTac2.RollbackAsync();
        await giaoTac1.RollbackAsync();
    }

    [Fact]
    public async Task Rollback_KhongDeLaiDuLieu()
    {
        // Nền cho UT-13: bên gọi ROLLBACK thì tồn và thẻ kho trở về như trước.
        var sp = await _duLieu.TaoSanPhamAsync("HTRB");
        await using (var db = csdl.TaoDbContext())
        {
            await using var giaoTac = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO ton_kho (san_pham_id, so_luong_ton) VALUES ({sp}, 7)");
            await giaoTac.RollbackAsync();
        }

        Assert.Null(await _duLieu.DocTonKhoAsync(sp));
    }

    private async Task<int> DemAsync(string sql) => Convert.ToInt32(await csdl.DocMotGiaTriAsync(sql));
}

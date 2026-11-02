using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ConvenienceStore.Web.Tests.Returns;

[Collection(CsdlKhoCollection.Ten)]
public sealed class InvoiceLookupMySqlTests(CsdlKhoFixture fixture)
{
    private sealed record Invoice(long Id, string Code, int ProductId);
    private sealed class Scenario(CsdlKhoFixture fixture, CuaHangDbContext db, IDbContextTransaction transaction) : IAsyncDisposable
    {
        public CuaHangDbContext Db => db;
        public MySqlInvoiceLookupStore Store => new(db);
        public string Phone { get; } = "0" + Random.Shared.Next(100000000, 999999999);
        public int CustomerId, OtherCashierId;
        public static async Task<Scenario> Create(CsdlKhoFixture fixture)
        {
            var db = fixture.TaoDbContext();
            var scenario = new Scenario(fixture, db, await db.Database.BeginTransactionAsync());
            scenario.CustomerId = checked((int)await scenario.Insert("""
                INSERT INTO khach_hang (ho_ten, so_dien_thoai, nguoi_tao_id) VALUES ('Khách DT01', @phone, @tn);
                SELECT LAST_INSERT_ID();
                """, ("@phone", scenario.Phone), ("@tn", fixture.TaiKhoanThuNganId)));
            scenario.OtherCashierId = checked((int)await scenario.Insert("""
                INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id, bat_buoc_doi_mk)
                VALUES (@name, 'khong-dang-nhap', 'Thu ngân khác', (SELECT id FROM vai_tro WHERE ma = 'TN'), FALSE);
                SELECT LAST_INSERT_ID();
                """, ("@name", "dt" + Guid.NewGuid().ToString("N"))));
            return scenario;
        }
        public async Task<long> Insert(string sql, params (string Ten, object? GiaTri)[] args) =>
            Convert.ToInt64(await db.DocMotGiaTriAsync(sql, default, args));
        public async Task<Invoice> AddInvoice(DateTime? purchased = null, bool cancelled = false, bool forbidden = false, int? cashier = null)
        {
            var category = forbidden ? checked((int)await Insert("""
                INSERT INTO danh_muc (ten, khong_doi_tra) VALUES (@name, TRUE); SELECT LAST_INSERT_ID();
                """, ("@name", "DT-" + Guid.NewGuid().ToString("N")))) : fixture.DanhMucId;
            var product = checked((int)await Insert("""
                INSERT INTO san_pham (ma, ten_san_pham, don_vi_tinh, danh_muc_id, nguoi_tao_id)
                VALUES (@ma, 'Tên hiện tại', 'Hộp', @dm, @tn); SELECT LAST_INSERT_ID();
                """, ("@ma", "SP" + Guid.NewGuid().ToString("N")[..12]), ("@dm", category), ("@tn", fixture.TaiKhoanThuNganId)));
            var tn = cashier ?? OtherCashierId;
            var when = purchased ?? InvoiceLookupServiceTests.Now.DateTime.AddDays(-1);
            var cart = await Insert("""
                INSERT INTO gio_hang (thu_ngan_id, khach_hang_id, trang_thai, hoan_tat_luc)
                VALUES (@tn, @kh, 'DA_HOAN_TAT', @time); SELECT LAST_INSERT_ID();
                """, ("@tn", tn), ("@kh", CustomerId), ("@time", when));
            var code = "HD" + Guid.NewGuid().ToString("N")[..12];
            var id = await Insert("""
                INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, khach_hang_id, thanh_toan_luc,
                    tong_tien_hang, tong_giam, tong_thanh_toan, tien_khach_dua, tien_thua, so_tien_mot_diem, diem_cong)
                VALUES (@ma, @gio, @tn, @kh, @time, 100000, 10000, 90000, 90000, 0, 10000, 9);
                SELECT LAST_INSERT_ID();
                """, ("@ma", code), ("@gio", cart), ("@tn", tn), ("@kh", CustomerId), ("@time", when));
            var promotion = await Insert("""
                INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, nguoi_tao_id)
                VALUES (@ma, 'KM test', 'SO_TIEN', 1000, '2026-01-01', '2027-01-01', @ad); SELECT LAST_INSERT_ID();
                """, ("@ma", "KM" + Guid.NewGuid().ToString("N")[..10]), ("@ad", fixture.TaiKhoanAdminId));
            await db.ChayAsync("""
                INSERT INTO ct_hoa_don (hoa_don_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, don_gia, giam_don_vi, khuyen_mai_id, gia_von)
                VALUES (@hd, @sp, 'Tên snapshot', 'Hộp', 10, 10000, 1000, @km, 8000)
                """, default, ("@hd", id), ("@sp", product), ("@km", promotion));
            if (cancelled) await db.ChayAsync("""
                UPDATE hoa_don SET trang_thai = 'DA_HUY', nguoi_huy_id = @ad, huy_luc = @now, ly_do_huy = 'Test DT01'
                WHERE id = @id
                """, default, ("@ad", fixture.TaiKhoanAdminId), ("@now", InvoiceLookupServiceTests.Now.DateTime), ("@id", id));
            return new Invoice(id, code, product);
        }
        public async Task AddSlip(Invoice invoice, string status, int quantity)
        {
            var approved = status != "CHO_DUYET";
            var value = quantity * 9000L;
            var id = await Insert("""
                INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, chinh_sach_id, trang_thai,
                    can_duyet_qua_han, gia_tri_hang_tra, tien_hoan, thu_ngan_id, nguoi_duyet_id, duyet_luc, hoan_tat_luc)
                VALUES (@ma, @loai, @hd, (SELECT MIN(id) FROM chinh_sach_doi_tra), @status,
                    TRUE, @value, @refund, @tn, @ad, @approvedAt, @completedAt); SELECT LAST_INSERT_ID();
                """, ("@ma", "DT" + Guid.NewGuid().ToString("N")[..12]), ("@loai", status == "DA_DUYET" ? "DOI_HANG" : "TRA_HANG"),
                ("@hd", invoice.Id), ("@status", status), ("@value", value), ("@refund", status == "DA_DUYET" ? 0L : value),
                ("@tn", fixture.TaiKhoanThuNganId), ("@ad", approved ? fixture.TaiKhoanAdminId : null),
                ("@approvedAt", approved ? InvoiceLookupServiceTests.Now.DateTime : null),
                ("@completedAt", status == "HOAN_TAT" ? InvoiceLookupServiceTests.Now.DateTime : null));
            await db.ChayAsync("""
                INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong, don_gia_thuc_tra, ly_do, cach_xu_ly, gia_von)
                VALUES (@p, @sp, @hd, @qty, 9000, 'KHONG_DUNG_NHU_CAU', 'NHAP_LAI', 8000)
                """, default, ("@p", id), ("@sp", invoice.ProductId), ("@hd", invoice.Id), ("@qty", quantity));
        }
        public InvoiceLookupService Service => new(Store, new InvoiceLookupServiceTests.FixedTime(InvoiceLookupServiceTests.Now));
        public async ValueTask DisposeAsync()
        { await transaction.RollbackAsync(); await transaction.DisposeAsync(); await db.DisposeAsync(); }
    }

    [Fact]
    public async Task ExactCodeFindsOtherCashiersInvoiceAndPhoneSearchRestrictsDates()
    {
        await using var s = await Scenario.Create(fixture);
        var recent = await s.AddInvoice();
        await s.AddInvoice(cashier: fixture.TaiKhoanThuNganId);
        var old = await s.AddInvoice(new DateTime(2026, 11, 9, 23, 59, 59));
        await s.AddInvoice(InvoiceLookupServiceTests.Now.DateTime.AddDays(1));
        var phoneResult = await s.Service.LookupAsync(new() { SoDienThoai = s.Phone }, InvoiceLookupServiceTests.Cashier());
        Assert.Equal(2, phoneResult.Total);
        Assert.Contains(phoneResult.Items, i => i.Id == recent.Id);
        Assert.DoesNotContain(phoneResult.Items, i => i.Id == old.Id);
        var exact = await s.Service.LookupAsync(new() { Ma = old.Code }, InvoiceLookupServiceTests.Cashier());
        Assert.True(Assert.Single(exact.Items).CanDuyetNgoaiLe);
    }

    [Fact]
    public async Task PendingApprovedAndCompletedReserveQuantityButRejectedDoesNot()
    {
        await using var s = await Scenario.Create(fixture);
        var invoice = await s.AddInvoice();
        await s.AddSlip(invoice, "CHO_DUYET", 1); await s.AddSlip(invoice, "DA_DUYET", 2);
        await s.AddSlip(invoice, "HOAN_TAT", 3); await s.AddSlip(invoice, "BI_TU_CHOI", 4);
        var result = await s.Service.LookupAsync(new() { Ma = invoice.Code }, InvoiceLookupServiceTests.Cashier());
        var line = Assert.Single(Assert.Single(result.Items).ChiTiet);
        Assert.Equal(6, line.SoLuongDaTraHoacDangXuLy); Assert.Equal(4, line.SoLuongConDuocTra);
        Assert.Equal("Tên snapshot", line.TenSanPham); Assert.Equal(9000, line.GiaThucTraMoiDonVi);
    }

    [Fact]
    public async Task EffectivePolicyWinsAndFuturePolicyDoesNotApply()
    {
        await using var s = await Scenario.Create(fixture);
        var invoice = await s.AddInvoice(new DateTime(2026, 11, 10, 21, 0, 0));
        await s.Db.ChayAsync("""
            INSERT INTO chinh_sach_doi_tra (so_ngay_doi_tra, hieu_luc_tu) VALUES (3, '2026-11-16'), (30, '2026-11-18')
            """, default);
        var result = await s.Service.LookupAsync(new() { Ma = invoice.Code }, InvoiceLookupServiceTests.Cashier());
        Assert.Equal(3, Assert.Single(result.Items).SoNgayDoiTra);
        Assert.True(result.Items[0].CanDuyetNgoaiLe);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CancelledAndCategoryRestrictedInvoicesAreMarked(bool cancelled, bool forbidden)
    {
        await using var s = await Scenario.Create(fixture);
        var invoice = await s.AddInvoice(cancelled: cancelled, forbidden: forbidden);
        var result = await s.Service.LookupAsync(new() { Ma = invoice.Code }, InvoiceLookupServiceTests.Cashier());
        var found = Assert.Single(result.Items);
        Assert.False(found.CoTheDoiTra); Assert.Equal(cancelled, found.DaHuy);
        Assert.Equal(!cancelled && forbidden, found.CanDuyetNgoaiLe);
    }

    [Fact]
    public async Task ExactSearchIsParameterizedAndPagedPhoneResultsAreStable()
    {
        await using var s = await Scenario.Create(fixture);
        var older = await s.AddInvoice(InvoiceLookupServiceTests.Now.DateTime.AddDays(-2));
        await s.AddInvoice();
        foreach (var query in new[] { "%", "' OR 1=1 --", older.Code[..^1] })
            Assert.Empty((await s.Service.LookupAsync(new() { Ma = query }, InvoiceLookupServiceTests.Cashier())).Items);
        var page = await s.Service.LookupAsync(new() { SoDienThoai = s.Phone, Page = 2, Size = 1 }, InvoiceLookupServiceTests.Cashier());
        Assert.Equal(2, page.Total); Assert.Equal(older.Id, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task LookupDoesNotChangeStockPointsOrReturnDocuments()
    {
        await using var s = await Scenario.Create(fixture);
        var invoice = await s.AddInvoice();
        async Task<string> Fingerprint() => string.Join("|",
            await s.Db.DocMotGiaTriAsync("SELECT COALESCE(SUM(so_luong_ton),0) FROM ton_kho", default),
            await s.Db.DocMotGiaTriAsync("SELECT COALESCE(SUM(diem_cong),0) FROM hoa_don", default),
            await s.Db.DocMotGiaTriAsync("SELECT COUNT(*) FROM phieu_doi_tra", default));
        var before = await Fingerprint();
        await s.Service.LookupAsync(new() { Ma = invoice.Code }, InvoiceLookupServiceTests.Cashier());
        await s.Service.LookupAsync(new() { SoDienThoai = s.Phone }, InvoiceLookupServiceTests.Cashier());
        Assert.Equal(before, await Fingerprint());
    }
}

using System.Security.Claims;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Tests.Returns;

public sealed class InvoiceLookupServiceTests
{
    internal static readonly DateTimeOffset Now = new(2026, 11, 17, 23, 59, 59, TimeSpan.FromHours(7));
    internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now.ToUniversalTime(); }
    internal static ClaimsPrincipal Cashier(string role = "TN", bool permission = true, string id = "7", bool mustChange = false) =>
        new(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role),
            new Claim(ThongTinXacThuc.ClaimQuyen, permission ? MaQuyen.DoiTra : MaQuyen.BanHang),
            new Claim(ThongTinXacThuc.ClaimBatBuocDoiMatKhau, mustChange ? "true" : "false")
        }, "test"));
    internal static OriginalInvoiceFact Invoice(string status = "DA_THANH_TOAN", long reserved = 0, bool noReturns = false,
        DateTime? purchased = null) => new() {
        Id = 10, Ma = "HD-TEST", ThuNganId = 99, TenThuNgan = "Thu ngân khác",
        NgayMua = purchased ?? new DateTime(2026, 11, 10, 21, 0, 0), TrangThai = status,
        TenKhachHang = "Khách test", SoDienThoai = "0901234567", TongThanhToan = 90000,
        ChiTiet = { new(5, "Tên lúc bán", "Hộp", 10, 10000, 1000, "Danh mục", noReturns, reserved) }
    };
    internal sealed class FakeStore(OriginalInvoiceFact? invoice, ReturnLookupPolicy? policy = null, bool missingPolicy = false) : IInvoiceLookupStore
    {
        public int Reads;
        public InvoiceLookupFilter? Last;
        public DateTime NowRead;
        public Task<InvoiceLookupSnapshot> ReadAsync(InvoiceLookupFilter filter, DateTime nowVietnam, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested(); Reads++; Last = filter; NowRead = nowVietnam;
            return Task.FromResult(new InvoiceLookupSnapshot(missingPolicy ? null : policy ?? new(1, 7),
                invoice is null ? [] : [invoice], invoice is null ? 0 : 1, 1, filter.Size));
        }
    }
    private static InvoiceLookupService Service(IInvoiceLookupStore store, DateTimeOffset? now = null) => new(store, new FixedTime(now ?? Now));
    private static InvoiceLookupRequest ByCode => new() { Ma = "HD-TEST" };

    [Fact]
    public async Task CashierSeesOtherCashiersInvoiceAndSavedNetUnitPrice_BR_DT_08()
    {
        var result = await Service(new FakeStore(Invoice())).LookupAsync(ByCode, Cashier());
        var invoice = Assert.Single(result.Items);
        Assert.Equal("Thu ngân khác", invoice.TenThuNgan);
        Assert.Equal(9000, Assert.Single(invoice.ChiTiet).GiaThucTraMoiDonVi);
        Assert.True(invoice.CoTheDoiTra);
        Assert.Equal("******4567", invoice.SoDienThoai);
        Assert.Equal(TimeSpan.FromHours(7), invoice.NgayMua.Offset);
    }

    [Theory]
    [InlineData("2026-11-17T16:59:59Z", true)]
    [InlineData("2026-11-17T17:00:00Z", false)]
    public async Task DeadlineIsEndOfNthVietnamCalendarDay(string now, bool valid)
    {
        var result = await Service(new FakeStore(Invoice()), DateTimeOffset.Parse(now)).LookupAsync(ByCode, Cashier());
        var invoice = Assert.Single(result.Items);
        Assert.Equal(new DateOnly(2026, 11, 17), invoice.NgayHetHan);
        Assert.Equal(valid, invoice.CoTheDoiTra);
        Assert.Equal(!valid, invoice.CanDuyetNgoaiLe);
    }

    [Theory]
    [InlineData(0, 10, true)]
    [InlineData(6, 4, true)]
    [InlineData(10, 0, false)]
    [InlineData(12, 0, false)]
    public async Task RemainingQuantityNeverNegative(long reserved, long remaining, bool valid)
    {
        var result = await Service(new FakeStore(Invoice(reserved: reserved))).LookupAsync(ByCode, Cashier());
        var line = Assert.Single(Assert.Single(result.Items).ChiTiet);
        Assert.Equal(remaining, line.SoLuongConDuocTra);
        Assert.Equal(valid, line.HopLe);
        if (!valid) Assert.Contains("HET_SO_LUONG", line.LyDo);
    }

    [Fact]
    public async Task CancelledInvoiceIsVisibleButCannotReturnOrBypassWithApproval_E2()
    {
        var result = await Service(new FakeStore(Invoice("DA_HUY", noReturns: true))).LookupAsync(ByCode, Cashier());
        var invoice = Assert.Single(result.Items);
        Assert.True(invoice.DaHuy); Assert.False(invoice.CoTheDoiTra); Assert.False(invoice.CanDuyetNgoaiLe);
        Assert.Contains("HOA_DON_DA_HUY", Assert.Single(invoice.ChiTiet).LyDo);
    }

    [Fact]
    public async Task RestrictedCategoryRequiresApproval_E3()
    {
        var result = await Service(new FakeStore(Invoice(noReturns: true))).LookupAsync(ByCode, Cashier());
        var line = Assert.Single(Assert.Single(result.Items).ChiTiet);
        Assert.False(line.HopLe); Assert.True(line.CanDuyetNgoaiLe);
        Assert.Contains("DANH_MUC_KHONG_DOI_TRA", line.LyDo);
    }

    [Fact]
    public async Task CurrentPolicyControlsDeadline()
    {
        var result = await Service(new FakeStore(Invoice(), new(2, 3))).LookupAsync(ByCode, Cashier());
        var invoice = Assert.Single(result.Items);
        Assert.Equal(2, invoice.ChinhSachId); Assert.Equal(new DateOnly(2026, 11, 13), invoice.NgayHetHan);
        Assert.False(invoice.CoTheDoiTra); Assert.True(invoice.CanDuyetNgoaiLe);
    }

    [Fact]
    public async Task NoInvoiceProducesEmptyPage_E1()
    {
        var result = await Service(new FakeStore(null)).LookupAsync(ByCode, Cashier());
        Assert.Empty(result.Items); Assert.Equal(0, result.Total);
    }

    [Theory]
    [InlineData(null, null, 1, 20)]
    [InlineData("HD", "0901234567", 1, 20)]
    [InlineData("123456789012345678901", null, 1, 20)]
    [InlineData(null, "090123456", 1, 20)]
    [InlineData(null, "1901234567", 1, 20)]
    [InlineData(null, "090123456%", 1, 20)]
    [InlineData("HD", null, 0, 20)]
    [InlineData("HD", null, 1, 0)]
    public async Task InvalidSearchReturns422BeforeDatabase(string? code, string? phone, int page, int size)
    {
        var store = new FakeStore(Invoice());
        var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => Service(store).LookupAsync(
            new() { Ma = code, SoDienThoai = phone, Page = page, Size = size }, Cashier()));
        Assert.Equal(422, error.MaHttp); Assert.Equal(0, store.Reads);
    }

    [Theory]
    [InlineData("AD", true, "7", false)]
    [InlineData("NK", true, "7", false)]
    [InlineData("TN", false, "7", false)]
    [InlineData("TN", true, "abc", false)]
    [InlineData("TN", true, "0", false)]
    [InlineData("TN", true, "7", true)]
    public async Task InvalidActorOrForcedPasswordDeniedBeforeDatabase(string role, bool permission, string id, bool change)
    {
        var store = new FakeStore(Invoice());
        var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => Service(store).LookupAsync(ByCode, Cashier(role, permission, id, change)));
        Assert.Equal(403, error.MaHttp); Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task AnonymousReturns401()
    {
        var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => Service(new FakeStore(null)).LookupAsync(ByCode, new ClaimsPrincipal()));
        Assert.Equal(401, error.MaHttp);
    }

    [Fact]
    public async Task TrimsSearchCapsPageSizeAndPassesVietnamTime()
    {
        var store = new FakeStore(Invoice());
        await Service(store).LookupAsync(new() { SoDienThoai = " 0901234567 ", Size = 500 }, Cashier());
        Assert.Equal("0901234567", store.Last!.SoDienThoai); Assert.Equal(100, store.Last.Size);
        Assert.Null(store.Last.Ma); Assert.Equal(Now.DateTime, store.NowRead);
    }

    [Fact]
    public async Task MissingEffectivePolicyReturnsConflict()
    {
        var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => Service(new FakeStore(null, missingPolicy: true)).LookupAsync(ByCode, Cashier()));
        Assert.Equal(409, error.MaHttp);
    }

    [Fact]
    public async Task FutureInvoiceCannotBecomeAnApprovalException()
    {
        var result = await Service(new FakeStore(Invoice(purchased: Now.DateTime.AddDays(1)))).LookupAsync(ByCode, Cashier());
        var invoice = Assert.Single(result.Items);
        Assert.False(invoice.CoTheDoiTra); Assert.False(invoice.CanDuyetNgoaiLe);
    }
}

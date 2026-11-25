using System.Security.Claims;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConvenienceStore.Web.Tests.Returns;

[Collection(CsdlKhoCollection.Ten)]
public sealed class ExchangePostgreSqlTests(CsdlKhoFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 10, 0, 0, TimeSpan.FromHours(7));
    private readonly TimeProvider clock = new InvoiceLookupServiceTests.FixedTime(Now);

    [Theory]
    [InlineData(20000, 3, 3, 0, 0, 0)]
    [InlineData(30000, 3, 4, 2, 10000, 12000)]
    public async Task CompletesExchangeWithStockLinkedInvoicesCashAndRevenue(
        long newPrice, int stock, int originalAfter, int newAfter, long cashDue, long received)
    {
        var s = await SeedAsync(newPrice, stock, sameProduct: cashDue == 0);
        var cashBefore = await Scalar("SELECT so_du FROM quy_tien_mat");
        await using var db = fixture.TaoDbContext();
        var exchange = Service(db);
        var user = Cashier(s.Cashier);
        var request = Request(s);
        var preview = await exchange.PreviewAsync(request, user);
        Assert.Equal(20000, preview.Returned.Refund);
        Assert.Equal(cashDue, preview.CashDue);
        request.ConfirmationToken = preview.ConfirmationToken;
        request.CashReceived = received;
        request.CashConfirmed = cashDue > 0;

        var id = await exchange.CreateAsync(request, user);
        var receipt = await exchange.ReceiptAsync(id, user);
        Assert.Equal(newPrice, receipt.NewTotal);
        Assert.Equal(received - cashDue, receipt.Change);
        Assert.Equal(originalAfter, await Stock(s.OriginalProduct));
        if (s.OriginalProduct != s.NewProduct) Assert.Equal(newAfter, await Stock(s.NewProduct));
        Assert.Equal(20000, await Scalar("SELECT tien_tru_hang_doi FROM hoa_don WHERE id=@id", ("@id", receipt.NewInvoiceId)));
        Assert.Equal(0, await Scalar("SELECT tien_hoan FROM phieu_doi_tra WHERE id=@id", ("@id", id)));
        Assert.Equal(cashBefore + cashDue, await Scalar("SELECT so_du FROM quy_tien_mat"));
        Assert.Equal(cashDue, await Scalar("SELECT COALESCE(SUM(so_tien),0) FROM giao_dich_tien_mat WHERE hoa_don_id=@id", ("@id", receipt.NewInvoiceId)));
        Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM the_kho WHERE loai_bien_dong='TRA_HANG' AND chung_tu_id=@id", ("@id", id)));
        Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM the_kho WHERE loai_bien_dong='BAN_HANG' AND chung_tu_id=@id", ("@id", receipt.NewInvoiceId)));
        var reports = await new PostgreSqlTransactionalReportsDataSource(db)
            .GetReturnsAsync(Now.AddHours(-1), Now.AddHours(1), default);
        Assert.Equal(20000, Assert.Single(reports, x => x.Id == id).Lines.Single().RefundAmount);
    }

    [Theory]
    [InlineData(10000, 3, 422, "DT03_NEGATIVE_DIFFERENCE")]
    [InlineData(30000, 0, 409, "DOI_TRA_XUNG_DOT")]
    public async Task BlocksNegativeDifferenceAndInsufficientStock(long price, int stock, int status, string code)
    {
        var s = await SeedAsync(price, stock);
        await using var db = fixture.TaoDbContext();
        var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => Service(db).PreviewAsync(Request(s), Cashier(s.Cashier)));
        Assert.Equal(status, error.MaHttp);
        Assert.Equal(code, error.Ma);
        Assert.Equal(0, await CountExchanges(s));
    }

    [Fact]
    public async Task RejectsAConfirmationAfterTheCartChanges()
    {
        var s = await SeedAsync(30000, 3);
        await using var db = fixture.TaoDbContext();
        var exchange = Service(db);
        var request = await ConfirmAsync(exchange, s);
        await fixture.ChayAsync("UPDATE ct_gio_hang SET so_luong=2 WHERE gio_hang_id=@id", ("@id", s.Cart));
        var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => exchange.CreateAsync(request, Cashier(s.Cashier)));
        Assert.Equal(409, error.MaHttp);
        Assert.Equal(0, await CountExchanges(s));
        Assert.Equal(3, await Stock(s.NewProduct));
    }

    [Fact]
    public async Task BlocksAnExchangeThatRequiresDt06()
    {
        var s = await SeedAsync(30000, 3);
        await fixture.ChayAsync("""
            WITH category AS (
                INSERT INTO danh_muc(ten, khong_doi_tra) VALUES ('Danh mục cần duyệt DT03', TRUE) RETURNING id
            )
            UPDATE san_pham SET danh_muc_id = (SELECT id FROM category) WHERE id = @id
            """, ("@id", s.OriginalProduct));
        await using var db = fixture.TaoDbContext();
        var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => Service(db).PreviewAsync(Request(s), Cashier(s.Cashier)));
        Assert.Equal(409, error.MaHttp);
        Assert.Equal(0, await CountExchanges(s));
    }

    [Fact]
    public async Task TwoCashiersCannotExchangeTheSamePurchasedUnit()
    {
        var first = await SeedAsync(30000, 4);
        var secondCashier = await InsertCashierAsync();
        var secondCart = await InsertActiveCartAsync(secondCashier, first.NewProduct, 30000, null);
        var second = first with { Cashier = secondCashier, Cart = secondCart };
        await using var firstDb = fixture.TaoDbContext();
        await using var secondDb = fixture.TaoDbContext();
        var firstService = Service(firstDb);
        var secondService = Service(secondDb);
        var firstRequest = await ConfirmAsync(firstService, first);
        var secondRequest = await ConfirmAsync(secondService, second);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Attempt(ExchangeService service, Seed seed, ExchangeRequest request)
        {
            await start.Task;
            try
            {
                await service.CreateAsync(request, Cashier(seed.Cashier));
                return true;
            }
            catch (LoiNghiepVu error) when (error.MaHttp is 409 or 422)
            {
                return false;
            }
        }
        var attempts = new[] { Attempt(firstService, first, firstRequest), Attempt(secondService, second, secondRequest) };
        start.SetResult();
        Assert.Single(await Task.WhenAll(attempts), x => x);
        Assert.Equal(1, await CountExchanges(first));
        Assert.Equal(3, await Stock(first.NewProduct));
    }

    [Fact]
    public async Task FailureAfterCheckoutRollsBackInvoiceCartStockPointsAndCash()
    {
        var s = await SeedAsync(30000, 3, member: true);
        var cashBefore = await Scalar("SELECT so_du FROM quy_tien_mat");
        await using var db = fixture.TaoDbContext();
        var service = Service(db, new FailingPoints());
        var request = await ConfirmAsync(service, s);
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, Cashier(s.Cashier)));
        Assert.Equal("Lỗi giả lập sau khi lưu hóa đơn và kho.", failure.Message);
        Assert.Equal(0, await CountExchanges(s));
        Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM hoa_don WHERE gio_hang_id=@id", ("@id", s.Cart)));
        Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM gio_hang WHERE id=@id AND trang_thai='DANG_BAN'", ("@id", s.Cart)));
        Assert.Equal(3, await Stock(s.OriginalProduct));
        Assert.Equal(3, await Stock(s.NewProduct));
        Assert.Equal(cashBefore, await Scalar("SELECT so_du FROM quy_tien_mat"));
        Assert.Equal(2, await Scalar("SELECT diem_hien_co FROM khach_hang WHERE id=@id", ("@id", s.Customer)));
        Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM lich_su_diem WHERE khach_hang_id=@id", ("@id", s.Customer)));
    }

    private ExchangeService Service(CuaHangDbContext db, IReturnPointsGateway? points = null)
    {
        var stock = BoTroGoiDichVuKho.TaoDichVu(db);
        points ??= new ReturnPointsGateway();
        return new(db, new ReturnOrderService(db, stock, points, clock),
            new PostgreSqlPosCheckoutStore(db, stock, new CashLedgerService(), NullLogger<PostgreSqlPosCheckoutStore>.Instance),
            stock, points, clock);
    }

    private async Task<Seed> SeedAsync(long price, int stock, bool sameProduct = false, bool member = false)
    {
        var inventory = new BoTroGoiDichVuKho(fixture);
        var original = await inventory.TaoSanPhamCoTonAsync("DT03", 3, 5000);
        var replacement = sameProduct ? original : await inventory.TaoSanPhamCoTonAsync("DT03", stock, 5000);
        var cashier = await InsertCashierAsync();
        long? customer = member ? await Scalar("""
            INSERT INTO khach_hang(ho_ten, so_dien_thoai, diem_hien_co, nguoi_tao_id)
            VALUES ('Khách DT03', @phone, 2, @cashier) RETURNING id
            """, ("@phone", "0" + Random.Shared.Next(100000000, 999999999)), ("@cashier", cashier)) : null;
        var originalCart = await Scalar("""
            INSERT INTO gio_hang(thu_ngan_id, khach_hang_id, trang_thai, hoan_tat_luc)
            VALUES (@cashier, @kh, 'DA_HOAN_TAT', @paid) RETURNING id
            """, ("@cashier", cashier), ("@kh", customer), ("@paid", Now.AddDays(-1)));
        var invoice = await Scalar("""
            INSERT INTO hoa_don(ma_hoa_don, gio_hang_id, thu_ngan_id, khach_hang_id, thanh_toan_luc,
                tong_tien_hang, tong_giam, tong_thanh_toan, tien_khach_dua, tien_thua, diem_cong, so_tien_mot_diem)
            VALUES (@code, @cart, @cashier, @kh, @paid, 20000, 0, 20000, 20000, 0, @points, @rate) RETURNING id
            """, ("@code", "HD" + Guid.NewGuid().ToString("N")[..12]), ("@cart", originalCart),
            ("@cashier", cashier), ("@kh", customer), ("@paid", Now.AddDays(-1)), ("@points", member ? 2 : 0),
            ("@rate", member ? 10000L : null));
        await fixture.ChayAsync("""
            INSERT INTO ct_hoa_don(hoa_don_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, don_gia, gia_von)
            VALUES (@invoice, @product, 'Hàng gốc', 'Cái', 1, 20000, 5000)
            """, ("@invoice", invoice), ("@product", original));
        var cart = await InsertActiveCartAsync(cashier, replacement, price, customer);
        return new(invoice, cart, cashier, original, replacement, customer);
    }

    private Task<long> InsertCashierAsync() => Scalar("""
        INSERT INTO tai_khoan(ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id, bat_buoc_doi_mk)
        VALUES (@name, 'fixture', 'Thu ngân DT03', 2, FALSE) RETURNING id
        """, ("@name", "dt03" + Guid.NewGuid().ToString("N")[..12]));

    private async Task<long> InsertActiveCartAsync(long cashier, long product, long price, long? customer)
    {
        var cart = await Scalar("INSERT INTO gio_hang(thu_ngan_id, khach_hang_id) VALUES (@cashier, @kh) RETURNING id",
            ("@cashier", cashier), ("@kh", customer));
        await fixture.ChayAsync("""
            INSERT INTO ct_gio_hang(gio_hang_id, san_pham_id, so_luong, don_gia)
            VALUES (@cart, @product, 1, @price)
            """, ("@cart", cart), ("@product", product), ("@price", price));
        return cart;
    }

    private static async Task<ExchangeRequest> ConfirmAsync(ExchangeService service, Seed s)
    {
        var request = Request(s);
        var preview = await service.PreviewAsync(request, Cashier(s.Cashier));
        request.ConfirmationToken = preview.ConfirmationToken;
        request.CashReceived = preview.CashDue;
        request.CashConfirmed = true;
        return request;
    }

    private static ClaimsPrincipal Cashier(long id) => InvoiceLookupServiceTests.Cashier(id: id.ToString());

    private static ExchangeRequest Request(Seed s) => new()
    {
        InvoiceId = s.Invoice,
        Lines = [new() { ProductId = s.OriginalProduct, Quantity = 1, Reason = "LOI_SAN_PHAM", Condition = "Nguyên trạng" }]
    };

    private async Task<long> Scalar(string sql, params (string, object?)[] parameters) =>
        Convert.ToInt64(await fixture.DocMotGiaTriAsync(sql, parameters));
    private Task<long> Stock(long product) => Scalar("SELECT so_luong_ton FROM ton_kho WHERE san_pham_id=@id", ("@id", product));
    private Task<long> CountExchanges(Seed s) => Scalar("SELECT COUNT(*) FROM phieu_doi_tra WHERE hoa_don_goc_id=@id", ("@id", s.Invoice));
    private sealed record Seed(long Invoice, long Cart, long Cashier, long OriginalProduct, long NewProduct, long? Customer);

    private sealed class FailingPoints : IReturnPointsGateway
    {
        public Task<int> RecoverAsync(CuaHangDbContext context, long invoiceId, long returnId, long cashierId,
            long refund, CancellationToken ct) => throw new InvalidOperationException("Lỗi giả lập sau khi lưu hóa đơn và kho.");
    }
}

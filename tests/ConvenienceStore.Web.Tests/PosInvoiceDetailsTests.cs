using System.Security.Claims;
using System.Text.Json;
using ConvenienceStore.Web.Controllers;
using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConvenienceStore.Web.Tests;

public class PosInvoiceDetailsTests
{
    private static PosInvoiceViewModel Invoice(string status = "DA_THANH_TOAN") => new()
    {
        InvoiceId = 15, InvoiceCode = "HD-15", CashierId = 7, Status = status,
        CreatedAt = new DateTime(2026, 10, 29, 10, 0, 0),
        CustomerId = 9, CustomerName = "Khách A", PointsEarned = 10,
        Subtotal = 100000, Discount = 10000, Total = 90000, CashReceived = 100000, Change = 10000,
        Items = [new() { ProductId = 3, ProductName = "Tên lúc bán", Quantity = 2,
            UnitPrice = 50000, Discount = 10000, LineTotal = 90000, PromotionId = 1, PromotionCode = "KM01" }],
        RelatedReturns = [new(2, "DT02", "TRA_HANG", "HOAN_TAT", new(2026, 10, 29), 45000, 0, 5, true)]
    };
    private sealed class FakeStore(PosInvoiceViewModel? invoice) : IPosInvoiceStore
    {
        public int Reads;
        public Task<PosInvoiceViewModel?> GetByIdAsync(long id, CancellationToken ct = default)
        { ct.ThrowIfCancellationRequested(); Reads++; return Task.FromResult(invoice); }
    }
    private static PosInvoiceService Service(IPosInvoiceStore store) => new(store, NullLogger<PosInvoiceService>.Instance);
    private static PosInvoiceViewer Cashier => new(7, false, true, false);
    private static PosInvoiceViewer Admin => new(1, true, false, false);
    private static ClaimsPrincipal User(bool admin = false, int id = 7) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, id.ToString()),
        new Claim(ThongTinXacThuc.ClaimQuyen, admin ? MaQuyen.HoaDonXemTatCa : MaQuyen.HoaDonXemCuaMinh)
    ], "test"));
    private static ControllerContext Context(bool admin = false, int id = 7) => new()
    { HttpContext = new DefaultHttpContext { User = User(admin, id) } };

    [Theory]
    [InlineData("DA_THANH_TOAN")]
    [InlineData("DA_HUY")]
    public async Task OwnerCanViewBothStatusesAndReprint(string status)
    {
        var invoice = (await Service(new FakeStore(Invoice(status))).GetDetailsAsync(15, Cashier))!;
        Assert.True(invoice.IsReprint);
        Assert.Equal(status == "DA_HUY", invoice.IsCancelled);
        Assert.Equal("Tên lúc bán", invoice.Items[0].ProductName);
        Assert.Equal(10000, invoice.Items[0].Discount);
        Assert.Equal("KM01", invoice.Items[0].PromotionCode);
        Assert.Equal(10, invoice.PointsEarned);
        Assert.Single(invoice.RelatedReturns);
    }

    [Fact]
    public async Task AdminCanViewOtherCashierWithoutSalesPermission() =>
        Assert.NotNull(await Service(new FakeStore(Invoice())).GetDetailsAsync(15, Admin));

    [Fact]
    public async Task OtherCashierDenied() =>
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(new FakeStore(Invoice()))
            .GetDetailsAsync(15, new(8, false, true, true)));

    [Fact]
    public async Task NoPermissionDeniedBeforeReadingStore()
    {
        var store = new FakeStore(Invoice());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(store).GetDetailsAsync(15, new(7, false, false, true)));
        Assert.Equal(0, store.Reads);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidIdDoesNotReadStore(long id)
    {
        var store = new FakeStore(Invoice());
        Assert.Null(await Service(store).GetDetailsAsync(id, Cashier));
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task MissingInvoiceReturnsNull() =>
        Assert.Null(await Service(new FakeStore(null)).GetDetailsAsync(15, Cashier));

    [Fact]
    public async Task OriginalPos07StillRejectsCancelledInvoice() =>
        Assert.Null(await Service(new FakeStore(Invoice("DA_HUY"))).GetByIdAsync(15));

    [Fact]
    public async Task ReprintDoesNotChangeMoneyOrPoints()
    {
        var invoice = Invoice();
        var service = Service(new FakeStore(invoice));
        await service.GetDetailsAsync(15, Cashier);
        await service.GetDetailsAsync(15, Cashier);
        Assert.Equal(90000, invoice.Total);
        Assert.Equal(10, invoice.PointsEarned);
        Assert.Single(invoice.Items);
        Assert.Single(invoice.RelatedReturns);
    }

    [Fact]
    public async Task CancellationPropagated()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => Service(new FakeStore(Invoice())).GetDetailsAsync(15, Cashier, cts.Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApiDetailAndPrintRejectOtherCashier(bool print)
    {
        var controller = new InvoiceDetailsApiController(Service(new FakeStore(Invoice()))) { ControllerContext = Context(id: 8) };
        var result = Assert.IsType<ObjectResult>(print ? await controller.Print(15, default) : await controller.Details(15, default));
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(MaLoiChung.KhongDuQuyen, Assert.IsType<PhanHoiLoi>(result.Value).Code);
    }

    [Fact]
    public async Task ApiCancelledPrintCarriesBothMarkers()
    {
        var controller = new InvoiceDetailsApiController(Service(new FakeStore(Invoice("DA_HUY")))) { ControllerContext = Context() };
        var result = Assert.IsType<OkObjectResult>(await controller.Print(15, default));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal("K80", json.RootElement.GetProperty("khoGiay").GetString());
        Assert.True(json.RootElement.GetProperty("inLai").GetBoolean());
        Assert.True(json.RootElement.GetProperty("daHuy").GetBoolean());
        Assert.Equal("DA_HUY", json.RootElement.GetProperty("hoaDon").GetProperty("trangThai").GetString());
    }

    [Fact]
    public void ApiContractContainsOriginalAmountsAndVietnamTime()
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(PosInvoiceApiData.From(Invoice())));
        var root = json.RootElement;
        Assert.Equal(90000, root.GetProperty("thanhTien").GetInt64());
        Assert.EndsWith("+07:00", root.GetProperty("thoiDiemTao").GetString());
        Assert.Equal(3, root.GetProperty("chiTiet")[0].GetProperty("sanPhamId").GetInt32());
        Assert.Single(root.GetProperty("phieuDoiTra").EnumerateArray());
    }

    [Fact]
    public async Task MvcCancelledInvoiceIsViewableByAdmin()
    {
        var controller = new InvoiceDetailsController(Service(new FakeStore(Invoice("DA_HUY")))) { ControllerContext = Context(true) };
        var result = Assert.IsType<ViewResult>(await controller.Details(15, default));
        Assert.Equal("~/Views/Invoices/Details.cshtml", result.ViewName);
        Assert.True(Assert.IsType<PosInvoiceViewModel>(result.Model).IsCancelled);
    }

    [Fact]
    public async Task MvcRejectsOtherCashier()
    {
        var controller = new InvoiceDetailsController(Service(new FakeStore(Invoice()))) { ControllerContext = Context(id: 8) };
        Assert.IsType<ForbidResult>(await controller.Details(15, default));
    }

    [Fact]
    public async Task ApiMissingInvoiceUsesCommonErrorEnvelope()
    {
        var controller = new InvoiceDetailsApiController(Service(new FakeStore(null))) { ControllerContext = Context() };
        var result = Assert.IsType<ObjectResult>(await controller.Details(15, default));
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(MaLoiChung.KhongTimThay, Assert.IsType<PhanHoiLoi>(result.Value).Code);
    }

    [Fact]
    public async Task ApiInvalidIdReturns422()
    {
        var controller = new InvoiceDetailsApiController(Service(new FakeStore(Invoice()))) { ControllerContext = Context() };
        var result = Assert.IsType<ObjectResult>(await controller.Details(0, default));
        Assert.Equal(422, result.StatusCode);
    }

    [Fact]
    public void BothControllersRequireInvoicePolicy()
    {
        foreach (var type in new[] { typeof(InvoiceDetailsController), typeof(InvoiceDetailsApiController) })
            Assert.Equal(PosInvoiceAccess.PolicyXem, Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Policy);
    }

    [Fact]
    public void Pos07AcceptsInvoicePartiallyPaidWithExchangeCredit()
    {
        var invoice = Invoice(); invoice.ExchangeCredit = 40000; invoice.CashReceived = 50000; invoice.Change = 0;
        Assert.True(Service(new FakeStore(invoice)).ValidateForPrinting(invoice, out _));
    }
}

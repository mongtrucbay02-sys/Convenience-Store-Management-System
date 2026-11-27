using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace ConvenienceStore.Web.Tests.Returns;

public sealed partial class ExchangePostgreSqlTests
{
    // Login has no object ownership, superuser or BYPASSRLS; it inherits only the runtime grants.
    private async Task WithRuntimeDb(Func<CuaHangDbContext, Task> action)
    {
        var role = "dt_runtime_" + Guid.NewGuid().ToString("N");
        var password = Guid.NewGuid().ToString("N");
        await fixture.ChayAsync($"CREATE ROLE {role} LOGIN NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE PASSWORD '{password}'; GRANT smartmart_backend TO {role}");
        try
        {
            var settings = new NpgsqlConnectionStringBuilder(fixture.ChuoiKetNoi)
            { Username = role, Password = password, Pooling = false };
            await using var db = new CuaHangDbContext(new DbContextOptionsBuilder<CuaHangDbContext>()
                .UseNpgsql(settings.ConnectionString, pg => pg.SetPostgresVersion(17, 0)).Options);
            await db.Database.OpenConnectionAsync();
            await using (var cmd = db.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = """
                    SELECT NOT r.rolsuper AND NOT r.rolbypassrls AND NOT r.rolcreatedb AND NOT r.rolcreaterole
                       AND current_user=session_user AND current_user<>pg_get_userbyid(d.datdba)
                    FROM pg_roles r CROSS JOIN pg_database d
                    WHERE r.rolname=current_user AND d.datname=current_database()
                    """;
                Assert.Equal(true, await cmd.ExecuteScalarAsync());
            }
            await action(db);
        }
        finally { await fixture.ChayAsync($"DROP ROLE {role}"); }
    }

    [Fact]
    public async Task SamePriceReplacementRequiresReapprovalAndPreservesEveryDecision_RuntimeRole()
    {
        var s = await SeedApprovalAsync(member: true);
        var replacement = await new BoTroGoiDichVuKho(fixture).TaoSanPhamCoTonAsync("DT03-NEW", 3, 5000);
        await WithRuntimeDb(async db =>
        {
            var exchange = Service(db);
            var approvals = Approvals(db);
            var id = await exchange.CreateAsync(await ConfirmAsync(exchange, s), Cashier(s.Cashier));
            await approvals.DecideAsync(id, fixture.TaiKhoanAdminId, true, "Duyệt phương án 1", false, default);
            await fixture.ChayAsync("UPDATE ct_gio_hang SET san_pham_id=@product WHERE gio_hang_id=@cart",
                ("@product", replacement), ("@cart", s.Cart));
            var denied = await Assert.ThrowsAsync<LoiNghiepVu>(() => approvals.CompleteExchangeAsync(id, s.Cashier, 10000, true, default));
            Assert.Equal("DT03_PHUONG_AN_THAY_DOI", denied.Ma);
            Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM hoa_don WHERE gio_hang_id=@id", ("@id", s.Cart)));
            var proposal = await approvals.PreviewResubmissionAsync(id, s.Cashier, default);
            await Assert.ThrowsAsync<LoiNghiepVu>(() => approvals.ResubmitAsync(id, s.Cashier, "stale-token", default));
            await approvals.ResubmitAsync(id, s.Cashier, proposal.Token, default);
            Assert.Equal("CHO_DUYET", (await approvals.DetailsAsync(id, null, default)).Slip.Status);
            await Assert.ThrowsAsync<LoiNghiepVu>(() => approvals.CompleteExchangeAsync(id, s.Cashier, 10000, true, default));
            await approvals.DecideAsync(id, fixture.TaiKhoanAdminId, true, "Duyệt phương án 2", false, default);
            await approvals.CompleteExchangeAsync(id, s.Cashier, 10000, true, default);
            var detail = await approvals.DetailsAsync(id, null, default);
            var history = ExchangePlanHistory.Read(detail.Slip.ExchangePlan)!;
            Assert.Equal(2, history.Versions.Length);
            Assert.Equal(s.NewProduct, Assert.Single(history.Versions[0].Lines).ProductId);
            Assert.Equal("Duyệt phương án 1", history.Versions[0].Note);
            Assert.Equal(replacement, Assert.Single(history.Current.Lines).ProductId);
            Assert.Equal("Duyệt phương án 2", history.Current.Note);
            var receipt = await exchange.ReceiptAsync(id, Cashier(s.Cashier));
            Assert.Equal(replacement, await Scalar("SELECT san_pham_id FROM ct_hoa_don WHERE hoa_don_id=@id", ("@id", receipt.NewInvoiceId)));
        });
    }

    [Fact]
    public async Task LegacyApprovedExchangeWithoutSnapshotCannotBeGrandfathered_RuntimeRole()
    {
        var s = await SeedApprovalAsync();
        await WithRuntimeDb(async db =>
        {
            var exchange = Service(db);
            var approvals = Approvals(db);
            var id = await exchange.CreateAsync(await ConfirmAsync(exchange, s), Cashier(s.Cashier));
            await approvals.DecideAsync(id, fixture.TaiKhoanAdminId, true, "Duyệt", false, default);
            await fixture.ChayAsync("UPDATE phieu_doi_tra SET phuong_an_doi=NULL WHERE id=@id", ("@id", id));
            var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => approvals.CompleteExchangeAsync(id, s.Cashier, 10000, true, default));
            Assert.Equal("DT03_PHUONG_AN_THAY_DOI", error.Ma);
            var proposal = await approvals.PreviewResubmissionAsync(id, s.Cashier, default);
            await approvals.ResubmitAsync(id, s.Cashier, proposal.Token, default);
            var history = ExchangePlanHistory.Read((await approvals.DetailsAsync(id, null, default)).Slip.ExchangePlan)!;
            Assert.Null(history.Current.Decision);
            Assert.Equal(fixture.TaiKhoanAdminId, history.LegacyDecision!.DecidedBy);
            Assert.Equal("Duyệt", history.LegacyDecision.Note);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LockedCashierCannotCompleteApprovedExchange_RuntimeRole(bool temporary)
    {
        var s = await SeedApprovalAsync();
        await WithRuntimeDb(async db =>
        {
            var exchange = Service(db);
            var approvals = Approvals(db);
            var id = await exchange.CreateAsync(await ConfirmAsync(exchange, s), Cashier(s.Cashier));
            await approvals.DecideAsync(id, fixture.TaiKhoanAdminId, true, "Duyệt", false, default);
            await fixture.ChayAsync(temporary
                ? "UPDATE tai_khoan SET khoa_tam_den=NOW()+INTERVAL '15 minutes' WHERE id=@id"
                : "UPDATE tai_khoan SET trang_thai='DA_KHOA', ly_do_khoa='Khóa kiểm thử' WHERE id=@id", ("@id", s.Cashier));
            var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => approvals.CompleteExchangeAsync(id, s.Cashier, 10000, true, default));
            Assert.Equal(403, error.MaHttp);
            Assert.Equal("DA_DUYET", (await approvals.DetailsAsync(id, null, default)).Slip.Status);
            Assert.Equal(3, await Stock(s.NewProduct));
        });
    }

    [Fact]
    public async Task InsufficientCashRollsBackAdminDecisionStockAndPoints_RuntimeRole()
    {
        var s = await SeedApprovalAsync(member: true);
        var balance = await Scalar("SELECT so_du FROM quy_tien_mat");
        try
        {
            await fixture.ChayAsync("UPDATE quy_tien_mat SET so_du=0 WHERE id=1");
            await WithRuntimeDb(async db =>
            {
                var returns = new ReturnOrderService(db, BoTroGoiDichVuKho.TaoDichVu(db), new ReturnPointsGateway(), clock);
                var request = Request(s);
                request.ConfirmationToken = (await returns.PreviewAsync(request, Cashier(s.Cashier))).ConfirmationToken;
                var id = await returns.CreateAsync(request, Cashier(s.Cashier));
                var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => Approvals(db).DecideAsync(id,
                    fixture.TaiKhoanAdminId, true, "Admin trực tiếp hoàn", true, default));
                Assert.Equal("QUY_TIEN_MAT_KHONG_DU", error.Ma);
                Assert.Equal("CHO_DUYET", (await Approvals(db).DetailsAsync(id, null, default)).Slip.Status);
                Assert.Null(await fixture.DocMotGiaTriAsync("SELECT nguoi_duyet_id FROM phieu_doi_tra WHERE id=@id", ("@id", id)));
                Assert.Equal(3, await Stock(s.OriginalProduct));
                Assert.Equal(2, await Scalar("SELECT diem_hien_co FROM khach_hang WHERE id=@id", ("@id", s.Customer)));
                Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM giao_dich_tien_mat WHERE phieu_doi_tra_id=@id", ("@id", id)));
            });
        }
        finally { await fixture.ChayAsync("UPDATE quy_tien_mat SET so_du=@balance WHERE id=1", ("@balance", balance)); }
    }

    [Fact]
    public async Task LostResponseAndRetryCannotRecordTheExchangeTwice_RuntimeRole()
    {
        var s = await SeedAsync(30000, 3, member: true);
        await WithRuntimeDb(async db =>
        {
            var exchange = Service(db);
            var request = await ConfirmAsync(exchange, s);
            var id = await exchange.CreateAsync(request, Cashier(s.Cashier));
            var retry = await Assert.ThrowsAsync<LoiNghiepVu>(() => exchange.CreateAsync(request, Cashier(s.Cashier)));
            Assert.Contains(retry.MaHttp, new[] { 409, 422 });
            Assert.Equal(1, await CountExchanges(s));
            Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM hoa_don WHERE gio_hang_id=@id", ("@id", s.Cart)));
            Assert.Equal(2, await Stock(s.NewProduct));
            Assert.Equal("HOAN_TAT", (await exchange.ReceiptAsync(id, Cashier(s.Cashier))).Status);
        });
    }

    [Fact]
    public async Task ExchangeAndOrdinaryPosCannotSellTheSameLastUnit_RuntimeRole()
    {
        var s = await SeedAsync(30000, 1);
        var otherCashier = await InsertCashierAsync();
        await InsertActiveCartAsync(otherCashier, s.NewProduct, 30000, null);
        await using var previewDb = fixture.TaoDbContext();
        var request = await ConfirmAsync(Service(previewDb), s);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Attempt(bool isExchange)
        {
            bool success = false;
            await WithRuntimeDb(async db =>
            {
                await gate.Task;
                try
                {
                    if (isExchange) await Service(db).CreateAsync(request, Cashier(s.Cashier));
                    else
                    {
                        await using var tx = await db.Database.BeginTransactionAsync();
                        await new PostgreSqlPosCheckoutStore(db, BoTroGoiDichVuKho.TaoDichVu(db),
                            new CashLedgerService(), NullLogger<PostgreSqlPosCheckoutStore>.Instance)
                            .SaveOrderAsync(otherCashier, 30000, default);
                        await tx.CommitAsync();
                    }
                    success = true;
                }
                catch (Exception ex) when (ex is LoiNghiepVu or PosCheckoutException) { }
            });
            return success;
        }
        var attempts = new[] { Attempt(true), Attempt(false) };
        gate.SetResult();
        Assert.Single(await Task.WhenAll(attempts), x => x);
        Assert.Equal(0, await Stock(s.NewProduct));
    }

    [Fact]
    public async Task ReturningAnExchangeInvoiceReconcilesPointsCashAndRevenue_RuntimeRole()
    {
        var s = await SeedAsync(30000, 3, member: true);
        var balance = await Scalar("SELECT so_du FROM quy_tien_mat");
        try
        {
            await fixture.ChayAsync("UPDATE quy_tien_mat SET so_du=50000 WHERE id=1");
            await WithRuntimeDb(async db =>
            {
                var exchange = Service(db);
                var id = await exchange.CreateAsync(await ConfirmAsync(exchange, s), Cashier(s.Cashier));
                var receipt = await exchange.ReceiptAsync(id, Cashier(s.Cashier));
                var nowClock = new InvoiceLookupServiceTests.FixedTime(DateTimeOffset.UtcNow);
                var returns = new ReturnOrderService(db, BoTroGoiDichVuKho.TaoDichVu(db), new ReturnPointsGateway(), nowClock);
                var request = new ReturnOrderRequest { InvoiceId = receipt.NewInvoiceId!.Value,
                    Lines = [new() { ProductId=s.NewProduct, Quantity=1, Reason="LOI_SAN_PHAM", Condition="Nguyên trạng" }], CashConfirmed=true };
                request.ConfirmationToken = (await returns.PreviewAsync(request, Cashier(s.Cashier))).ConfirmationToken;
                var returnId = await returns.CreateAsync(request, Cashier(s.Cashier));
                Assert.Equal(30000, await Scalar("SELECT tien_hoan FROM phieu_doi_tra WHERE id=@id", ("@id", returnId)));
                Assert.Equal(30000, await Scalar("SELECT so_du FROM quy_tien_mat"));
                Assert.Equal(0, await Scalar("SELECT diem_hien_co FROM khach_hang WHERE id=@id", ("@id", s.Customer)));
                Assert.Equal(3, await Stock(s.NewProduct));
                var reports = new PostgreSqlTransactionalReportsDataSource(db);
                var from = Now.AddDays(-2); var to = DateTimeOffset.UtcNow.AddDays(1);
                var sales = (await reports.GetPaidInvoicesAsync(from, to, default)).Where(x=>x.Id==s.Invoice || x.Id==receipt.NewInvoiceId).ToArray();
                var credits = (await reports.GetReturnsAsync(from, to, default)).Where(x=>x.Id==id || x.Id==returnId).ToArray();
                Assert.Equal(2, sales.Length); Assert.Equal(2, credits.Length);
                Assert.Equal(0, sales.Sum(x=>x.Lines.Sum(l=>l.Quantity*l.ListPriceSnapshot)-x.PromotionDiscount)
                    -credits.Sum(x=>x.Lines.Sum(l=>l.RefundAmount)));
            });
        }
        finally { await fixture.ChayAsync("UPDATE quy_tien_mat SET so_du=@balance WHERE id=1", ("@balance", balance)); }
    }

    [Fact]
    public async Task AdminRefundRecordsActualAdminWhileKeepingCashierOwnership_RuntimeRole()
    {
        var s = await SeedApprovalAsync(member: true);
        var balance = await Scalar("SELECT so_du FROM quy_tien_mat");
        try
        {
            await fixture.ChayAsync("UPDATE quy_tien_mat SET so_du=30000 WHERE id=1");
            await WithRuntimeDb(async db =>
            {
                var returns = new ReturnOrderService(db, BoTroGoiDichVuKho.TaoDichVu(db), new ReturnPointsGateway(), clock);
                var request = Request(s);
                request.ConfirmationToken = (await returns.PreviewAsync(request, Cashier(s.Cashier))).ConfirmationToken;
                var id = await returns.CreateAsync(request, Cashier(s.Cashier));
                await Approvals(db).DecideAsync(id, fixture.TaiKhoanAdminId, true, "Tôi trực tiếp hoàn tiền", true, default);
                Assert.NotEqual(s.Cashier, fixture.TaiKhoanAdminId);
                Assert.Equal(fixture.TaiKhoanAdminId, await Scalar("SELECT nguoi_thuc_hien_id FROM giao_dich_tien_mat WHERE phieu_doi_tra_id=@id", ("@id", id)));
                Assert.Equal(s.Cashier, await Scalar("SELECT thu_ngan_id FROM phieu_doi_tra WHERE id=@id", ("@id", id)));
                Assert.Equal("Admin test", (await new ReturnRefundService(new CashLedgerService()).ReceiptAsync(db, id, s.Cashier)).Cashier);
                Assert.Equal(10000, await Scalar("SELECT so_du FROM quy_tien_mat"));
            });
        }
        finally { await fixture.ChayAsync("UPDATE quy_tien_mat SET so_du=@balance WHERE id=1", ("@balance", balance)); }
    }

    [Fact]
    public async Task OrdinaryPosUsingOldCartCannotSubstituteAnotherCartForApprovedPlan_RuntimeRole()
    {
        var s = await SeedApprovalAsync();
        await WithRuntimeDb(async db =>
        {
            var exchange = Service(db); var approvals = Approvals(db);
            var id = await exchange.CreateAsync(await ConfirmAsync(exchange, s), Cashier(s.Cashier));
            await approvals.DecideAsync(id, fixture.TaiKhoanAdminId, true, "Duyệt giỏ ban đầu", false, default);
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                await new PostgreSqlPosCheckoutStore(db, BoTroGoiDichVuKho.TaoDichVu(db), new CashLedgerService(),
                    NullLogger<PostgreSqlPosCheckoutStore>.Instance).SaveOrderAsync(s.Cashier, 30000, default);
                await tx.CommitAsync();
            }
            var differentCart = await InsertActiveCartAsync(s.Cashier, s.NewProduct, 30000, null);
            var error = await Assert.ThrowsAsync<LoiNghiepVu>(() => approvals.CompleteExchangeAsync(id, s.Cashier, 10000, true, default));
            Assert.Equal("DT03_PHUONG_AN_THAY_DOI", error.Ma);
            Assert.Equal("DA_DUYET", (await approvals.DetailsAsync(id, null, default)).Slip.Status);
            Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM hoa_don WHERE gio_hang_id=@id", ("@id", differentCart)));
        });
    }
}

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConvenienceStore.Web.Tests.PostgreSql;
public sealed partial class PostgreSqlRuntimeTests
{
    [Fact]
    public async Task SM01_ForcedPassword_BlocksMvcAndApi_ThenAllowsChangeAndLogout()
    {
        await using var db=Db();
        await db.ChayAsync("UPDATE tai_khoan SET bat_buoc_doi_mk=TRUE WHERE id=1",default);
        var address=await StartWeb();
        var cookies=new CookieContainer();
        using var client=new HttpClient(new HttpClientHandler {CookieContainer=cookies,AllowAutoRedirect=false}) {BaseAddress=address};
        var login=await client.PostAsJsonAsync("/api/v1/auth/login",new {tenDangNhap="pg_admin",matKhau=Password});
        Assert.Equal(HttpStatusCode.OK,login.StatusCode);
        foreach(var path in new[]{"/Customers","/NhaCungCap","/Kho","/Promotions/Promotions","/api/v1/products"})
        {
            var response=await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
            Assert.Contains("PHAI_DOI_MAT_KHAU",await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsync("/Customers/Save",
            new FormUrlEncodedContent(new Dictionary<string,string>{{"Name","Blocked write"},{"Phone","0901234567"}}))).StatusCode);
        Assert.Empty(await db.KhachHangs.ToListAsync());
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN",Uri.UnescapeDataString(cookies.GetCookies(address)["XSRF-TOKEN"]!.Value));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/products",new {})).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsync("/api/v1/auth/logout",null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/auth/login",new {tenDangNhap="pg_admin",matKhau=Password})).StatusCode);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN",Uri.UnescapeDataString(cookies.GetCookies(address)["XSRF-TOKEN"]!.Value));
        var change=await client.PostAsJsonAsync("/api/v1/auth/change-password",new {matKhauHienTai=Password,matKhauMoi="ChangedTest123!",nhapLaiMatKhauMoi="ChangedTest123!"});
        Assert.Equal(HttpStatusCode.NoContent,change.StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/Customers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsync("/api/v1/auth/logout",null)).StatusCode);
    }

    [Fact]
    public async Task SM02_ImmediatePrice_PreservesDatabasePrecisionAndIsEffective()
    {
        var product=await Product();await using var db=Db();
        var result=await new EfProductPricingRepository(db).CreateAsync(product,new(){GiaBan=21000,PhienBan=1},1);
        var stored=await db.LichSuGia.SingleAsync(p=>p.Id==result.Id);
        Assert.Equal(DateTimeKind.Utc,stored.HieuLucTu.Kind);
        Assert.Equal(21000,(await new PostgreSqlPosCatalog(Configuration).FindAsync(product))!.Price);
    }

    [Fact]
    public async Task SM02_ExplicitVietnamPastIsRejected_AndFutureOffsetRoundTripsAsUtc()
    {
        var product=await Product();await using var db=Db();
        var repository=new EfProductPricingRepository(db);
        var past=DateTimeOffset.UtcNow.AddHours(-1).ToOffset(TimeSpan.FromHours(7));
        await Assert.ThrowsAsync<LoiNghiepVu>(()=>repository.CreateAsync(product,new(){GiaBan=21000,PhienBan=1,HieuLucTu=past.ToString("O")},1));
        var future=new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToOffset(TimeSpan.FromHours(7));
        future=future.AddTicks(-(future.Ticks%TimeSpan.TicksPerSecond));
        var result=await repository.CreateAsync(product,new(){GiaBan=22000,PhienBan=1,HieuLucTu=future.ToString("O")},1);
        Assert.Equal(future.UtcDateTime,(await db.LichSuGia.SingleAsync(p=>p.Id==result.Id)).HieuLucTu);
        Assert.Equal(future.UtcDateTime,ProductPriceRules.ResolveEffectiveAt(future.UtcDateTime.ToString("O"),DateTime.UtcNow));
    }

    private async Task<PromotionRecord> TestPromotion(long product)
    {
        await using var db=Db();
        return await new PostgreSqlPromotionDataSource(db,Http(1,"AD")).CreateAsync(new(
            0,"","Concurrency test",null,DiscountType.Percent,10,null,[new(product,"PG01","Product")],
            DateTimeOffset.UtcNow.AddDays(-1),DateTimeOffset.UtcNow.AddDays(1),false,PromotionStatus.Active,0),default);
    }

    [Fact]
    public async Task SM03_ParallelEdits_ExactlyOneCasWinsAndStaleStatusCannotOverwrite()
    {
        var product=await Product();var record=await TestPromotion(product);
        async Task<bool> Edit(string name) {await using var db=Db();return await new PostgreSqlPromotionDataSource(db,Http()).UpdateAsync(record with{Name=name},default);}
        var results=await Task.WhenAll(Edit("Editor A"),Edit("Editor B"));
        Assert.Single(results,x=>x);
        await using var verify=Db();var source=new PostgreSqlPromotionDataSource(verify,Http());
        var saved=(await source.GetByIdAsync(record.Id,default))!;
        Assert.Equal(2,saved.Version);
        var service=new PromotionService(source,new PostgreSqlProductLookup(verify),TimeProvider.System);
        var status=await service.ChangeStatusAsync(record.Id,PromotionTargetStatus.Paused,default,record.Version);
        Assert.Equal("PROMOTION_CONFLICT",status.Error!.Code);
        Assert.Equal(saved,(await source.GetByIdAsync(record.Id,default))! with {Products=saved.Products});
    }

    [Fact]
    public async Task SM03_CheckoutUsageCommittedWhileEditorWaits_BlocksRestrictedEdit()
    {
        var product=await Product();var record=await TestPromotion(product);
        await using var checkout=Db();await using var transaction=await checkout.Database.BeginTransactionAsync();
        await checkout.DocMotGiaTriAsync("SELECT id FROM chuong_trinh_khuyen_mai WHERE id=@id FOR SHARE",default,("@id",record.Id));
        await using var editor=Db();
        var pending=new PostgreSqlPromotionDataSource(editor,Http()).UpdateAsync(record with{DiscountValue=20},default);
        var deadline=DateTime.UtcNow.AddSeconds(8);bool waiting=false;
        while(DateTime.UtcNow<deadline)
        {
            await using var observer=Db();
            waiting=Convert.ToInt32(await observer.DocMotGiaTriAsync("SELECT COUNT(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock'",default))>0;
            if(waiting)break;
            await Task.Delay(25);
        }
        Assert.True(waiting,"Editor must wait for the checkout promotion lock.");
        var cart=await checkout.InsertIdAsync("INSERT INTO gio_hang(thu_ngan_id,trang_thai) VALUES(2,'DA_HOAN_TAT')",default);
        var invoice=await checkout.InsertIdAsync("INSERT INTO hoa_don(ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,tong_tien_hang,tong_giam,tong_thanh_toan,tien_khach_dua,tien_thua) VALUES('HD-RACE',@cart,2,NOW(),20000,2000,18000,18000,0)",default,("@cart",cart));
        await checkout.ChayAsync("INSERT INTO ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,giam_don_vi,gia_von,khuyen_mai_id) VALUES(@invoice,@product,'Snapshot','Hop',1,20000,2000,5000,@promotion)",default,("@invoice",invoice),("@product",product),("@promotion",record.Id));
        await transaction.CommitAsync();
        Assert.False(await pending);
        await using var verify=Db();var saved=(await new PostgreSqlPromotionDataSource(verify,Http()).GetByIdAsync(record.Id,default))!;
        Assert.Equal(10,saved.DiscountValue);Assert.Equal(1,saved.Version);Assert.Equal(1,saved.UsageCount);
    }

    [Fact]
    public async Task SM03_ProductLinkFailure_RollsBackHeadVersionAndLinks()
    {
        var product=await Product();var record=await TestPromotion(product);
        await using var db=Db();var source=new PostgreSqlPromotionDataSource(db,Http());
        await Assert.ThrowsAsync<PostgresException>(()=>source.UpdateAsync(record with{Name="Must rollback",Products=[new(long.MaxValue,"bad","Missing")]},default));
        var saved=(await source.GetByIdAsync(record.Id,default))!;
        Assert.Equal(record.Name,saved.Name);Assert.Equal(1,saved.Version);Assert.Equal(product,Assert.Single(saved.Products).Id);
    }

    [Fact]
    public async Task SM06_BigintIdsAndPointBalance_SurviveCartCheckoutReceiptAndCancellation()
    {
        const long product=5000000001,customer=5000000002,balance=5000000003;
        await using var db=Db();
        await db.ChayAsync("""
            INSERT INTO danh_muc(id,ten) VALUES(5000000000,'Bigint category');
            INSERT INTO san_pham(id,ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id) VALUES(5000000001,'BIGINT','Bigint product',5000000000,'Hop',1);
            INSERT INTO ton_kho(san_pham_id,so_luong_ton,gia_von_binh_quan) VALUES(5000000001,20,5000);
            INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id) VALUES(5000000001,20000,NOW()-INTERVAL '1 day',1);
            INSERT INTO khach_hang(id,ho_ten,so_dien_thoai,diem_hien_co,nguoi_tao_id) VALUES(5000000002,'Bigint customer','0901234567',5000000003,1);
            SELECT setval('smartmart.gio_hang_id_seq',5000000010,false);
            SELECT setval('smartmart.hoa_don_id_seq',5000000020,false);
            """,default);
        var catalog=new PostgreSqlPosCatalog(Configuration);
        Assert.Equal(product,(await catalog.FindAsync(product))!.Id);
        Assert.Equal(product,Assert.Single(await catalog.SearchAsync(null,5000000000)).Id);
        var cart=Cart();Assert.Null(await cart.AddAsync(product));Assert.Null(await cart.SetCustomerAsync(customer));
        Assert.Equal(balance,(await cart.GetCartAsync()).CustomerPoints);
        var logger=new RecordingLogger<PosCheckoutService>();
        var checkout=new PosCheckoutService(db,new PostgreSqlPosCheckoutStore(db,Stock(db),new ConvenienceStore.Web.Modules.Returns.Services.CashLedgerService(),Microsoft.Extensions.Logging.Abstractions.NullLogger<PostgreSqlPosCheckoutStore>.Instance),logger);
        var result=await checkout.CompleteAsync(20000,User(2,"TN"));
        Assert.True(result.Success,result.ErrorMessage+" "+logger.Error);Assert.True(result.InvoiceId>uint.MaxValue);
        var invoice=await new PostgreSqlPosInvoiceStore(db,Microsoft.Extensions.Logging.Abstractions.NullLogger<PostgreSqlPosInvoiceStore>.Instance).GetByIdAsync(result.InvoiceId!.Value,default);
        Assert.NotNull(invoice);
        var cancelled=await new PosInvoiceCancellationService(db,Stock(db),TimeProvider.System,new ConvenienceStore.Web.Modules.Returns.Services.CashLedgerService()).CancelAsync(result.InvoiceId.Value,"Bigint regression",User(1,"AD"));
        Assert.Equal(result.InvoiceId.Value,cancelled.InvoiceId);
        Assert.Equal(balance,Convert.ToInt64(await db.DocMotGiaTriAsync("SELECT diem_hien_co FROM khach_hang WHERE id=@id",default,("@id",customer))));
    }

    [Theory]
    [InlineData("2026-11-23T16:59:59Z","20261123")]
    [InlineData("2026-11-23T17:00:00Z","20261124")]
    [InlineData("2026-11-24T00:00:00+07:00","20261124")]
    public async Task SM07_InvoiceCounterUsesVietnamDateAtUtcBoundary(string instant,string expected)
    {
        await using var db=Db();
        var timestamp=Convert.ToDateTime(await db.DocMotGiaTriAsync("SELECT CAST(@at AS timestamptz)",default,("@at",DateTimeOffset.Parse(instant,CultureInfo.InvariantCulture))));
        Assert.Equal(expected,ConvenienceStore.Web.Shared.Api.GioVietNam.MaNgayChungTu(timestamp));
    }

    [Fact]
    public async Task SM03_EfUpdatesAlsoUseVersionAndStaleDeleteIsRejected()
    {
        var product=await Product();var record=await TestPromotion(product);
        await using var first=Db();await using var second=Db();
        var a=await first.ChuongTrinhKhuyenMais.SingleAsync(x=>x.Id==record.Id);
        var b=await second.ChuongTrinhKhuyenMais.SingleAsync(x=>x.Id==record.Id);
        a.Ten="EF winner";b.Ten="EF stale";
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>second.SaveChangesAsync());
        Assert.Equal(2,a.PhienBan);
        await using var verify=Db();
        var error=await Assert.ThrowsAsync<LoiNghiepVu>(()=>new PromotionUsageStore(verify).DeleteAsync(record.Id,default,record.Version));
        Assert.Equal("PROMOTION_CONFLICT",error.Ma);
        Assert.Equal("EF winner",(await verify.ChuongTrinhKhuyenMais.SingleAsync()).Ten);
    }

    private sealed class RecordingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public string? Error {get;private set;}
        public IDisposable? BeginScope<TState>(TState state) where TState:notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel level)=>true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel level,Microsoft.Extensions.Logging.EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
        { if(exception is not null) Error=exception.ToString(); }
    }
}

using System.Security.Claims;
using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Dtos;
using ConvenienceStore.Web.Modules.Auth.Services;
using ConvenienceStore.Web.Modules.Customers.Models;
using ConvenienceStore.Web.Modules.Customers.Services;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Modules.Purchasing;
using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Modules.Reports.Models;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace ConvenienceStore.Web.Tests.PostgreSql;

[Trait("Database", "PostgreSql")]
public sealed class PostgreSqlRuntimeTests : IAsyncLifetime
{
    private string _server = "";
    private string _connection = "";
    private Process? _web;
    private Task<string>? _webOutput;
    private Task<string>? _webError;
    private readonly string _database = "smartmart_pgtest_" + Guid.NewGuid().ToString("N");
    private const string Password = "RuntimeTest123!";
    private static ClaimsPrincipal User(int id, string role) => new(new ClaimsIdentity([
        new(ClaimTypes.NameIdentifier,id.ToString()),new(ClaimTypes.Role,role),
        ..ConvenienceStore.Web.Modules.Auth.Security.BangQuyen.LayQuyen(role).Select(p=>new Claim("quyen",p))],"test"));
    private sealed class RequestHttp : IHttpContextAccessor { public HttpContext? HttpContext { get; set; } }
    private static IHttpContextAccessor Http(int id = 2, string role = "TN") =>
        new RequestHttp { HttpContext = new DefaultHttpContext { User = User(id,role) } };
    private IConfiguration Configuration => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["ConnectionStrings:CuaHang"]=_connection,["Database:Provider"]="PostgreSql" }).Build();
    private CuaHangDbContext Db() => new(new DbContextOptionsBuilder<CuaHangDbContext>()
        .UseNpgsql(_connection,p=>p.SetPostgresVersion(17,0)).Options);
    private static DichVuKho Stock(CuaHangDbContext db) => new(db,NullLogger<DichVuKho>.Instance);
    private PosCartService Cart() => new(Http(),new MySqlPosCatalog(Configuration));

    public async Task InitializeAsync()
    {
        var value=Environment.GetEnvironmentVariable("ConnectionStrings__CuaHangPostgreSqlTest");
        if(string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Thiếu ConnectionStrings__CuaHangPostgreSqlTest; chỉ dùng PostgreSQL test localhost.");
        var settings=new NpgsqlConnectionStringBuilder(value) { Database="postgres",Pooling=false };
        if(settings.Host is not ("127.0.0.1" or "localhost" or "::1")) throw new InvalidOperationException("Refuse test database creation outside loopback.");
        _server=settings.ConnectionString;
        await using(var server=new NpgsqlConnection(_server)) {
            await server.OpenAsync();
            await using var command=new NpgsqlCommand("CREATE DATABASE \""+_database+"\"",server);
            await command.ExecuteNonQueryAsync();
        }
        settings.Database=_database;settings.SearchPath="smartmart";
        _connection=settings.ConnectionString;
        await using var db=Db();
        await db.ChayAsync(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"SchemaReference","schema_postgresql.sql")),default);
        await db.ChayAsync("""
            INSERT INTO vai_tro(id,ma,ten) VALUES (1,'AD','Admin'),(2,'TN','Thu ngân'),(3,'NK','Kho');
            INSERT INTO tai_khoan(id,ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
              VALUES (1,'pg_admin',@hash,'Admin test',1,FALSE),(2,'pg_cashier',@hash,'Cashier test',2,FALSE);
            SELECT setval('smartmart.tai_khoan_id_seq',100);
            INSERT INTO quy_tac_tich_diem(so_tien_mot_diem,dang_bat,hieu_luc_tu) VALUES(10000,TRUE,NOW()-INTERVAL '1 day');
            INSERT INTO chinh_sach_doi_tra(so_ngay_doi_tra,nguong_tien_can_duyet,hieu_luc_tu) VALUES(7,500000,NOW()-INTERVAL '1 day');
            INSERT INTO quy_tien_mat(id,so_du,da_chot_so_du_dau) VALUES(1,1000000,TRUE);
            """,default,("@hash",KhoiTaoAdminDauTien.BoBamMatKhau.HashPassword(new object(),Password)));
    }
    public async Task DisposeAsync()
    {
        if(_web is not null)
        {
            if(!_web.HasExited)_web.Kill(entireProcessTree:true);
            await _web.WaitForExitAsync();
            if(_webOutput is not null)await _webOutput;
            if(_webError is not null)await _webError;
            _web.Dispose();
        }
        if(string.IsNullOrEmpty(_server))return;
        await using var server=new NpgsqlConnection(_server);await server.OpenAsync();
        await using var command=new NpgsqlCommand("DROP DATABASE IF EXISTS \""+_database+"\" WITH (FORCE)",server);
        await command.ExecuteNonQueryAsync();
    }
    private async Task<int> Product(int stock = 20)
    {
        await using var db=Db();
        var category=await db.InsertIdAsync("INSERT INTO danh_muc(ten) VALUES ('Đồ uống')",default);
        var id=await db.InsertIdAsync("INSERT INTO san_pham(ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id) VALUES('PG01','Sữa tươi',@category,'Hộp',1)",default,("@category",category));
        await db.ChayAsync("INSERT INTO ton_kho(san_pham_id,so_luong_ton,gia_von_binh_quan) VALUES(@id,@quantity,5000)",default,("@id",id),("@quantity",stock));
        await db.ChayAsync("INSERT INTO lich_su_gia(san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id) VALUES(@id,20000,NOW()-INTERVAL '1 day',1)",default,("@id",id));
        return checked((int)id);
    }
    private async Task<long> Sale(int quantity = 2, bool customer = true)
    {
        var product=await Product();
        var cart=Cart();
        Assert.Null(await cart.AddAsync(product));
        Assert.Null(await cart.UpdateAsync(product,quantity));
        await using var db=Db();
        if(customer){var id=await new CustomerService(db).SaveAsync(null,new CustomerInput {Name="Khách PG",Phone="0901234567"},1,default);Assert.Null(await cart.SetCustomerAsync((int)id));}
        var checkout=new PosCheckoutService(db,new MySqlPosCheckoutStore(db,Stock(db),new CashLedgerService(),NullLogger<MySqlPosCheckoutStore>.Instance),NullLogger<PosCheckoutService>.Instance);
        var result=await checkout.CompleteAsync(100000,User(2,"TN"));
        Assert.True(result.Success,result.ErrorMessage);
        return result.InvoiceId!.Value;
    }

    [Fact]
    public async Task Search_PreservesVietnameseAccentAndCaseInsensitiveContract()
    {
        await Product();await using var db=Db();
        var products=await new ConvenienceStore.Web.Modules.Products.Services.EfProductReadRepository(db)
            .ListAsync(new(){Keyword="SUA TUOI"},false);
        Assert.Single(products.Items);
        Assert.Single(await new MySqlPosCatalog(Configuration).SearchAsync("sua TUOI"));
        Assert.Single((await new MySqlProductLookup(db).SearchAsync("SUA",1,10,default)).Items);
        Assert.Single((await new MySqlProductLookup(db).SearchAsync(null,1,10,default)).Items);
        var customers=new CustomerService(db);
        await customers.SaveAsync(null,new(){Name="Nguyễn Tiến Trung",Phone="0901234567"},1,default);
        Assert.Single(await customers.SearchAsync("NGUYEN TIEN",true,default));
    }

    [Fact]
    public async Task Reports_UseVietnamMidnightForUtcPaidAt()
    {
        var product=await Product();await using var db=Db();
        var times=new[]{new DateTime(2026,10,31,16,59,59,DateTimeKind.Utc),new DateTime(2026,10,31,17,0,0,DateTimeKind.Utc)};
        for(var i=0;i<times.Length;i++)
        {
            var cart=await db.InsertIdAsync("INSERT INTO gio_hang(thu_ngan_id,trang_thai) VALUES(2,'DA_HOAN_TAT')",default);
            var invoice=await db.InsertIdAsync("""
                INSERT INTO hoa_don(ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,tong_tien_hang,tong_giam,tong_thanh_toan,tien_khach_dua,tien_thua)
                VALUES(@code,@cart,2,@paid,20000,0,20000,20000,0)
                """,default,("@code","HD-PG-"+i),("@cart",cart),("@paid",times[i]));
            await db.ChayAsync("""
                INSERT INTO ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,giam_don_vi,gia_von)
                VALUES(@invoice,@product,'Sữa tươi','Hộp',1,20000,0,5000)
                """,default,("@invoice",invoice),("@product",product));
        }
        var service=new OperationalReportsService(new MySqlTransactionalReportsDataSource(db),TimeProvider.System);
        var report=await service.GetRevenueAsync(new(){Period="custom",From=new(2026,11,1),To=new(2026,11,1)},default);
        Assert.Equal(1,report.Metrics[0].Value);Assert.Equal(20000,report.Metrics[3].Value);
        var facts=await new MySqlDashboardDataSource(db).GetPaidInvoicesAsync(new DateTimeOffset(2026,11,1,0,0,0,TimeSpan.FromHours(7)),new DateTimeOffset(2026,11,2,0,0,0,TimeSpan.FromHours(7)),default);
        Assert.Single(facts);
    }

    [Fact]
    public async Task Session_ExplicitExpiryRevokesEvenWhenIdleTimeIsFresh()
    {
        await using var db=Db();var auth=new DichVuXacThuc(db);
        var login=await auth.DangNhapAsync(new("pg_cashier",Password));
        await db.ChayAsync("UPDATE phien_dang_nhap SET ngay_tao=NOW()-INTERVAL '1 hour',het_han_luc=NOW()-INTERVAL '1 second'",default);
        Assert.Null((await auth.XacThucPhienAsync(login.TokenPhien)).NguoiDung);
        Assert.NotNull((await db.PhienDangNhaps.SingleAsync()).HuyLuc);
    }

    [Fact]
    public async Task Cart_ConcurrentFirstRequestsCreateOneActiveCart()
    {
        await Product();
        await Task.WhenAll(Enumerable.Range(0,4).Select(_=>Cart().GetCartAsync()));
        await using var db=Db();
        Assert.Equal(1,await db.GioHangs.CountAsync(g=>g.ThuNganId==2 && g.TrangThai=="DANG_BAN"));
    }

    [Fact]
    public async Task HeldOrder_HoldRecallAndCancelKeepStockUnchanged()
    {
        var product=await Product();var cart=Cart();Assert.Null(await cart.AddAsync(product));
        var service=new PosHeldOrderService(new MySqlPosCatalog(Configuration),Http());
        Assert.True((await service.HoldAsync("Khách quay lại")).Success);
        await service.ListAsync();
        await using var db=Db();var held=await db.GioHangs.Where(g=>g.TrangThai=="TAM_GIU").Select(g=>g.Id).SingleAsync();
        Assert.True((await service.RecallAsync((int)held)).Success);
        await cart.CancelAsync();
        Assert.Equal(20,(await db.TonKhos.SingleAsync()).SoLuongTon);
        Assert.Equal("DA_HUY",(await db.GioHangs.SingleAsync(g=>g.Id==held)).TrangThai);
    }

    [Fact]
    public async Task CountAndDisposal_ChangeStockOnlyOnApproval()
    {
        var product=await Product(10);await using var db=Db();
        var controller=new ConvenienceStore.Web.Modules.Inventory.Controllers.StockDocumentsController(db,Stock(db),TimeProvider.System)
        { ControllerContext=new Microsoft.AspNetCore.Mvc.ControllerContext {HttpContext=Http(1,"AD").HttpContext!} };
        await controller.CreateCount("CHON_SAN_PHAM",null,[product],default);
        var count=await db.PhieuKiemKes.SingleAsync();
        await controller.SaveCount(count.Id,count.PhienBan,[new(product,8,null)],true,default);
        Assert.Equal(10,(await db.TonKhos.AsNoTracking().SingleAsync()).SoLuongTon);
        await controller.ReviewCount(count.Id,true,null,default);
        Assert.Equal(8,(await db.TonKhos.AsNoTracking().SingleAsync()).SoLuongTon);
        await controller.CreateDisposal([new(product,2,"HONG",null)],true,default);
        var disposal=await db.PhieuXuatHuys.SingleAsync();
        Assert.Equal(8,(await db.TonKhos.AsNoTracking().SingleAsync()).SoLuongTon);
        await controller.ReviewDisposal(disposal.Id,true,null,default);
        Assert.Equal(6,(await db.TonKhos.AsNoTracking().SingleAsync()).SoLuongTon);
    }

    [Fact]
    public async Task ConcurrentCheckout_LastItemSoldOnceAndCashLedgerRemainsConsistent()
    {
        var product=await Product(1);
        await using(var db=Db())await db.ChayAsync("INSERT INTO tai_khoan(id,ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk) VALUES(3,'other_cashier','unused','Thu ngân khác',2,FALSE)",default);
        Assert.Null(await Cart().AddAsync(product));
        var other=new PosCartService(Http(3),new MySqlPosCatalog(Configuration));Assert.Null(await other.AddAsync(product));
        async Task<PosCheckoutResult> Pay(int actor)
        {
            await using var db=Db();
            return await new PosCheckoutService(db,new MySqlPosCheckoutStore(db,Stock(db),new CashLedgerService(),NullLogger<MySqlPosCheckoutStore>.Instance),NullLogger<PosCheckoutService>.Instance).CompleteAsync(20000,User(actor,"TN"));
        }
        var results=await Task.WhenAll(Pay(2),Pay(3));Assert.Single(results,r=>r.Success);
        await using var verify=Db();Assert.Equal(0,(await verify.TonKhos.SingleAsync()).SoLuongTon);
        Assert.Equal(1,await verify.HoaDons.CountAsync());Assert.Equal(1020000,(await verify.QuyTienMats.SingleAsync()).SoDu);
    }

    private async Task<Uri> StartWeb()
    {
        var listener=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);
        listener.Start();var port=((System.Net.IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
        var address=new Uri($"http://127.0.0.1:{port}");
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../src/ConvenienceStore.Web"));
        var start=new ProcessStartInfo("dotnet") {WorkingDirectory=root,UseShellExecute=false,
            CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
        var configuration=AppContext.BaseDirectory.Contains(Path.DirectorySeparatorChar+"Release"+Path.DirectorySeparatorChar)?"Release":"Debug";
        start.ArgumentList.Add(Path.Combine(root,"bin",configuration,"net10.0","ConvenienceStore.Web.dll"));
        start.Environment["ASPNETCORE_URLS"]=address.ToString();
        start.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
        start.Environment["DOTNET_ENVIRONMENT"]="Development";
        start.Environment["ConnectionStrings__CuaHang"]=_connection;
        start.Environment["Database__Provider"]="PostgreSql";
        start.Environment["AdminDauTien__TenDangNhap"]="";
        start.Environment["AdminDauTien__MatKhauTam"]="";
        _web=Process.Start(start)!;_webOutput=_web.StandardOutput.ReadToEndAsync();_webError=_web.StandardError.ReadToEndAsync();
        using var probe=new HttpClient {Timeout=TimeSpan.FromSeconds(1)};
        for(var i=0;i<100;i++)
        {
            if(_web.HasExited)throw new InvalidOperationException("Production app exited before HTTP test.");
            try{var response=await probe.GetAsync(new Uri(address,"/login.html"));if(response.IsSuccessStatusCode)return address;}
            catch(HttpRequestException){}catch(TaskCanceledException){}
            await Task.Delay(150);
        }
        throw new TimeoutException("Production app did not listen for PostgreSQL HTTP test.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("AD")]
    [InlineData("TN")]
    public async Task ProductionHttp_LoginPermissionsPagesAndReports(string? role)
    {
        await Product();var address=await StartWeb();
        var cookies=new CookieContainer();
        using var client=new HttpClient(new HttpClientHandler {CookieContainer=cookies,AllowAutoRedirect=false}) {BaseAddress=address};
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/login.html")).StatusCode);
        if(role is null)
        {
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/reports/revenue")).StatusCode);
            return;
        }
        var login=await client.PostAsJsonAsync("/api/v1/auth/login",new {tenDangNhap=role=="AD"?"pg_admin":"pg_cashier",matKhau=Password});
        Assert.True(login.IsSuccessStatusCode,await login.Content.ReadAsStringAsync());
        foreach(var path in new[]{"/Products","/Customers"})
        {
            var page=await client.GetAsync(path);
            Assert.True(page.IsSuccessStatusCode,path+": "+await page.Content.ReadAsStringAsync());
        }
        Assert.Equal(role=="TN"?HttpStatusCode.OK:HttpStatusCode.Forbidden,(await client.GetAsync("/Pos")).StatusCode);
        foreach(var path in new[]{"revenue","products","categories","cashiers","inventory","purchases","gross-profit","returns","customers","promotions"})
        {
            var response=await client.GetAsync("/api/reports/"+path);
            Assert.True(response.StatusCode==(role=="AD"?HttpStatusCode.OK:HttpStatusCode.Forbidden),path+": "+await response.Content.ReadAsStringAsync());
        }
        if(role=="AD")
        {
            foreach(var path in new[]{"/Dashboard","/Kho","/Kho/ChungTu","/NhaCungCap","/NhapHang","/Reports/Reports/Dashboard"})
            {
                var response=await client.GetAsync(path);
                Assert.True(response.IsSuccessStatusCode,path+": "+await response.Content.ReadAsStringAsync());
            }
            var body=new {ma="PG_HTTP",tenSanPham="Tạo qua API",danhMucId=1,donViTinh="Hộp",giaBan=10000,nguongToiThieu=5};
            Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/products",body)).StatusCode);
            var xsrf=cookies.GetCookies(address)["XSRF-TOKEN"]!.Value;
            client.DefaultRequestHeaders.Add("X-XSRF-TOKEN",Uri.UnescapeDataString(xsrf));
            var create=await client.PostAsJsonAsync("/api/v1/products",body);
            Assert.True(create.StatusCode==HttpStatusCode.Created,await create.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Auth_CreateLoginChangePasswordLogout_AndLockAfterFiveFailures()
    {
        await using var db=Db();
        var account=await new DichVuTaiKhoan(db).TaoAsync(new("new_cashier","Nhân viên mới",null,null,Password,"TN"));
        var auth=new DichVuXacThuc(db);
        var login=await auth.DangNhapAsync(new("new_cashier",Password));
        await auth.DoiMatKhauAsync((int)account.Id,new(null,"Changed123!","Changed123!"));
        var active=await auth.XacThucPhienAsync(login.TokenPhien);
        Assert.NotNull(active.NguoiDung);
        await auth.DangXuatAsync(active.NguoiDung!.PhienId);
        Assert.Null((await auth.XacThucPhienAsync(login.TokenPhien)).NguoiDung);
        for(var i=0;i<5;i++) await Assert.ThrowsAsync<LoiNghiepVu>(()=>auth.DangNhapAsync(new("new_cashier","wrong")));
        var locked=await Assert.ThrowsAsync<LoiNghiepVu>(()=>auth.DangNhapAsync(new("new_cashier","Changed123!")));
        Assert.Equal(423,locked.MaHttp);
        await new DichVuTaiKhoan(db).DatLaiMatKhauAsync(account.Id,new("ResetAgain123!"));
        var resetLogin=await auth.DangNhapAsync(new("new_cashier","ResetAgain123!"));
        Assert.NotNull((await auth.XacThucPhienAsync(resetLogin.TokenPhien)).NguoiDung);
    }

    [Fact]
    public async Task Customers_CreateEditDuplicateAdjustAndDelete_PreservesPointHistory()
    {
        await using var db=Db();var customers=new CustomerService(db);
        var id=await customers.SaveAsync(null,new(){Name="Khách PG",Phone="0901234567",BirthDate=new DateTime(2000,1,2)},1,default);
        Assert.Equal("090****567",Assert.Single(await customers.SearchAsync("0901234567",false,default)).Phone);
        await Assert.ThrowsAsync<LoiNghiepVu>(()=>customers.SaveAsync(null,new(){Name="Trùng",Phone="0901234567"},1,default));
        var input=await customers.GetInputAsync(id,default);input.Name="Đã sửa";await customers.SaveAsync(id,input,1,default);
        await customers.AdjustAsync(id,new(){Points=5,Reason="Bù điểm"},1,default);
        await Assert.ThrowsAsync<LoiNghiepVu>(()=>customers.AdjustAsync(id,new(){Points=-6,Reason="Không âm"},1,default));
        Assert.Equal(5, (await customers.HistoryAsync(id,true,default)).Customer.Points);
        await customers.DeleteAsync(id,default);
        Assert.Equal("DA_AN",(await db.KhachHangs.AsNoTracking().SingleAsync(k=>k.Id==id)).TrangThai);
        Assert.Single(await db.LichSuDiems.Where(l=>l.KhachHangId==id).ToListAsync());
    }

    [Fact]
    public async Task Purchasing_ConfirmTwiceCannotDoubleStockAndUpdatesAverageCost()
    {
        var product=await Product(10);await using var db=Db();
        var supplier=await db.InsertIdAsync("INSERT INTO nha_cung_cap(ma,ten,so_dien_thoai) VALUES('NCCPG','Nhà cung cấp','0901234567')",default);
        var service=new PurchaseReceiptService(db,Stock(db),TimeProvider.System);
        var id=await service.CreateAsync(new(){SupplierId=supplier,Lines=[new(product,10,10000)]},1,default);
        await service.ConfirmAsync(id,1,default);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ConfirmAsync(id,1,default));
        var row=await db.TonKhos.AsNoTracking().SingleAsync(k=>k.SanPhamId==product);
        Assert.Equal(20,row.SoLuongTon);Assert.Equal(7500,row.GiaVonBinhQuan);
    }

    [Fact]
    public async Task Checkout_CommitsInvoiceStockPointsAndCash_AndPreventsDuplicateSale()
    {
        var invoice=await Sale();await using var db=Db();
        var stored=await db.HoaDons.AsNoTracking().SingleAsync(h=>h.Id==invoice);
        Assert.Equal(40000,stored.TongThanhToan);Assert.Equal(4,stored.DiemCong);
        Assert.Equal(18,(await db.TonKhos.SingleAsync()).SoLuongTon);
        Assert.Equal(4,(await db.KhachHangs.SingleAsync()).DiemHienCo);
        Assert.Equal(1040000,(await db.QuyTienMats.SingleAsync()).SoDu);
        Assert.Single(await db.GiaoDichTienMats.ToListAsync());
        Assert.Equal(40000,(await db.CtHoaDons.SingleAsync()).ThanhTien);
        await using var tx=await db.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<PosCheckoutException>(()=>new MySqlPosCheckoutStore(db,Stock(db),new CashLedgerService(),NullLogger<MySqlPosCheckoutStore>.Instance).SaveOrderAsync(2,100000,default));
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task Cancellation_RestoresStockPointsAndCash_AndRetainsInvoiceSnapshot()
    {
        var invoice=await Sale();await using var db=Db();
        await new PosInvoiceCancellationService(db,Stock(db),TimeProvider.System,new CashLedgerService()).CancelAsync(invoice,"Khách đổi ý",User(1,"AD"));
        Assert.Equal("DA_HUY",(await db.HoaDons.SingleAsync()).TrangThai);
        Assert.Equal(20,(await db.TonKhos.SingleAsync()).SoLuongTon);
        Assert.Equal(0,(await db.KhachHangs.SingleAsync()).DiemHienCo);
        Assert.Equal(1000000,(await db.QuyTienMats.SingleAsync()).SoDu);
        Assert.Single(await db.CtHoaDons.ToListAsync());
    }

    [Fact]
    public async Task Returns_RefundAndRestockRecoverPointsExactlyOnce()
    {
        var invoice=await Sale();await using var db=Db();
        var product=await db.CtHoaDons.Select(l=>l.SanPhamId).SingleAsync();
        var refunds=new ReturnRefundService(new CashLedgerService());
        var returns=new ReturnOrderService(db,Stock(db),new ReturnPointsGateway(),TimeProvider.System,refunds);
        var request=new ReturnOrderRequest {InvoiceId=invoice,CashConfirmed=true,Lines=[new(){ProductId=(int)product,Quantity=1,Reason="LOI_SAN_PHAM",Condition="Nguyên hộp",Handling="NHAP_LAI"}]};
        request.ConfirmationToken=(await returns.PreviewAsync(request,User(2,"TN"))).ConfirmationToken;
        var id=await returns.CreateAsync(request,User(2,"TN"));
        Assert.Equal(19,(await db.TonKhos.AsNoTracking().SingleAsync()).SoLuongTon);
        Assert.Equal(2,(await db.KhachHangs.AsNoTracking().SingleAsync()).DiemHienCo);
        Assert.Equal(1020000,(await db.QuyTienMats.AsNoTracking().SingleAsync()).SoDu);
        await using var tx=await db.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<LoiNghiepVu>(()=>refunds.RefundAsync(db,id,2));
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task Promotions_MemberSelectionAndReportsUseSamePaidSnapshot()
    {
        var product=await Product();await using var db=Db();
        var source=new MySqlPromotionDataSource(db,Http(1,"AD"));
        var now=DateTimeOffset.UtcNow;
        var promotion=await source.CreateAsync(new(0,"","Giảm thành viên",null,DiscountType.FixedAmount,2000,null,[new(product,"PG01","Sữa tươi")],now.AddHours(-1),now.AddDays(1),true,PromotionStatus.Active,0),default);
        var cart=Cart();Assert.Null(await cart.AddAsync(product));
        var customer=await new CustomerService(db).SaveAsync(null,new(){Name="Thành viên",Phone="0901234567"},1,default);
        Assert.Null(await cart.SetCustomerAsync((int)customer));
        var checkout=new PosCheckoutService(db,new MySqlPosCheckoutStore(db,Stock(db),new CashLedgerService(),NullLogger<MySqlPosCheckoutStore>.Instance),NullLogger<PosCheckoutService>.Instance);
        var sale=await checkout.CompleteAsync(20000,User(2,"TN"));Assert.True(sale.Success,sale.ErrorMessage);
        Assert.Equal(18000,sale.Total);
        var facts=new MySqlTransactionalReportsDataSource(db);
        var query=new ReportQuery {Period="custom",From=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(-1)),To=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(1))};
        var service=new OperationalReportsService(facts,TimeProvider.System);
        var revenue=await service.GetRevenueAsync(query,default);
        Assert.Equal(18000,revenue.Metrics[3].Value);
        await service.GetProductsAsync(query,default);await service.GetCategoriesAsync(query,default);
        await service.GetCashiersAsync(query,default);await service.GetInventoryAsync(query,default);
        await service.GetPurchasesAsync(query,default);await service.GetGrossProfitAsync(query,default);
        Assert.Single(await new PromotionUsageStore(db).ReadAsync(null,null,promotion.Id,default));
        var week=new WeekEightReportsService(new MySqlWeekEightReportDataSource(db,new PromotionUsageStore(db)),TimeProvider.System);
        foreach(var code in new[]{"BC-09","BC-10","BC-11"})await week.GetAsync(code,query,default);
    }
}

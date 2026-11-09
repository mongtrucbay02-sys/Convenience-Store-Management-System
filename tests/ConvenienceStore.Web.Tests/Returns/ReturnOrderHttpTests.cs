using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Returns;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConvenienceStore.Web.Tests.Returns;

[Collection(CsdlKhoCollection.Ten)]
public class ReturnOrderHttpTests(CsdlKhoFixture fixture)
{
    private static readonly DateTimeOffset Now=new(2026,11,5,15,0,0,TimeSpan.FromHours(7));
    private sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,
        UrlEncoder encoder,CsdlKhoFixture fixture):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string actor=Request.Headers["X-Test-Actor"].ToString();
            if(actor.Length==0)return Task.FromResult(AuthenticateResult.NoResult());
            var user=InvoiceLookupServiceTests.Cashier(actor is "FORCED" or "NO_PERMISSION"?"TN":actor,
                permission:actor!="NO_PERMISSION",mustChange:actor=="FORCED",id:fixture.TaiKhoanThuNganId.ToString());
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(user,Scheme.Name)));
        }
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)=>PhanHoiLoiApi.GhiAsync(Context,401,"CHUA_DANG_NHAP","Đăng nhập.",null);
        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)=>PhanHoiLoiApi.GhiAsync(Context,403,"KHONG_DU_QUYEN","Không đủ quyền.",null);
    }
    private async Task<WebApplication> Start()
    {
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions {
            ApplicationName=typeof(ReturnOrderService).Assembly.GetName().Name,
            ContentRootPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../src/ConvenienceStore.Web")) });
        builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Logging.ClearProviders();
        builder.Services.AddSingleton(fixture);builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddControllersWithViews(o=> { o.Filters.Add<KiemTraXsrfFilter>();o.Filters.Add<KiemTraDoiMatKhauFilter>(); }).AddApplicationPart(typeof(ReturnOrderService).Assembly);
        builder.Services.AddXuLyLoiApi();builder.Services.AddDbContext<CuaHangDbContext>(o=>o.UseMySQL(fixture.ChuoiKetNoi).AddInterceptors(new GioVietNamInterceptor()));
        builder.Services.AddModuleReturns();builder.Services.AddModuleInventory();
        builder.Services.AddSingleton<TimeProvider>(new InvoiceLookupServiceTests.FixedTime(Now));
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions,TestAuth>("test",_=>{});
        builder.Services.AddAuthorization(o=>o.AddPolicy(MaQuyen.DoiTra,p=>p.RequireAuthenticatedUser().RequireClaim(ThongTinXacThuc.ClaimQuyen,MaQuyen.DoiTra)));
        var app=builder.Build();app.UseXuLyLoiApi();app.UseRouting();app.UseAuthentication();app.UseAuthorization();
        app.MapControllers();app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");await app.StartAsync();return app;
    }
    private static HttpClient Client(WebApplication app,string? actor="TN",bool csrf=true)
    {
        var client=new HttpClient { BaseAddress=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        if(actor is not null)client.DefaultRequestHeaders.Add("X-Test-Actor",actor);
        if(csrf) { const string token="test-session-not-production";client.DefaultRequestHeaders.Add("Cookie","cs_session="+token);client.DefaultRequestHeaders.Add("X-XSRF-TOKEN",TokenPhien.TaoXsrf(token)); }
        return client;
    }
    private async Task<(long Invoice,int Product)> Seed()
    {
        int product=await new DuLieuTestKho(fixture).TaoSanPhamAsync("DT02HTTP");
        await fixture.ChayAsync("INSERT INTO chinh_sach_doi_tra (hieu_luc_tu) VALUES (@now)",("@now",Now.DateTime.AddHours(-1)));
        long cart=Convert.ToInt64(await fixture.DocMotGiaTriAsync("INSERT INTO gio_hang (thu_ngan_id,trang_thai,hoan_tat_luc) VALUES (@tn,'DA_HOAN_TAT',@now); SELECT LAST_INSERT_ID();",("@tn",fixture.TaiKhoanThuNganId),("@now",Now.DateTime.AddDays(-1))));
        long invoice=Convert.ToInt64(await fixture.DocMotGiaTriAsync("""
            INSERT INTO hoa_don (ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,tong_tien_hang,tong_giam,tong_thanh_toan,tien_khach_dua,tien_thua)
            VALUES (@ma,@g,@tn,@now,50000,0,50000,50000,0); SELECT LAST_INSERT_ID();
            """,("@ma","HD"+Guid.NewGuid().ToString("N")[..12]),("@g",cart),("@tn",fixture.TaiKhoanThuNganId),("@now",Now.DateTime.AddDays(-1))));
        await fixture.ChayAsync("""
            INSERT INTO ct_hoa_don (hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,gia_von)
            VALUES (@hd,@p,'Sữa <script>alert(1)</script>','Hộp',2,25000,8000)
            """,("@hd",invoice),("@p",product));
        return(invoice,product);
    }
    private static ReturnOrderRequest Request(long invoice,int product)=>new() {
        InvoiceId=invoice,CashConfirmed=true,Lines=[new() { ProductId=product,Quantity=1,Reason="KHAC",Note="Khách <script>",Condition="Nguyên bao bì",Handling="HUY" }] };
    [Theory]
    [InlineData(null,401)]
    [InlineData("AD",403)]
    [InlineData("NK",403)]
    [InlineData("NO_PERMISSION",403)]
    [InlineData("FORCED",403)]
    public async Task ApiRequiresAuthenticatedCashierPermission(string? actor,int expected)
    {
        await using var app=await Start();using var client=Client(app,actor);
        Assert.Equal(expected,(int)(await client.PostAsJsonAsync("/api/v1/returns/preview",new ReturnOrderRequest())).StatusCode);
    }
    [Fact]
    public async Task ApiRequiresXsrfAndReturnsSharedValidationEnvelope()
    {
        await using var app=await Start();using var noToken=Client(app,csrf:false);
        Assert.Equal(HttpStatusCode.Forbidden,(await noToken.PostAsJsonAsync("/api/v1/returns",new ReturnOrderRequest())).StatusCode);
        using var client=Client(app);var response=await client.PostAsJsonAsync("/api/v1/returns/preview",new ReturnOrderRequest());
        Assert.Equal(422,(int)response.StatusCode);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("code",out _));Assert.True(json.RootElement.TryGetProperty("traceId",out _));
        var malformed=await client.PostAsync("/api/v1/returns/preview",new StringContent("{",Encoding.UTF8,"application/json"));
        Assert.Equal(HttpStatusCode.BadRequest,malformed.StatusCode);
    }
    [Fact]
    public async Task ApiPreviewCreateAndReceipt_UsesServerAmountsAndNoStore()
    {
        var seed=await Seed();await using var app=await Start();using var client=Client(app);
        var request=Request(seed.Invoice,seed.Product);
        var previewResponse=await client.PostAsJsonAsync("/api/v1/returns/preview",request);previewResponse.EnsureSuccessStatusCode();
        var preview=(await previewResponse.Content.ReadFromJsonAsync<ReturnPreview>())!;Assert.Equal(25000,preview.Refund);
        Assert.DoesNotContain("cost",await previewResponse.Content.ReadAsStringAsync());
        request.ConfirmationToken=preview.ConfirmationToken;
        var created=await client.PostAsJsonAsync("/api/v1/returns",request);Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        Assert.NotNull(created.Headers.Location);var receipt=(await created.Content.ReadFromJsonAsync<ReturnReceipt>())!;
        Assert.Equal("HOAN_TAT",receipt.Status);Assert.Equal(TimeSpan.FromHours(7),receipt.CreatedAt.Offset);
        var get=await client.GetAsync(created.Headers.Location);Assert.Equal(HttpStatusCode.OK,get.StatusCode);Assert.True(get.Headers.CacheControl!.NoStore);
        var html=await client.GetStringAsync("/Returns/Receipts/"+receipt.Id);
        Assert.Contains("PHIẾU TRẢ HÀNG",WebUtility.HtmlDecode(html));Assert.Contains("Khách hàng ký",WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("<script>alert(1)</script>",html);
    }
    [Fact]
    public async Task MvcFormContainsAntiforgery_AndRejectsMissingToken()
    {
        var seed=await Seed();await using var app=await Start();using var client=Client(app);
        string html=await client.GetStringAsync("/Returns/New?invoiceId="+seed.Invoice);
        Assert.Contains("__RequestVerificationToken",html);Assert.Contains("/Returns/Preview",html);
        Assert.DoesNotContain("<script>alert(1)</script>",html);
        var post=await client.PostAsync("/Returns/Preview",new FormUrlEncodedContent(new Dictionary<string,string> { ["Request.InvoiceId"]=seed.Invoice.ToString() }));
        Assert.Equal(HttpStatusCode.BadRequest,post.StatusCode);
        var exchange=await client.GetStringAsync("/Returns/Exchange?invoiceId="+seed.Invoice);
        Assert.Contains("DT-03",WebUtility.HtmlDecode(exchange));
    }
}

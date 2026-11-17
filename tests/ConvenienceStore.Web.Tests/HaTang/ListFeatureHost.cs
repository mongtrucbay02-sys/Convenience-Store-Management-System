using ConvenienceStore.Web.Modules.Auth;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConvenienceStore.Web.Tests.HaTang;

// Real production session authentication; only domain dependencies may be replaced.
public static class ListFeatureHost
{
    public static async Task<WebApplication> Start(CsdlMySqlTam database, Action<IServiceCollection> register)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ApplicationName = typeof(ConvenienceStore.Web.Data.CuaHangDbContext).Assembly.GetName().Name,
            ContentRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/ConvenienceStore.Web"))
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddControllersWithViews();
        builder.Services.AddScoped(_ => database.TaoDbContext());
        builder.Services.AddModuleAuth();
        builder.Services.AddXuLyLoiApi();
        register(builder.Services);
        var app = builder.Build();
        app.UseXuLyLoiApi();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        // Match Program.cs, including the legacy module landing route.
        app.MapControllerRoute("areas", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync();
        return app;
    }

    public static async Task<HttpClient> Client(WebApplication app, CsdlMySqlTam database, string? actor)
    {
        var client = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) {
            BaseAddress = new Uri(app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single())
        };
        if (actor is null) return client;
        var token = TokenPhien.TaoToken();
        var name = "feature_" + Guid.NewGuid().ToString("N");
        var role = actor is "TN" or "NK" ? actor : "AD";
        await database.ChayAsync("""
            INSERT INTO tai_khoan (ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk,trang_thai,ly_do_khoa)
            SELECT @name,'unused','HTTP feature',id,@forced,@status,
                IF(@status='DA_KHOA','Test lock',NULL) FROM vai_tro WHERE ma=@role;
            INSERT INTO phien_dang_nhap (tai_khoan_id,token_bam,hoat_dong_cuoi,huy_luc)
            SELECT id,@token,IF(@expired,NOW()-INTERVAL 1 HOUR,NOW()),IF(@revoked,NOW(),NULL)
            FROM tai_khoan WHERE ten_dang_nhap=@name;
            """, ("@name", name), ("@role", role), ("@forced", actor == "FORCED"),
            ("@status", actor == "LOCKED" ? "DA_KHOA" : "HOAT_DONG"),
            ("@token", TokenPhien.Bam(token)), ("@expired", actor == "EXPIRED"), ("@revoked", actor == "REVOKED"));
        client.DefaultRequestHeaders.Add("Cookie", "cs_session=" + (actor == "FORGED" ? "forged" : token));
        // Untrusted role/identity headers must not grant permissions.
        client.DefaultRequestHeaders.Add("X-Role", "AD");
        return client;
    }
}

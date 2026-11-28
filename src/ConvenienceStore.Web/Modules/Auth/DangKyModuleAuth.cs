using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Auth;

public static class DangKyModuleAuth
{
    public static IServiceCollection AddModuleAuth(this IServiceCollection services)
    {
        services.AddScoped<IDichVuXacThuc, DichVuXacThuc>();
        services.AddScoped<IDichVuTaiKhoan, DichVuTaiKhoan>();

        services.AddAuthentication(ThongTinXacThuc.TenScheme)
            .AddScheme<AuthenticationSchemeOptions, XuLyXacThucPhien>(ThongTinXacThuc.TenScheme, null);

        services.AddAuthorization(tuyChon =>
        {
            foreach (var quyen in MaQuyen.TatCa)
                tuyChon.AddPolicy(quyen, chinhSach => chinhSach
                    .AddAuthenticationSchemes(ThongTinXacThuc.TenScheme)
                    .RequireAuthenticatedUser()
                    .RequireClaim(ThongTinXacThuc.ClaimQuyen, quyen));
        });

        services.Configure<MvcOptions>(tuyChon =>
        {
            tuyChon.Filters.Add<KiemTraXsrfFilter>();
            tuyChon.Filters.Add<KiemTraDoiMatKhauFilter>();
        });

        return services;
    }
}

using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Shared.Errors;

public static class CauHinhLoiApi
{
    public static IServiceCollection AddXuLyLoiApi(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(tuyChon =>
            tuyChon.InvalidModelStateResponseFactory = nguCanh =>
            {
                var chiTiet = nguCanh.ModelState
                    .Where(m => m.Value is { Errors.Count: > 0 } && m.Key.StartsWith("$.", StringComparison.Ordinal))
                    .Select(m => new LoiTruong(TenTruong(m.Key), "Giá trị gửi lên không đúng định dạng."))
                    .ToList();
                return PhanHoiLoiApi.TaoKetQua(
                    nguCanh.HttpContext,
                    StatusCodes.Status400BadRequest,
                    MaLoiChung.YeuCauKhongHopLe,
                    ThongBaoLoi.YeuCauKhongHopLe,
                    chiTiet.Count > 0 ? chiTiet : null);
            });
        return services;
    }

    public static IApplicationBuilder UseXuLyLoiApi(this IApplicationBuilder app) =>
        app.UseMiddleware<XuLyLoiApiMiddleware>();

    private static string TenTruong(string khoa)
    {
        var ten = khoa[2..];
        return ten.Length == 0 ? ten : char.ToLowerInvariant(ten[0]) + ten[1..];
    }
}

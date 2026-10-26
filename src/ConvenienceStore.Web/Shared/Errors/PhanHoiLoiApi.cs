using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Shared.Errors;

public static class PhanHoiLoiApi
{
    public static PhanHoiLoi Tao(HttpContext nguCanh, string ma, string thongBao, object? chiTiet) =>
        new(ma, thongBao, chiTiet, nguCanh.TraceIdentifier);

    public static ObjectResult TaoKetQua(HttpContext nguCanh, int maHttp, string ma, string thongBao, object? chiTiet) =>
        new(Tao(nguCanh, ma, thongBao, chiTiet)) { StatusCode = maHttp };

    public static async Task GhiAsync(HttpContext nguCanh, int maHttp, string ma, string thongBao, object? chiTiet)
    {
        nguCanh.Response.StatusCode = maHttp;
        nguCanh.Response.Headers.CacheControl = "no-store";
        await nguCanh.Response.WriteAsJsonAsync(Tao(nguCanh, ma, thongBao, chiTiet), nguCanh.RequestAborted);
    }
}

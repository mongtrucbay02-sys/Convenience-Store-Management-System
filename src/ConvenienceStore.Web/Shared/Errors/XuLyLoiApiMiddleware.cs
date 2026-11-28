namespace ConvenienceStore.Web.Shared.Errors;

public sealed class XuLyLoiApiMiddleware(RequestDelegate tiepTheo, ILogger<XuLyLoiApiMiddleware> log)
{
    public static bool LaDuongDanApi(PathString duongDan) => duongDan.StartsWithSegments("/api");

    public async Task InvokeAsync(HttpContext nguCanh)
    {
        if (!LaDuongDanApi(nguCanh.Request.Path))
        {
            await tiepTheo(nguCanh);
            return;
        }

        try
        {
            await tiepTheo(nguCanh);
        }
        catch (LoiNghiepVu loi) when (!nguCanh.Response.HasStarted)
        {
            if (loi.MaHttp >= StatusCodes.Status500InternalServerError)
                log.LogError(loi, "Lỗi {Ma} tại {DuongDan}", loi.Ma, nguCanh.Request.Path);
            await PhanHoiLoiApi.GhiAsync(nguCanh, loi.MaHttp, loi.Ma, loi.Message, loi.ChiTiet);
        }
        catch (Exception loi) when (!nguCanh.Response.HasStarted && !nguCanh.RequestAborted.IsCancellationRequested)
        {
            log.LogError(loi, "Lỗi không lường trước tại {DuongDan}", nguCanh.Request.Path);
            await PhanHoiLoiApi.GhiAsync(
                nguCanh, StatusCodes.Status500InternalServerError, MaLoiChung.LoiMayChu, ThongBaoLoi.LoiMayChu, null);
        }
    }
}

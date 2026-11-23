using System.Security.Claims;

namespace ConvenienceStore.Web.Services.Pos;

public interface ICurrentCashier
{
    string Id { get; }
}

public sealed class HttpContextCurrentCashier : ICurrentCashier
{
    private readonly IHttpContextAccessor _http;
    private readonly IWebHostEnvironment _env;

    public HttpContextCurrentCashier(
        IHttpContextAccessor http, IWebHostEnvironment env)
    {
        _http = http;
        _env = env;
    }

    public string Id
    {
        get
        {
            var id = _http.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(id))
                return id;

            // Tạm thời, khi module AUTH (#5) chưa xong: chỉ cho phép khi chạy Development.
            if (_env.IsDevelopment())
                return "demo-cashier";

            throw new InvalidOperationException(
                "Thu ngân chưa đăng nhập.");
        }
    }
}
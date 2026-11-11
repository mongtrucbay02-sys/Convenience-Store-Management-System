
using ConvenienceStore.Web.Modules.Returns.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConvenienceStore.Web.Modules.Returns;

public static class DangKyModuleReturns
{
    public static IServiceCollection AddModuleReturns(
        this IServiceCollection services)
    {
        // Đăng ký TimeProvider dùng chung
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        // Đăng ký dịch vụ tra cứu hóa đơn
        services.AddScoped<IInvoiceLookupStore, MySqlInvoiceLookupStore>();
        services.AddScoped<InvoiceLookupService>();

        // Đăng ký dịch vụ quản lý quỹ tiền mặt
        services.AddScoped<CashLedgerService>();

        // Đăng ký dịch vụ hoàn tiền
        services.AddScoped<ReturnRefundService>();

        // Đăng ký dịch vụ đổi trả hàng
        services.AddScoped<ReturnOrderService>();

        // Đăng ký cổng tích hợp thu hồi điểm KH-06
        services.TryAddScoped<IReturnPointsGateway, PendingKh06ReturnPointsGateway>();

        return services;
    }
}

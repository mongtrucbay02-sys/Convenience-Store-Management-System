using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Returns.Services;

/// <summary>KH-06 adapter must use this same DbContext transaction, and never commit itself.</summary>
public interface IReturnPointsGateway
{
    Task<int> RecoverAsync(CuaHangDbContext transactionContext, long invoiceId, long returnId,
        int cashierId, long refund, CancellationToken ct);
}

/// <summary>Fail closed until #5 supplies the agreed KH-06 shared function.</summary>
public sealed class PendingKh06ReturnPointsGateway : IReturnPointsGateway
{
    public Task<int> RecoverAsync(CuaHangDbContext transactionContext, long invoiceId, long returnId,
        int cashierId, long refund, CancellationToken ct) =>
        throw new LoiNghiepVu("CHUA_TICH_HOP_KH06", 409,
            "Chưa tích hợp hàm thu hồi điểm KH-06. Chưa hoàn tiền, chưa thay đổi tồn kho hoặc điểm.");
}

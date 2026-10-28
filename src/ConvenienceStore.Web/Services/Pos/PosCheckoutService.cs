
using System.Data;
using System.Security.Claims;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ConvenienceStore.Web.Services.Pos;

/// <summary>
/// Kết quả hoàn tất đơn hàng POS-07.
/// </summary>
public sealed class PosCheckoutResult
{
    public bool Success { get; init; }

    public long? InvoiceId { get; init; }

    public string? InvoiceCode { get; init; }

    public string? ErrorMessage { get; init; }

    public long Total { get; init; }

    public long CashReceived { get; init; }

    public long Change { get; init; }

    public int PointsEarned { get; init; }
}

/// <summary>
/// Kết quả lưu đơn hàng trước khi commit transaction.
/// </summary>
public sealed record PosOrderSaved(
    long InvoiceId,
    string InvoiceCode,
    long Total,
    long CashReceived,
    long Change,
    int PointsEarned);

/// <summary>
/// Lỗi nghiệp vụ POS-07.
/// </summary>
public sealed class PosCheckoutException(string message)
    : Exception(message);

/// <summary>
/// Store chịu trách nhiệm:
/// - Kiểm tra tồn kho
/// - Kiểm tra khuyến mãi
/// - Lưu hóa đơn và chi tiết
/// - Cập nhật tồn kho
/// - Tích điểm
///
/// Tất cả phải dùng transaction của DbContext.
/// </summary>
public interface IPosCheckoutStore
{
    Task<PosOrderSaved> SaveOrderAsync(
        int cashierId,
        long cashReceived,
        CancellationToken cancellationToken);
}

/// <summary>
/// POS-07: Hoàn tất đơn hàng.
/// Điều phối transaction và xử lý lỗi E1, E2, E3.
/// </summary>
public sealed class PosCheckoutService(
    CuaHangDbContext db,
    IPosCheckoutStore store,
    ILogger<PosCheckoutService> log)
{
    public async Task<PosCheckoutResult> CompleteAsync(
        long cashReceived,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        // ==========================================
        // 1. KIỂM TRA THU NGÂN
        // ==========================================

        if (user?.Identity?.IsAuthenticated != true)
        {
            return Failed("Thu ngân chưa đăng nhập.");
        }

        int cashierId;

        try
        {
            cashierId = user.LayTaiKhoanId();
        }
        catch (Exception ex)
            when (ex is InvalidOperationException
                   or FormatException)
        {
            return Failed(
                "Không xác định được tài khoản thu ngân.");
        }

        if (cashReceived < 0)
        {
            return Failed(
                "Số tiền khách đưa không hợp lệ.");
        }

        // ==========================================
        // 2. MỞ TRANSACTION
        // ==========================================

        IDbContextTransaction? transaction = null;

        try
        {
            transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(
                ex,
                "POS-07: Không thể mở transaction. " +
                "Thu ngân {CashierId}",
                cashierId);

            return Failed(
                "Không thể kết nối cơ sở dữ liệu. " +
                "Đơn hàng chưa được hoàn tất. " +
                "Vui lòng thử lại.");
        }

        await using (transaction)
        {
            PosOrderSaved saved;

            // ==========================================
            // 3. GHI ĐƠN HÀNG TRONG TRANSACTION
            // ==========================================

            try
            {
                saved = await store.SaveOrderAsync(
                    cashierId,
                    cashReceived,
                    cancellationToken);
            }

            // E1, E2: LỖI NGHIỆP VỤ
            catch (Exception ex)
                when (ex is PosCheckoutException
                      or LoiNghiepVu)
            {
                await RollbackAsync(transaction);

                log.LogWarning(
                    "POS-07: Không thể chốt đơn. " +
                    "Thu ngân {CashierId}. Lý do: {Reason}",
                    cashierId,
                    ex.Message);

                return Failed(ex.Message);
            }

            // YÊU CẦU BỊ HỦY
            catch (OperationCanceledException)
            {
                await RollbackAsync(transaction);
                throw;
            }

            // E3: LỖI GHI DỮ LIỆU
            catch (Exception ex)
            {
                log.LogError(
                    ex,
                    "POS-07: Lỗi ghi dữ liệu. " +
                    "Thu ngân {CashierId}",
                    cashierId);

                await RollbackAsync(transaction);

                return Failed(
                    "Có lỗi khi lưu hóa đơn. " +
                    "Giao dịch đã được yêu cầu hoàn tác. " +
                    "Đơn hàng chưa được xác nhận thành công. " +
                    "Vui lòng kiểm tra và thử lại.");
            }

            // ==========================================
            // 4. COMMIT TRANSACTION
            // ==========================================

            try
            {
                await transaction.CommitAsync(
                    cancellationToken);
            }
            catch (Exception ex)
            {
                log.LogError(
                    ex,
                    "POS-07: Không xác định được kết quả " +
                    "commit hóa đơn {InvoiceCode}",
                    saved.InvoiceCode);

                // Không tự động thanh toán lại:
                // Commit có thể đã thành công trên MySQL.
                return Failed(
                    "Không xác định được kết quả lưu hóa đơn. " +
                    "Hãy kiểm tra danh sách hóa đơn " +
                    "trước khi thực hiện thanh toán lại.");
            }

            // ==========================================
            // 5. HOÀN TẤT THÀNH CÔNG
            // ==========================================

            log.LogInformation(
                "POS-07: Hoàn tất hóa đơn {InvoiceCode}. " +
                "ID: {InvoiceId}. Thu ngân: {CashierId}",
                saved.InvoiceCode,
                saved.InvoiceId,
                cashierId);

            return new PosCheckoutResult
            {
                Success = true,
                InvoiceId = saved.InvoiceId,
                InvoiceCode = saved.InvoiceCode,
                Total = saved.Total,
                CashReceived = saved.CashReceived,
                Change = saved.Change,
                PointsEarned = saved.PointsEarned
            };
        }
    }

    // ==========================================
    // ROLLBACK AN TOÀN
    // ==========================================

    private async Task RollbackAsync(
        IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            log.LogWarning(
                ex,
                "POS-07: Không thể xác nhận rollback. " +
                "Cần kiểm tra trạng thái giao dịch.");
        }
    }

    // ==========================================
    // KẾT QUẢ THẤT BẠI
    // ==========================================

    private static PosCheckoutResult Failed(
        string message)
    {
        return new PosCheckoutResult
        {
            Success = false,
            ErrorMessage = message
        };
    }
}

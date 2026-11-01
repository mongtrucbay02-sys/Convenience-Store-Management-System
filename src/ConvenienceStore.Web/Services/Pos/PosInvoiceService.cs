
using ConvenienceStore.Web.Models.Pos;
using Microsoft.Extensions.Logging;

namespace ConvenienceStore.Web.Services.Pos;

// Interface đọc hóa đơn từ CSDL.
// Lớp triển khai phải sử dụng đúng schema
// hóa đơn và thanh toán của nhóm.
public interface IPosInvoiceStore
{
    Task<PosInvoiceViewModel?> GetByIdAsync(
        long invoiceId,
        CancellationToken cancellationToken = default);
}

public sealed class PosInvoiceService
{
    private readonly IPosInvoiceStore _invoiceStore;
    private readonly ILogger<PosInvoiceService> _logger;

    public PosInvoiceService(
        IPosInvoiceStore invoiceStore,
        ILogger<PosInvoiceService> logger)
    {
        _invoiceStore = invoiceStore;
        _logger = logger;
    }

    // ==========================================
    // POS-07: LẤY HÓA ĐƠN THEO ID
    // ==========================================

    public async Task<PosInvoiceViewModel?> GetByIdAsync(
        long invoiceId,
        CancellationToken cancellationToken = default)
    {
        if (invoiceId <= 0)
        {
            return null;
        }

        var invoice = await _invoiceStore.GetByIdAsync(
            invoiceId,
            cancellationToken);

        if (invoice == null)
        {
            return null;
        }

        // Chỉ cho phép in hóa đơn đã thanh toán.
        if (!string.Equals(
                invoice.Status,
                "DA_THANH_TOAN",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Hoa don {InvoiceId} chua duoc thanh toan.",
                invoiceId);

            return null;
        }

        return invoice;
    }

    // POS-11: quyền xem của TN/AD áp dụng cho cả xem chi tiết lẫn dữ liệu in lại.
    public async Task<PosInvoiceViewModel?> GetDetailsAsync(long invoiceId,
        PosInvoiceViewer viewer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (!viewer.CanView) throw new UnauthorizedAccessException("Không có quyền xem hóa đơn.");
        if (invoiceId <= 0) return null;
        var invoice = await _invoiceStore.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null) return null;
        if (!PosInvoiceAccess.CanView(viewer, invoice.CashierId))
            throw new UnauthorizedAccessException("Không có quyền xem hóa đơn của thu ngân khác.");
        if (invoice.Status is not ("DA_THANH_TOAN" or "DA_HUY")) return null;
        // Không ghi DB, không tính lại giá/khuyến mãi/điểm khi xem hoặc in lại.
        invoice.IsReprint = true;
        return invoice;
    }

    // ==========================================
    // POS-07: KIỂM TRA DỮ LIỆU HÓA ĐƠN
    // ==========================================

    public bool ValidateForPrinting(
        PosInvoiceViewModel? invoice,
        out string error)
    {
        error = "";

        if (invoice == null)
        {
            error = "Không tìm thấy hóa đơn.";
            return false;
        }

        if (invoice.InvoiceId <= 0 ||
            string.IsNullOrWhiteSpace(invoice.InvoiceCode))
        {
            error = "Thông tin hóa đơn không hợp lệ.";
            return false;
        }

        if (!string.Equals(
                invoice.Status,
                "DA_THANH_TOAN",
                StringComparison.OrdinalIgnoreCase))
        {
            error = "Chỉ được in hóa đơn đã thanh toán.";
            return false;
        }

        if (invoice.Items == null ||
            invoice.Items.Count == 0)
        {
            error = "Hóa đơn không có sản phẩm.";
            return false;
        }

        if (invoice.Total < 0 ||
            invoice.ExchangeCredit < 0 || invoice.ExchangeCredit > invoice.Total ||
            invoice.CashReceived + invoice.ExchangeCredit < invoice.Total ||
            invoice.Change != invoice.CashReceived + invoice.ExchangeCredit - invoice.Total)
        {
            error = "Dữ liệu thanh toán không hợp lệ.";
            return false;
        }

        return true;
    }
}

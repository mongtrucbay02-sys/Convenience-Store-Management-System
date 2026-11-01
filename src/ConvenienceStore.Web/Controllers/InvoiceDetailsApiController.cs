using ConvenienceStore.Web.Models.Pos;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Authorize(Policy = PosInvoiceAccess.PolicyXem)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InvoiceDetailsApiController(PosInvoiceService invoices) : ControllerBase
{
    [HttpGet("{id:long}")]
    public Task<IActionResult> Details(long id, CancellationToken cancellationToken) => Read(id, false, cancellationToken);

    [HttpGet("{id:long}/print")]
    public Task<IActionResult> Print(long id, CancellationToken cancellationToken) => Read(id, true, cancellationToken);

    private async Task<IActionResult> Read(long id, bool print, CancellationToken cancellationToken)
    {
        if (id <= 0) return PhanHoiLoiApi.TaoKetQua(HttpContext, 422, MaLoiChung.DuLieuKhongHopLe,
            "Mã hóa đơn phải lớn hơn 0.", new[] { new LoiTruong("id", "Phải lớn hơn 0.") });
        try
        {
            var invoice = await invoices.GetDetailsAsync(id, PosInvoiceAccess.FromUser(User), cancellationToken);
            if (invoice is null) return PhanHoiLoiApi.TaoKetQua(HttpContext, 404,
                MaLoiChung.KhongTimThay, "Không tìm thấy hóa đơn.", null);
            var data = PosInvoiceApiData.From(invoice);
            return print ? Ok(new { hoaDon = data, khoGiay = "K80", inLai = true, daHuy = invoice.IsCancelled }) : Ok(data);
        }
        catch (UnauthorizedAccessException)
        {
            return PhanHoiLoiApi.TaoKetQua(HttpContext, 403, MaLoiChung.KhongDuQuyen,
                "Không có quyền xem hóa đơn này.", null);
        }
    }
}

// Giữ tên trường của hợp đồng POS/DT; ngày MySQL là giờ Việt Nam.
public static class PosInvoiceApiData
{
    public static object From(PosInvoiceViewModel invoice) => new
    {
        id = invoice.InvoiceId, ma = invoice.InvoiceCode, trangThai = invoice.Status,
        khachHangId = invoice.CustomerId, nguoiTaoId = invoice.CashierId,
        tenThuNgan = invoice.CashierName, tenKhachHang = invoice.CustomerName,
        soDienThoaiKhach = invoice.CustomerPhone,
        thoiDiemTao = new DateTimeOffset(DateTime.SpecifyKind(invoice.CreatedAt, DateTimeKind.Unspecified), TimeSpan.FromHours(7)),
        tongTienHang = invoice.Subtotal, tienGiam = invoice.Discount, thanhTien = invoice.Total,
        tienTruHangDoi = invoice.ExchangeCredit, tienKhachDua = invoice.CashReceived,
        tienThua = invoice.Change, diemDaCong = invoice.PointsEarned,
        huyLuc = invoice.CancelledAt is { } cancelledAt
            ? (DateTimeOffset?)new DateTimeOffset(DateTime.SpecifyKind(cancelledAt, DateTimeKind.Unspecified), TimeSpan.FromHours(7)) : null,
        lyDoHuy = invoice.CancellationReason, nguoiHuy = invoice.CancelledBy,
        chiTiet = invoice.Items.Select(line => new {
            // ct_hoa_don dùng khóa ghép; mã sản phẩm là định danh dòng trong một hóa đơn.
            id = line.ProductId, sanPhamId = line.ProductId, tenSanPham = line.ProductName,
            donViTinh = line.Unit, soLuong = line.Quantity, donGia = line.UnitPrice,
            tienGiam = line.Discount, thanhTien = line.LineTotal,
            khuyenMaiId = line.PromotionId, maKhuyenMai = line.PromotionCode
        }),
        phieuDoiTra = invoice.RelatedReturns.Select(item => new {
            id = item.Id, ma = item.Code, loai = item.Type, trangThai = item.Status,
            ngayLap = new DateTimeOffset(DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Unspecified), TimeSpan.FromHours(7)),
            tienHoan = item.Refund, tienThuThem = item.AdditionalPayment,
            diemThuHoi = item.PointsReversed, laHoaDonGoc = item.IsOriginalInvoice
        })
    };
}

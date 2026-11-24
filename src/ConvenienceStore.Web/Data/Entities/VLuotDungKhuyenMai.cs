using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class VLuotDungKhuyenMai
{
    public long? ChuongTrinhId { get; set; }

    public long? HoaDonId { get; set; }

    public string? MaHoaDon { get; set; }

    public DateTime? ThanhToanLuc { get; set; }

    public long? ThuNganId { get; set; }

    public long? KhachHangId { get; set; }

    public decimal? SoLuongSanPham { get; set; }

    public decimal? TienGiam { get; set; }
}

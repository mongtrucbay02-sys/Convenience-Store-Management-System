using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class VLuotDungKhuyenMai
{
    public uint? ChuongTrinhId { get; set; }

    public uint HoaDonId { get; set; }

    public string MaHoaDon { get; set; } = null!;

    public DateTime ThanhToanLuc { get; set; }

    public uint ThuNganId { get; set; }

    public uint? KhachHangId { get; set; }

    public decimal? SoLuongSanPham { get; set; }

    public decimal? TienGiam { get; set; }
}

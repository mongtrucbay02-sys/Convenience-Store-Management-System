using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class VChuongTrinhKhuyenMai
{
    public long? Id { get; set; }

    public string? Ma { get; set; }

    public string? Ten { get; set; }

    public string? MoTa { get; set; }

    public string? LoaiGiam { get; set; }

    public long? GiaTri { get; set; }

    public long? GiamToiDa { get; set; }

    public DateTime? BatDauLuc { get; set; }

    public DateTime? KetThucLuc { get; set; }

    public bool? ChiThanhVien { get; set; }

    public string? TrangThai { get; set; }

    public long? NguoiTaoId { get; set; }

    public long? PhienBan { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public string? AnhUrl { get; set; }

    public string? TrangThaiHienThi { get; set; }
}

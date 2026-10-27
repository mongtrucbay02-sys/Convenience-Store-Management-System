using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class VChuongTrinhKhuyenMai
{
    public uint Id { get; set; }

    public string Ma { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public string LoaiGiam { get; set; } = null!;

    public uint GiaTri { get; set; }

    public uint? GiamToiDa { get; set; }

    public DateTime BatDauLuc { get; set; }

    public DateTime KetThucLuc { get; set; }

    public bool ChiThanhVien { get; set; }

    public string TrangThai { get; set; } = null!;

    public uint NguoiTaoId { get; set; }

    public uint PhienBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public string TrangThaiHienThi { get; set; } = null!;
}

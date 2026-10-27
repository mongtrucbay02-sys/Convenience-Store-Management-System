using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class NhaCungCap
{
    public uint Id { get; set; }

    public string Ma { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string? NguoiLienHe { get; set; }

    public string SoDienThoai { get; set; } = null!;

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? MaSoThue { get; set; }

    public string? GhiChu { get; set; }

    public string TrangThai { get; set; } = null!;

    public uint PhienBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();
}

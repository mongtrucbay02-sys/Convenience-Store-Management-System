using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class DanhMuc
{
    public uint Id { get; set; }

    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public bool KhongDoiTra { get; set; }

    public string TrangThai { get; set; } = null!;

    public uint PhienBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<PhieuKiemKe> PhieuKiemKes { get; set; } = new List<PhieuKiemKe>();

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}

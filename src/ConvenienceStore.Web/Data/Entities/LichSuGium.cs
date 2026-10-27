using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class LichSuGium
{
    public uint Id { get; set; }

    public uint SanPhamId { get; set; }

    public uint GiaBan { get; set; }

    public DateTime HieuLucTu { get; set; }

    public uint NguoiTaoId { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? HuyLuc { get; set; }

    public uint? NguoiHuyId { get; set; }

    public virtual TaiKhoan? NguoiHuy { get; set; }

    public virtual TaiKhoan NguoiTao { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}

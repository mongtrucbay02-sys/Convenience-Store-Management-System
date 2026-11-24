using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class LichSuGium
{
    public long Id { get; set; }

    public long SanPhamId { get; set; }

    public long GiaBan { get; set; }

    public DateTime HieuLucTu { get; set; }

    public long NguoiTaoId { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? HuyLuc { get; set; }

    public long? NguoiHuyId { get; set; }

    public virtual TaiKhoan? NguoiHuy { get; set; }

    public virtual TaiKhoan NguoiTao { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class PhienDangNhap
{
    public long Id { get; set; }

    public long TaiKhoanId { get; set; }

    public string TokenBam { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public DateTime HoatDongCuoi { get; set; }

    public DateTime? HuyLuc { get; set; }

    public DateTime HetHanLuc { get; set; }

    public virtual TaiKhoan TaiKhoan { get; set; } = null!;
}

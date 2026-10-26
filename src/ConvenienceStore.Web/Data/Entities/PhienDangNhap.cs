using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class PhienDangNhap
{
    public ulong Id { get; set; }

    public uint TaiKhoanId { get; set; }

    public string TokenBam { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public DateTime HoatDongCuoi { get; set; }

    public DateTime? HuyLuc { get; set; }

    public virtual TaiKhoan TaiKhoan { get; set; } = null!;
}

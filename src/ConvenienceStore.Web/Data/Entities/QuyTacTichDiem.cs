using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class QuyTacTichDiem
{
    public long Id { get; set; }

    public long SoTienMotDiem { get; set; }

    public bool DangBat { get; set; }

    public DateTime HieuLucTu { get; set; }

    public long? NguoiTaoId { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual TaiKhoan? NguoiTao { get; set; }
}

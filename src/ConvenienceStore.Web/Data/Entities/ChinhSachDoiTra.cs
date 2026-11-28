using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class ChinhSachDoiTra
{
    public long Id { get; set; }

    public short SoNgayDoiTra { get; set; }

    public long NguongTienCanDuyet { get; set; }

    public DateTime HieuLucTu { get; set; }

    public long? NguoiTaoId { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual TaiKhoan? NguoiTao { get; set; }

    public virtual ICollection<PhieuDoiTra> PhieuDoiTras { get; set; } = new List<PhieuDoiTra>();
}

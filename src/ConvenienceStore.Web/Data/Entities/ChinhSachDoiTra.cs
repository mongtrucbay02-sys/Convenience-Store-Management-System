using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class ChinhSachDoiTra
{
    public uint Id { get; set; }

    public ushort SoNgayDoiTra { get; set; }

    public ulong NguongTienCanDuyet { get; set; }

    public DateTime HieuLucTu { get; set; }

    public uint? NguoiTaoId { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual TaiKhoan? NguoiTao { get; set; }

    public virtual ICollection<PhieuDoiTra> PhieuDoiTras { get; set; } = new List<PhieuDoiTra>();
}

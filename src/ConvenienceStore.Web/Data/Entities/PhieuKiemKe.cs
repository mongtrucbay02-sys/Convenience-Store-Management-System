using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class PhieuKiemKe
{
    public uint Id { get; set; }

    public string MaPhieu { get; set; } = null!;

    public string PhamVi { get; set; } = null!;

    public uint? DanhMucId { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? GhiChu { get; set; }

    public uint NguoiLapId { get; set; }

    public DateTime NgayLap { get; set; }

    public DateTime? GuiDuyetLuc { get; set; }

    public uint? NguoiDuyetId { get; set; }

    public DateTime? DuyetLuc { get; set; }

    public string? LyDoTuChoi { get; set; }

    public uint PhienBan { get; set; }

    public virtual ICollection<CtKiemKe> CtKiemKes { get; set; } = new List<CtKiemKe>();

    public virtual DanhMuc? DanhMuc { get; set; }

    public virtual TaiKhoan? NguoiDuyet { get; set; }

    public virtual TaiKhoan NguoiLap { get; set; } = null!;
}

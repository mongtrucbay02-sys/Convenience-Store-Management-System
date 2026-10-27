using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class TonKho
{
    public uint SanPhamId { get; set; }

    public int SoLuongTon { get; set; }

    public decimal GiaVonBinhQuan { get; set; }

    public int NguongToiThieu { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual SanPham SanPham { get; set; } = null!;
}

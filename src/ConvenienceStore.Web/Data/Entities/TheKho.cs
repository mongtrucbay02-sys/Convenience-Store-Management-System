using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class TheKho
{
    public ulong Id { get; set; }

    public uint SanPhamId { get; set; }

    public string LoaiBienDong { get; set; } = null!;

    public string MaChungTu { get; set; } = null!;

    public uint ChungTuId { get; set; }

    public int SoLuongThayDoi { get; set; }

    public int TonSau { get; set; }

    public decimal GiaVonApDung { get; set; }

    public decimal GiaVonSau { get; set; }

    public uint NguoiThucHienId { get; set; }

    public DateTime ThoiDiem { get; set; }

    public virtual TaiKhoan NguoiThucHien { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class VGiaBanHienHanh
{
    public uint SanPhamId { get; set; }

    public uint LichSuGiaId { get; set; }

    public uint GiaBan { get; set; }

    public DateTime HieuLucTu { get; set; }
}

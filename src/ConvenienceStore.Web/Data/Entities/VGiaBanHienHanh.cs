using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class VGiaBanHienHanh
{
    public long? SanPhamId { get; set; }

    public long? LichSuGiaId { get; set; }

    public long? GiaBan { get; set; }

    public DateTime? HieuLucTu { get; set; }
}

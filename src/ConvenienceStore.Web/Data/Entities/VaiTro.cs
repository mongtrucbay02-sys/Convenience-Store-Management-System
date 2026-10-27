using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class VaiTro
{
    public byte Id { get; set; }

    public string Ma { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}

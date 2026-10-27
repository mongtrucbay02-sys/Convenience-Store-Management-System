using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class BoDemChungTu
{
    public string TienTo { get; set; } = null!;

    public string Ky { get; set; } = null!;

    public uint SoCuoi { get; set; }
}

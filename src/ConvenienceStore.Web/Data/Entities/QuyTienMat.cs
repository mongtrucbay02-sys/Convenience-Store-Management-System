using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class QuyTienMat
{
    public long Id { get; set; }

    public long SoDu { get; set; }

    public bool DaChotSoDuDau { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<GiaoDichTienMat> GiaoDichTienMats { get; set; } = new List<GiaoDichTienMat>();
}

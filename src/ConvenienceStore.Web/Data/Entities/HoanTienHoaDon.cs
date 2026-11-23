using System;
using System.Collections.Generic;

namespace ConvenienceStore.Web.Data.Entities;

public partial class HoanTienHoaDon
{
    public long Id { get; set; }

    public long HoaDonId { get; set; }

    public long SoTienHoan { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public long DiemDaThuHoi { get; set; }

    public long DiemChuaThuHoi { get; set; }

    public string LyDo { get; set; } = null!;

    public long NguoiThucHienId { get; set; }

    public DateTime ThoiDiem { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual TaiKhoan NguoiThucHien { get; set; } = null!;
}

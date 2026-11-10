using ConvenienceStore.Web.Modules.Identity.Entities;

namespace ConvenienceStore.Web.Modules.Customers.Entities;

public sealed class QuyTacTichDiem
{
    public uint Id { get; set; }

    public uint SoTienMotDiem { get; set; } = 10000;

    public bool DangBat { get; set; } = true;

    public DateTime HieuLucTu { get; set; }

    public uint? NguoiTaoId { get; set; }
    public TaiKhoan? NguoiTao { get; set; }

    public DateTime NgayTao { get; set; }
}
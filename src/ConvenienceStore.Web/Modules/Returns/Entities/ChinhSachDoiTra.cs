using ConvenienceStore.Web.Modules.Identity.Entities;

namespace ConvenienceStore.Web.Modules.Returns.Entities;

public sealed class ChinhSachDoiTra
{
    public uint Id { get; set; }

    public ushort SoNgayDoiTra { get; set; } = 7;

    public ulong NguongTienCanDuyet { get; set; } = 500000;

    public DateTime HieuLucTu { get; set; }

    public uint? NguoiTaoId { get; set; }
    public TaiKhoan? NguoiTao { get; set; }

    public DateTime NgayTao { get; set; }
}
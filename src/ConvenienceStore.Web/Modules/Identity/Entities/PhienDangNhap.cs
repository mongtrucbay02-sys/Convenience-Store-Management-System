namespace ConvenienceStore.Web.Modules.Identity.Entities;

public sealed class PhienDangNhap
{
    public ulong Id { get; set; }

    public uint TaiKhoanId { get; set; }
    public TaiKhoan TaiKhoan { get; set; } = null!;

    public string TokenBam { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; }
    public DateTime HoatDongCuoi { get; set; }
    public DateTime? HuyLuc { get; set; }
}

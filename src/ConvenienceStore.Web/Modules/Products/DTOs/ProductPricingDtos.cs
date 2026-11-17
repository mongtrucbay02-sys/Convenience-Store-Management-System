namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record ProductPriceChangeDto
{
    public long? GiaBan { get; init; }
    public string? HieuLucTu { get; init; }
    public uint PhienBan { get; init; }
    public bool XacNhanDuoiGiaVon { get; init; }
}
public sealed record ProductPriceCancelDto { public uint PhienBan { get; init; } }
public sealed record ProductPriceResult(uint Id, uint SanPhamId, uint GiaBan, DateTimeOffset HieuLucTu,
    uint NguoiTaoId, DateTimeOffset NgayTao, DateTimeOffset? HuyLuc, uint? NguoiHuyId, uint PhienBan);

namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record ProductPriceChangeDto
{
    public long? GiaBan { get; init; }
    public string? HieuLucTu { get; init; }
    public long PhienBan { get; init; }
    public bool XacNhanDuoiGiaVon { get; init; }
}
public sealed record ProductPriceCancelDto { public long PhienBan { get; init; } }
public sealed record ProductPriceResult(long Id, long SanPhamId, long GiaBan, DateTimeOffset HieuLucTu,
    long NguoiTaoId, DateTimeOffset NgayTao, DateTimeOffset? HuyLuc, long? NguoiHuyId, long PhienBan);

namespace ConvenienceStore.Web.Modules.Products.DTOs;

public sealed record ProductDetailsDto : ProductListItemDto
{
    public string? AnhUrl { get; init; }
    public string? MoTa { get; init; }
    public int NguongToiThieu { get; init; }
    public DateTimeOffset NgayCapNhat { get; init; }
    public PagedResultDto<ProductPriceDto> GiaDaLenLich { get; init; } = new([], 0, 1, 20);
    public PagedResultDto<ProductPriceDto>? LichSuGia { get; init; }
}

public sealed record ProductPriceDto(long Id, long GiaBan, DateTimeOffset HieuLucTu,
    long? NguoiTaoId, DateTimeOffset? NgayTao, DateTimeOffset? HuyLuc, long? NguoiHuyId);

public sealed record ProductPricesQuery
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
    public bool DaLenLich { get; init; } = true;
}

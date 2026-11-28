namespace ConvenienceStore.Web.Modules.Inventory.Models;

public sealed record KetQuaDong(
    long SanPhamId,
    int SoLuongThayDoi,
    int TonTruoc,
    int TonSau,
    decimal GiaVonTruoc,
    decimal GiaVonSau,
    decimal GiaVonApDung,
    long? TheKhoId);

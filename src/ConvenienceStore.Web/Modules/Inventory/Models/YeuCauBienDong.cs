namespace ConvenienceStore.Web.Modules.Inventory.Models;

public sealed record YeuCauBienDong(
    LoaiBienDong Loai,
    long ChungTuId,
    string MaChungTu,
    long NguoiThucHienId,
    IReadOnlyList<DongBienDong> Dong);

namespace ConvenienceStore.Web.Modules.Inventory.Models;

public sealed record YeuCauBienDong(
    LoaiBienDong Loai,
    int ChungTuId,
    string MaChungTu,
    int NguoiThucHienId,
    IReadOnlyList<DongBienDong> Dong);

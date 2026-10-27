using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConvenienceStore.Web.Tests.Inventory;

public sealed class BoTroGoiDichVuKho(CsdlKhoFixture csdl)
{
    private readonly DuLieuTestKho _duLieu = new(csdl);

    public static DichVuKho TaoDichVu(CuaHangDbContext db) => new(db, NullLogger<DichVuKho>.Instance);

    public async Task<int> TaoSanPhamCoTonAsync(string tienTo, int ton, decimal giaVon, int nguong = 5)
    {
        var sanPhamId = await _duLieu.TaoSanPhamAsync(tienTo);
        await using var db = csdl.TaoDbContext();
        await using var giaoTac = await db.Database.BeginTransactionAsync();
        var dichVu = TaoDichVu(db);
        await dichVu.KhoiTaoTonKhoAsync(sanPhamId, nguong);
        if (ton > 0)
            await dichVu.GhiNhanBienDongAsync(new YeuCauBienDong(
                LoaiBienDong.NHAP_HANG,
                DuLieuTestKho.IdChungTuGia(),
                DuLieuTestKho.MaChungTuGia("PN"),
                csdl.TaiKhoanKhoId,
                [new DongBienDong(sanPhamId, SoLuong: ton, GiaVon: giaVon)]));
        await giaoTac.CommitAsync();
        return sanPhamId;
    }

    public async Task<KetQuaBienDong> GhiVaCommitAsync(LoaiBienDong loai, params DongBienDong[] dong)
    {
        await using var db = csdl.TaoDbContext();
        await using var giaoTac = await db.Database.BeginTransactionAsync();
        var ketQua = await TaoDichVu(db).GhiNhanBienDongAsync(TaoYeuCau(loai, dong));
        await giaoTac.CommitAsync();
        return ketQua;
    }

    public async Task<LoiDichVuKho> GhiVaBatLoiAsync(LoaiBienDong loai, params DongBienDong[] dong)
    {
        await using var db = csdl.TaoDbContext();
        await using var giaoTac = await db.Database.BeginTransactionAsync();
        try
        {
            await TaoDichVu(db).GhiNhanBienDongAsync(TaoYeuCau(loai, dong));
        }
        catch (LoiDichVuKho loi)
        {
            await giaoTac.RollbackAsync();
            return loi;
        }

        await giaoTac.RollbackAsync();
        throw new InvalidOperationException("Mong đợi dịch vụ kho báo lỗi nhưng không có lỗi.");
    }

    public YeuCauBienDong TaoYeuCau(LoaiBienDong loai, IReadOnlyList<DongBienDong> dong) => new(
        loai,
        DuLieuTestKho.IdChungTuGia(),
        DuLieuTestKho.MaChungTuGia(loai switch
        {
            LoaiBienDong.NHAP_HANG => "PN",
            LoaiBienDong.BAN_HANG => "HD",
            LoaiBienDong.KIEM_KE => "KK",
            _ => "XH",
        }),
        loai == LoaiBienDong.BAN_HANG ? csdl.TaiKhoanThuNganId : csdl.TaiKhoanKhoId,
        dong);
}

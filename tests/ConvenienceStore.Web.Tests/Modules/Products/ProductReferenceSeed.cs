using ConvenienceStore.Web.Tests.HaTang;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Modules.Inventory.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConvenienceStore.Web.Tests.Modules.Products;

internal static class ProductReferenceSeed
{
    public static async Task<(long Product, long Actor, long Category)> Unused(CsdlMySqlTam database)
    {
        var ids = await ProductReadSeed.Create(database);
        var actor = Convert.ToUInt32(await database.DocMotGiaTriAsync("SELECT nguoi_tao_id FROM san_pham WHERE id=@id", ("@id", ids.Active)));
        await using var db = database.TaoDbContext(); var inventory = new DichVuKho(db, NullLogger<DichVuKho>.Instance);
        var product = await new EfProductCreationRepository(db, inventory).CreateAsync(Sp03ProductUnitTests.Valid() with { Ma = "UNUSED-" + Guid.NewGuid().ToString("N")[..12], DanhMucId = ids.Category, DonViTinh = "Hộp" }, actor);
        return (product, actor, ids.Category);
    }

    public static async Task HeldAndDraft(CsdlMySqlTam db, long product, long actor)
    {
        var key = "held_" + Guid.NewGuid().ToString("N"); var supplier = "SUP-" + Guid.NewGuid().ToString("N")[..12]; var receipt = "DR-" + Guid.NewGuid().ToString("N")[..14];
        var phone = "0" + Random.Shared.NextInt64(100000000, 999999999).ToString();
        await db.ChayAsync("""
            INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id) SELECT @key,'unused','Held cashier',id FROM vai_tro WHERE ma='TN';
            INSERT INTO gio_hang(thu_ngan_id,trang_thai,tam_giu_luc) SELECT id,'TAM_GIU',NOW() FROM tai_khoan WHERE ten_dang_nhap=@key;
            INSERT INTO ct_gio_hang(gio_hang_id,san_pham_id,so_luong,don_gia)
            SELECT g.id,@product,1,10000 FROM gio_hang g JOIN tai_khoan t ON t.id=g.thu_ngan_id WHERE t.ten_dang_nhap=@key;
            INSERT INTO nha_cung_cap(ma,ten,so_dien_thoai) VALUES(@supplier,@key,@phone);
            INSERT INTO phieu_nhap(ma_phieu,nha_cung_cap_id,ngay_nhap,nguoi_lap_id,tong_tien) SELECT @receipt,id,CURDATE(),@actor,10000 FROM nha_cung_cap WHERE ma=@supplier;
            INSERT INTO ct_phieu_nhap(phieu_nhap_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,gia_nhap)
            SELECT id,@product,'Original receipt','Hộp',1,10000 FROM phieu_nhap WHERE ma_phieu=@receipt;
            """, ("@key", key), ("@supplier", supplier), ("@receipt", receipt), ("@phone", phone), ("@product", product), ("@actor", actor));
    }

    // SQL fixture, not an executed POS checkout/browser workflow.
    public static async Task<long> Invoice(CsdlMySqlTam db, long product)
    {
        var name = "snapshot_" + Guid.NewGuid().ToString("N"); var code = "SNAP-" + Guid.NewGuid().ToString("N")[..14];
        await db.ChayAsync("""
            INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id)
            SELECT @name,'unused','Snapshot cashier',id FROM vai_tro WHERE ma='TN';
            INSERT INTO gio_hang(thu_ngan_id,trang_thai,hoan_tat_luc)
            SELECT id,'DA_HOAN_TAT',NOW() FROM tai_khoan WHERE ten_dang_nhap=@name;
            INSERT INTO hoa_don(ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,tong_tien_hang,tong_thanh_toan,tien_khach_dua,tien_thua)
            SELECT @code,g.id,g.thu_ngan_id,NOW(),10000,10000,10000,0 FROM gio_hang g JOIN tai_khoan t ON t.id=g.thu_ngan_id WHERE t.ten_dang_nhap=@name;
            INSERT INTO ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,gia_von)
            SELECT h.id,p.id,'Original snapshot','Hộp',1,10000,8000 FROM hoa_don h JOIN san_pham p ON p.id=@product WHERE h.ma_hoa_don=@code;
            """, ("@name", name), ("@code", code), ("@product", product));
        return Convert.ToUInt32(await db.DocMotGiaTriAsync("SELECT id FROM hoa_don WHERE ma_hoa_don=@code", ("@code", code)));
    }
}

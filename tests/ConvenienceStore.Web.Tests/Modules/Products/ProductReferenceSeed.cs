using ConvenienceStore.Web.Tests.HaTang;

namespace ConvenienceStore.Web.Tests.Modules.Products;

internal static class ProductReferenceSeed
{
    // SQL fixture, not an executed POS checkout/browser workflow.
    public static async Task<uint> Invoice(CsdlMySqlTam db, uint product)
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

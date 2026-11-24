using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
namespace ConvenienceStore.Web.Data;
public partial class CuaHangDbContext
{
    // Scaffolding omits some CLR-zero defaults. Raw INSERTs still require the
    // database defaults, so retain the captured catalog definitions explicitly.
    private static void ConfigurePostgreSqlDefaults(ModelBuilder builder)
    {
        void Default(string table, string column, string sql)
        {
            var entity=builder.Model.GetEntityTypes().Single(e=>e.GetSchema()=="smartmart" && e.GetTableName()==table);
            var store=StoreObjectIdentifier.Table(table,"smartmart");
            var property=entity.GetProperties().Single(p=>p.GetColumnName(store)==column);
            if(property.FindAnnotation(RelationalAnnotationNames.DefaultValue) is null
                && property.GetDefaultValueSql() is null)
            {
                property.SetDefaultValueSql(sql);
                property.ValueGenerated=ValueGenerated.OnAdd;
            }
        }
        Default("bo_dem_chung_tu", "so_cuoi", "0");
        Default("chuong_trinh_khuyen_mai", "chi_thanh_vien", "false");
        Default("ct_gio_hang", "giam_don_vi", "0");
        Default("ct_hoa_don", "giam_don_vi", "0");
        Default("ct_phieu_doi_tra", "ngoai_le", "false");
        Default("danh_muc", "khong_doi_tra", "false");
        Default("hoa_don", "tong_giam", "0");
        Default("hoa_don", "tien_tru_hang_doi", "0");
        Default("hoa_don", "diem_cong", "0");
        Default("hoan_tien_hoa_don", "diem_da_thu_hoi", "0");
        Default("hoan_tien_hoa_don", "diem_chua_thu_hoi", "0");
        Default("khach_hang", "diem_hien_co", "0");
        Default("phieu_doi_tra", "can_duyet_qua_han", "false");
        Default("phieu_doi_tra", "can_duyet_danh_muc", "false");
        Default("phieu_doi_tra", "can_duyet_vuot_nguong", "false");
        Default("phieu_doi_tra", "tien_hoan", "0");
        Default("phieu_doi_tra", "tien_thu_them", "0");
        Default("phieu_doi_tra", "diem_thu_hoi", "0");
        Default("phieu_nhap", "tong_tien", "0");
        Default("quy_tien_mat", "so_du", "0");
        Default("quy_tien_mat", "da_chot_so_du_dau", "false");
        Default("tai_khoan", "so_lan_sai", "0");
        Default("ton_kho", "so_luong_ton", "0");
        Default("ton_kho", "gia_von_binh_quan", "0");
    }
}

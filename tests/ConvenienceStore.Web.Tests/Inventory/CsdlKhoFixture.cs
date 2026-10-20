using ConvenienceStore.Web.Tests.HaTang;
using Xunit;

namespace ConvenienceStore.Web.Tests.Inventory;

/// <summary>
/// T4-17: CSDL MySQL thật dùng chung cho các lớp test dịch vụ "Ghi nhận biến động kho"
/// (UT-01 → UT-13, Tài liệu dịch vụ kho 1.2 mục 6).
/// </summary>
/// <remarks>
/// Dữ liệu nền theo mẫu docs/db/kiem_thu_v1.sql: 3 tài khoản (AD, TN, NK) và 1 danh mục.
/// Mật khẩu băm là chuỗi giả vì test kho không đăng nhập; không phụ thuộc Admin đầu tiên (T4-16).
/// Không xóa dữ liệu giữa các test (trigger chặn xóa thẻ kho, BR-KHO-03): mỗi test tự tạo
/// sản phẩm riêng bằng <see cref="DuLieuTestKho"/> và chỉ đọc sản phẩm của mình.
/// </remarks>
public sealed class CsdlKhoFixture : CsdlMySqlTam
{
    public int TaiKhoanAdminId { get; private set; }

    public int TaiKhoanThuNganId { get; private set; }

    public int TaiKhoanKhoId { get; private set; }

    public int DanhMucId { get; private set; }

    protected override async Task NapDuLieuNenAsync()
    {
        await ChayAsync(
            """
            INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id, bat_buoc_doi_mk) VALUES
              ('test.admin', 'khong-dang-nhap', 'Admin test',     (SELECT id FROM vai_tro WHERE ma = 'AD'), FALSE),
              ('test.thungan', 'khong-dang-nhap', 'Thu ngân test', (SELECT id FROM vai_tro WHERE ma = 'TN'), FALSE),
              ('test.kho', 'khong-dang-nhap', 'Kho test',          (SELECT id FROM vai_tro WHERE ma = 'NK'), FALSE)
            """);
        await ChayAsync("INSERT INTO danh_muc (ten) VALUES ('Danh mục test')");

        TaiKhoanAdminId = await DocIdAsync("SELECT id FROM tai_khoan WHERE ten_dang_nhap = 'test.admin'");
        TaiKhoanThuNganId = await DocIdAsync("SELECT id FROM tai_khoan WHERE ten_dang_nhap = 'test.thungan'");
        TaiKhoanKhoId = await DocIdAsync("SELECT id FROM tai_khoan WHERE ten_dang_nhap = 'test.kho'");
        DanhMucId = await DocIdAsync("SELECT id FROM danh_muc WHERE ten = 'Danh mục test'");
    }

    private async Task<int> DocIdAsync(string sql) =>
        Convert.ToInt32(await DocMotGiaTriAsync(sql)
            ?? throw new InvalidOperationException("Không nạp được dữ liệu nền: " + sql));
}

/// <summary>
/// Gom các lớp test dịch vụ kho vào một collection: dùng chung một CSDL tạm, chạy tuần tự.
/// Lớp test khai báo <c>[Collection(CsdlKhoCollection.Ten)]</c> và nhận <see cref="CsdlKhoFixture"/> qua constructor.
/// </summary>
[CollectionDefinition(Ten)]
public sealed class CsdlKhoCollection : ICollectionFixture<CsdlKhoFixture>
{
    public const string Ten = "DichVuKho";
}

namespace ConvenienceStore.Web.Tests.Inventory;

/// <summary>Tồn kho đọc thẳng từ bảng ton_kho để kiểm tra kết quả.</summary>
public sealed record TonKhoDoc(int SoLuongTon, decimal GiaVonBinhQuan, int NguongToiThieu);

/// <summary>
/// T4-17: hàm dùng chung cho test dịch vụ kho. Mỗi test tạo sản phẩm riêng, nên các test
/// không lẫn dữ liệu của nhau dù CSDL không được dọn giữa các test.
/// </summary>
/// <remarks>
/// Tồn ban đầu phải tạo bằng chính dịch vụ (KhoiTaoTonKhoAsync rồi NHAP_HANG), không UPDATE thẳng
/// ton_kho, để thẻ kho khớp tồn (ERD Kho v1 mục 9). Lớp này chỉ tạo sản phẩm và đọc kết quả.
/// </remarks>
public sealed class DuLieuTestKho(CsdlKhoFixture csdl)
{
    private static int _soThuTu;

    /// <summary>
    /// Thêm một sản phẩm mới, chưa có bản ghi tồn kho. Mã dạng <c>UT01-1a2b3c4d</c> (tối đa 20 ký tự).
    /// </summary>
    /// <param name="tienTo">Mã test đặt đầu mã sản phẩm, ví dụ "UT01"; tối đa 11 ký tự.</param>
    public async Task<int> TaoSanPhamAsync(string tienTo = "UT")
    {
        if (tienTo.Length is 0 or > 11)
            throw new ArgumentException("Tiền tố dài 1–11 ký tự để mã sản phẩm không quá 20 ký tự.", nameof(tienTo));

        var ma = $"{tienTo}-{Guid.NewGuid().ToString("N")[..8]}";
        await using var ketNoi = await csdl.MoKetNoiAsync();
        await using var lenh = CsdlKhoFixture.TaoLenh(ketNoi,
            """
            INSERT INTO san_pham (ma, ten_san_pham, danh_muc_id, don_vi_tinh, nguoi_tao_id)
            VALUES (@ma, @ten, @dm, 'Cái', @nguoi)
            """,
            ("@ma", ma), ("@ten", "Sản phẩm " + ma), ("@dm", csdl.DanhMucId), ("@nguoi", csdl.TaiKhoanKhoId));
        await lenh.ExecuteNonQueryAsync();
        return checked((int)lenh.LastInsertedId);
    }

    /// <summary>Mã chứng từ giả không trùng, ví dụ <c>PN-TEST-0001</c> (the_kho không có khóa ngoại tới chứng từ).</summary>
    public static string MaChungTuGia(string tienTo = "PN") =>
        $"{tienTo}-TEST-{Interlocked.Increment(ref _soThuTu):D4}";

    /// <summary>Id chứng từ giả đi kèm mã chứng từ giả.</summary>
    public static int IdChungTuGia() => Interlocked.Increment(ref _soThuTu);

    /// <summary>Tồn hiện tại của sản phẩm; null nếu chưa có bản ghi ton_kho.</summary>
    public async Task<TonKhoDoc?> DocTonKhoAsync(int sanPhamId)
    {
        await using var ketNoi = await csdl.MoKetNoiAsync();
        await using var lenh = CsdlKhoFixture.TaoLenh(ketNoi,
            "SELECT so_luong_ton, gia_von_binh_quan, nguong_toi_thieu FROM ton_kho WHERE san_pham_id = @sp",
            ("@sp", sanPhamId));
        await using var doc = await lenh.ExecuteReaderAsync();
        if (!await doc.ReadAsync())
            return null;
        return new TonKhoDoc(
            Convert.ToInt32(doc.GetValue(0)), Convert.ToDecimal(doc.GetValue(1)), Convert.ToInt32(doc.GetValue(2)));
    }

    /// <summary>Số dòng thẻ kho của sản phẩm, lọc theo loại biến động nếu có (vd "BAN_HANG").</summary>
    public async Task<int> DemTheKhoAsync(int sanPhamId, string? loaiBienDong = null)
    {
        var kq = await csdl.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM the_kho WHERE san_pham_id = @sp AND (@loai IS NULL OR loai_bien_dong = @loai)",
            ("@sp", sanPhamId), ("@loai", loaiBienDong));
        return Convert.ToInt32(kq);
    }
}

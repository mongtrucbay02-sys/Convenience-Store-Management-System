namespace ConvenienceStore.Web.Shared.Errors;

public class LoiNghiepVu : Exception
{
    public LoiNghiepVu(string ma, int maHttp, string thongBao, object? chiTiet = null, Exception? loiGoc = null)
        : base(thongBao, loiGoc)
    {
        Ma = ma;
        MaHttp = maHttp;
        ChiTiet = chiTiet;
    }

    public string Ma { get; }

    public int MaHttp { get; }

    public object? ChiTiet { get; }

    public static LoiNghiepVu DuLieuKhongHopLe(IReadOnlyList<LoiTruong> loiTheoTruong) =>
        new(MaLoiChung.DuLieuKhongHopLe, StatusCodes.Status422UnprocessableEntity, ThongBaoLoi.DuLieuKhongHopLe, loiTheoTruong);

    public static LoiNghiepVu DuLieuKhongHopLe(string truong, string loi) =>
        DuLieuKhongHopLe([new LoiTruong(truong, loi)]);

    public static LoiNghiepVu KhongTimThay(string thongBao) =>
        new(MaLoiChung.KhongTimThay, StatusCodes.Status404NotFound, thongBao);

    public static LoiNghiepVu XungDot(string ma, string thongBao, object? chiTiet = null) =>
        new(ma, StatusCodes.Status409Conflict, thongBao, chiTiet);
}

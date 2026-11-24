using System.Data.Common;

namespace ConvenienceStore.Web.Shared.Data;

public static class DocDuLieu
{
    public static int SoNguyen(this DbDataReader doc, int cot) => Convert.ToInt32(doc.GetValue(cot));

    public static long SoNguyenDai(this DbDataReader doc, int cot) => Convert.ToInt64(doc.GetValue(cot));

    public static decimal SoThapPhan(this DbDataReader doc, int cot) => Convert.ToDecimal(doc.GetValue(cot));

    public static bool DungSai(this DbDataReader doc, int cot) => Convert.ToBoolean(doc.GetValue(cot));

    public static string Chuoi(this DbDataReader doc, int cot) => Convert.ToString(doc.GetValue(cot)) ?? "";

    public static string? ChuoiHoacNull(this DbDataReader doc, int cot) =>
        doc.IsDBNull(cot) ? null : Convert.ToString(doc.GetValue(cot));

    public static DateTime NgayGio(this DbDataReader doc, int cot) =>
        ConvenienceStore.Web.Shared.Api.GioVietNam.ThanhOffset(Convert.ToDateTime(doc.GetValue(cot))).DateTime;

    public static DateTime VietnamDateTime(this DbDataReader doc, int cot) => doc.NgayGio(cot);

    public static DateTime? NgayGioHoacNull(this DbDataReader doc, int cot) =>
        doc.IsDBNull(cot) ? null : doc.NgayGio(cot);

    public static long? SoNguyenDaiHoacNull(this DbDataReader doc, int cot) =>
        doc.IsDBNull(cot) ? null : Convert.ToInt64(doc.GetValue(cot));
}

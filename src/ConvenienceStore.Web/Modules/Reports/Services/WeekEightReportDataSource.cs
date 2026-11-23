using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Shared.Data;

namespace ConvenienceStore.Web.Modules.Reports.Services;

public sealed record WeekSale(long Id,string Code,DateTime At,long? CustomerId,string CustomerName,string CashierName,long Gross,long Discount)
{
    public long Net => Gross-Discount;
}
public sealed record WeekReturn(long Id,string Code,long InvoiceId,DateTime At,string Kind,long CashierId,string CashierName,
    long? CustomerId,string CustomerName,long ProductId,string ProductName,string Reason,int Quantity,long Value,long CashRefund,IReadOnlyList<long> Promotions,
    IReadOnlyDictionary<long,string>? PromotionNames=null);
public sealed record WeekCustomer(long Id,string Name,DateTime CreatedAt);
public sealed record WeekPoints(long Id,long CustomerId,string Name,DateTime At,string Kind,int Change,long? InvoiceId);
public interface IWeekEightReportDataSource
{
    Task<IReadOnlyList<WeekSale>> SalesAsync(DateTime from,DateTime to,CancellationToken ct);
    Task<IReadOnlyList<WeekReturn>> ReturnsAsync(DateTime from,DateTime to,CancellationToken ct);
    Task<IReadOnlyList<WeekCustomer>> CustomersAsync(DateTime from,DateTime to,CancellationToken ct);
    Task<IReadOnlyList<WeekPoints>> PointsAsync(DateTime from,DateTime to,CancellationToken ct);
    Task<IReadOnlyList<PromotionUse>> UsesAsync(DateTime from,DateTime to,CancellationToken ct);
}

public sealed class MySqlWeekEightReportDataSource(CuaHangDbContext db,PromotionUsageStore usage) : IWeekEightReportDataSource
{
    private static long Number(object x)=>Convert.ToInt64(x);
    private static long? NullableNumber(object x)=>x is DBNull?null:Number(x);
    public async Task<IReadOnlyList<WeekSale>> SalesAsync(DateTime from,DateTime to,CancellationToken ct) =>
        await db.DocDanhSachAsync("""
            SELECT h.id,h.ma_hoa_don,h.thanh_toan_luc,h.khach_hang_id,
              CASE WHEN k.trang_thai='HOAT_DONG' THEN k.ho_ten WHEN k.id IS NOT NULL THEN CONCAT('Khách đã ẩn #',k.id) ELSE 'Khách lẻ' END,
              a.ho_ten,SUM(d.so_luong*d.don_gia),SUM(d.so_luong*d.giam_don_vi)
            FROM hoa_don h JOIN ct_hoa_don d ON d.hoa_don_id=h.id
            JOIN tai_khoan a ON a.id=h.thu_ngan_id LEFT JOIN khach_hang k ON k.id=h.khach_hang_id
            WHERE h.trang_thai='DA_THANH_TOAN' AND h.thanh_toan_luc>=@from AND h.thanh_toan_luc<@to
            GROUP BY h.id,h.ma_hoa_don,h.thanh_toan_luc,h.khach_hang_id,k.trang_thai,k.ho_ten,k.id,a.ho_ten
            ORDER BY h.thanh_toan_luc,h.id
            """,r=>new WeekSale(Number(r[0]),r.GetString(1),r.GetDateTime(2),NullableNumber(r[3]),r.GetString(4),r.GetString(5),Number(r[6]),Number(r[7])),
            ct,("@from",from),("@to",to));

    public async Task<IReadOnlyList<WeekReturn>> ReturnsAsync(DateTime from,DateTime to,CancellationToken ct)
    {
        var rows=await db.DocDanhSachAsync("""
            SELECT h.id,h.ma_phieu,h.hoa_don_goc_id,h.hoan_tat_luc,h.loai,h.thu_ngan_id,a.ho_ten,h.khach_hang_id,
              CASE WHEN k.trang_thai='HOAT_DONG' THEN k.ho_ten WHEN k.id IS NOT NULL THEN CONCAT('Khách đã ẩn #',k.id) ELSE 'Khách lẻ' END,
              l.san_pham_id,d.ten_san_pham,l.ly_do,l.so_luong,l.thanh_tien,h.tien_hoan
            FROM phieu_doi_tra h JOIN ct_phieu_doi_tra l ON l.phieu_doi_tra_id=h.id
            JOIN hoa_don i ON i.id=h.hoa_don_goc_id AND i.trang_thai='DA_THANH_TOAN'
            JOIN ct_hoa_don d ON d.hoa_don_id=l.hoa_don_id AND d.san_pham_id=l.san_pham_id
            JOIN tai_khoan a ON a.id=h.thu_ngan_id LEFT JOIN khach_hang k ON k.id=h.khach_hang_id
            WHERE h.trang_thai='HOAN_TAT' AND h.hoan_tat_luc>=@from AND h.hoan_tat_luc<@to
            ORDER BY h.id,l.san_pham_id
            """,r=>new WeekReturn(Number(r[0]),r.GetString(1),Number(r[2]),r.GetDateTime(3),r.GetString(4),Number(r[5]),r.GetString(6),
                NullableNumber(r[7]),r.GetString(8),Number(r[9]),r.GetString(10),r.GetString(11),Convert.ToInt32(r[12]),Number(r[13]),Number(r[14]),[]),ct,("@from",from),("@to",to));
        // Separate rows avoid GROUP_CONCAT's length limit and preserve names for returns of prior-period sales.
        var promotions=await db.DocDanhSachAsync("""
            SELECT DISTINCT h.id,p.id,p.ten FROM phieu_doi_tra h
            JOIN ct_hoa_don d ON d.hoa_don_id=h.hoa_don_goc_id
            JOIN chuong_trinh_khuyen_mai p ON p.id=d.khuyen_mai_id
            WHERE h.trang_thai='HOAN_TAT' AND h.hoan_tat_luc>=@from AND h.hoan_tat_luc<@to
            ORDER BY h.id,p.id
            """,r=>new {ReturnId=Number(r[0]),Id=Number(r[1]),Name=r.GetString(2)},ct,("@from",from),("@to",to));
        var names=promotions.GroupBy(x=>x.ReturnId).ToDictionary(g=>g.Key,g=>(IReadOnlyDictionary<long,string>)g.ToDictionary(x=>x.Id,x=>x.Name));
        // Allocate header cash exactly once across reasons/products, including remainder; exchanges can refund zero cash.
        return rows.GroupBy(x=>x.Id).SelectMany(g=>
        {
            var lines=g.ToArray();var weight=lines.Sum(x=>x.Value);long allocated=0;
            return lines.Select((x,index)=>
            {
                var cash=index==lines.Length-1?x.CashRefund-allocated:weight==0?0:(long)Math.Floor((decimal)x.CashRefund*x.Value/weight);
                allocated+=cash;var map=names.GetValueOrDefault(x.Id);
                return x with {CashRefund=cash,Promotions=map?.Keys.ToArray()??[],PromotionNames=map};
            }).ToArray();
        }).ToArray();
    }

    public async Task<IReadOnlyList<WeekCustomer>> CustomersAsync(DateTime from,DateTime to,CancellationToken ct) =>
        await db.DocDanhSachAsync("""
            SELECT id,CASE WHEN trang_thai='HOAT_DONG' THEN ho_ten ELSE CONCAT('Khách đã ẩn #',id) END,ngay_tao
            FROM khach_hang WHERE ngay_tao>=@from AND ngay_tao<@to ORDER BY id
            """,r=>new WeekCustomer(Number(r[0]),r.GetString(1),r.GetDateTime(2)),ct,("@from",from),("@to",to));

    public async Task<IReadOnlyList<WeekPoints>> PointsAsync(DateTime from,DateTime to,CancellationToken ct) =>
        await db.DocDanhSachAsync("""
            SELECT l.id,l.khach_hang_id,CASE WHEN k.trang_thai='HOAT_DONG' THEN k.ho_ten ELSE CONCAT('Khách đã ẩn #',k.id) END,
              l.thoi_diem,l.loai,l.so_diem,l.hoa_don_id
            FROM lich_su_diem l JOIN khach_hang k ON k.id=l.khach_hang_id
            WHERE l.thoi_diem>=@from AND l.thoi_diem<@to ORDER BY l.id
            """,r=>new WeekPoints(Number(r[0]),Number(r[1]),r.GetString(2),r.GetDateTime(3),r.GetString(4),Convert.ToInt32(r[5]),NullableNumber(r[6])),ct,("@from",from),("@to",to));

    public async Task<IReadOnlyList<PromotionUse>> UsesAsync(DateTime from,DateTime to,CancellationToken ct)=>await usage.ReadAsync(from,to,null,ct);
}

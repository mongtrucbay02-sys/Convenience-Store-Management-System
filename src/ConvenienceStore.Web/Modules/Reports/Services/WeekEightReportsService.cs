using System.Globalization;
using ConvenienceStore.Web.Modules.Reports.Models;

namespace ConvenienceStore.Web.Modules.Reports.Services;

/// <summary>BC-09..11. Paid sale snapshots, completed returns on their completion date, and immutable point events.</summary>
public sealed class WeekEightReportsService(IWeekEightReportDataSource source,TimeProvider clock)
{
    public static readonly IReadOnlyDictionary<string,string> Reasons = new Dictionary<string,string> {
        ["LOI_SAN_PHAM"]="Lỗi sản phẩm",["HET_HAN"]="Hết hạn",["KHONG_DUNG_NHU_CAU"]="Không đúng nhu cầu",["KHAC"]="Khác" };
    private static string N(long n)=>n.ToString("N0",CultureInfo.GetCultureInfo("vi-VN"));
    private static string Dt(DateTime at)=>at.ToString("dd/MM/yyyy HH:mm");
    private static long Rate(long n,long d)=>d==0?0:(long)Math.Round(n*1000m/d,MidpointRounding.AwayFromZero);
    private static ReportMetric M(string label,long value,string format="currency")=>new(label,value,format);
    private static ReportTableRow R(string key,params string[] cells)=>new(key,cells);
    private static string Title(string code)=>code switch {"BC-09"=>"Báo cáo đổi trả","BC-10"=>"Khách hàng và tích điểm",_=>"Hiệu quả khuyến mãi"};

    public async Task<OperationalReportResponse> GetAsync(string code,ReportQuery query,CancellationToken ct,string? detailKey=null)
    {
        code=code.ToUpperInvariant();
        if(code is not ("BC-09" or "BC-10" or "BC-11")) throw new ArgumentException("Mã báo cáo không hợp lệ.",nameof(code));
        var valid=ReportRangeResolver.TryResolve(query,clock.GetUtcNow(),out var resolved,out var error);
        if(!valid) ReportRangeResolver.TryResolve(new ReportQuery(),clock.GetUtcNow(),out resolved,out _);
        var range=resolved!;
        OperationalReportResponse Build(IReadOnlyList<ReportMetric> metrics,IReadOnlyList<string> columns,
            IReadOnlyList<ReportTableRow> rows,IReadOnlyList<ReportChartPoint> chart,string? message=null)=>
            new(code+(detailKey is null?"":" detail"),Title(code)+(detailKey is null?"":" · Chi tiết"),range,metrics,columns,rows,chart,
                message ?? (rows.Count==0?"Không có dữ liệu trong khoảng đã chọn":null),query);
        if(!valid) return Build([],[],[],[],error);
        if((query.CashierId.HasValue && query.CashierId<=0) || (query.PromotionId.HasValue && query.PromotionId<=0)
            || (!string.IsNullOrWhiteSpace(query.ReturnReason) && !Reasons.ContainsKey(query.ReturnReason)))
            return Build([],[],[],[],"Bộ lọc báo cáo không hợp lệ.");
        var from=range.FromInclusive.DateTime;var to=range.ToExclusive.DateTime;
        var sales=await source.SalesAsync(from,to,ct);
        var returns=await source.ReturnsAsync(from,to,ct);

        if(code=="BC-09")
        {
            var filtered=returns.Where(x=>(query.CashierId is null || x.CashierId==query.CashierId)
                && (string.IsNullOrWhiteSpace(query.ReturnReason) || x.Reason==query.ReturnReason)).ToArray();
            if(detailKey is not null)
            {
                if(!detailKey.StartsWith("reason:",StringComparison.Ordinal) || !Reasons.ContainsKey(detailKey[7..]))
                    return Build([],[],[],[],"Dòng báo cáo không hợp lệ.");
                var rows=filtered.Where(x=>x.Reason==detailKey[7..]).Select(x=>R($"return:{x.Id}:{x.ProductId}",x.Code,
                    x.Kind=="DOI_HANG"?"Đổi hàng":"Trả hàng",Dt(x.At),x.CashierName,x.ProductName,N(x.Quantity),N(x.Value),N(x.CashRefund))).ToArray();
                return Build([], ["Phiếu","Loại","Thời điểm (UTC+7)","Thu ngân","Sản phẩm","Số lượng","Giá trị hàng trả","Tiền hoàn"],rows,[]);
            }
            var groups=filtered.GroupBy(x=>x.Reason).OrderBy(x=>x.Key).ToArray();
            var topProduct=filtered.GroupBy(x=>x.ProductId).OrderByDescending(g=>g.Sum(x=>x.Quantity)).ThenBy(g=>g.Key).FirstOrDefault();
            var totalValue=filtered.Sum(x=>x.Value);var gross=sales.Sum(x=>x.Gross);var promotion=sales.Sum(x=>x.Discount);
            var allReturned=returns.Sum(x=>x.Value);
            var metrics=new[]{M("Số phiếu trả",filtered.Where(x=>x.Kind=="TRA_HANG").Select(x=>x.Id).Distinct().Count(),"number"),
                M("Số phiếu đổi",filtered.Where(x=>x.Kind=="DOI_HANG").Select(x=>x.Id).Distinct().Count(),"number"),
                M("Số lượng trả",filtered.Sum(x=>x.Quantity),"number"),M("Tiền hoàn",filtered.Sum(x=>x.CashRefund)),
                M("Giá trị hàng trả",totalValue),M("Tỷ lệ trả / doanh thu sau KM",Rate(totalValue,gross-promotion),"percent"),
                M("Trả nhiều nhất: "+(topProduct?.First().ProductName??"—"),topProduct?.Sum(x=>x.Quantity)??0,"number"),
                M("Doanh thu gộp toàn kỳ",gross),M("Giảm trừ toàn kỳ",promotion+allReturned),M("Doanh thu thuần toàn kỳ",gross-promotion-allReturned)};
            if(filtered.Length==0) return Build(metrics,["Lý do","Số phiếu","Số lượng","Giá trị hàng trả","Tiền hoàn","Sản phẩm bị trả nhiều"],[],[]);
            var rowsMain=groups.Select(g=>R("reason:"+g.Key,Reasons.GetValueOrDefault(g.Key,g.Key),N(g.Select(x=>x.Id).Distinct().Count()),
                N(g.Sum(x=>x.Quantity)),N(g.Sum(x=>x.Value)),N(g.Sum(x=>x.CashRefund)),
                string.Join("; ",g.GroupBy(x=>x.ProductId).OrderByDescending(p=>p.Sum(x=>x.Quantity)).ThenBy(p=>p.Key).Take(3)
                    .Select(p=>$"{p.First().ProductName} × {p.Sum(x=>x.Quantity)}"))))
                .Append(R("total","Tổng cộng",N(filtered.Select(x=>x.Id).Distinct().Count()),N(filtered.Sum(x=>x.Quantity)),N(totalValue),N(filtered.Sum(x=>x.CashRefund)),"" )).ToArray();
            return Build(metrics,["Lý do","Số phiếu","Số lượng","Giá trị hàng trả","Tiền hoàn","Sản phẩm bị trả nhiều"],rowsMain,
                groups.Select(g=>new ReportChartPoint(Reasons.GetValueOrDefault(g.Key,g.Key),g.Sum(x=>x.Quantity))).ToArray());
        }

        if(code=="BC-10")
        {
            var customers=await source.CustomersAsync(from,to,ct);var points=await source.PointsAsync(from,to,ct);
            var ids=customers.Select(x=>x.Id).Concat(sales.Where(x=>x.CustomerId.HasValue).Select(x=>x.CustomerId!.Value))
                .Concat(returns.Where(x=>x.CustomerId.HasValue).Select(x=>x.CustomerId!.Value)).Concat(points.Select(x=>x.CustomerId)).Distinct().ToArray();
            long Earned(IEnumerable<WeekPoints> items)=>items.Where(x=>x.Kind=="TICH_DIEM").Sum(x=>(long)x.Change);
            long Recovered(IEnumerable<WeekPoints> items)=>-items.Where(x=>x.Kind.StartsWith("THU_HOI_",StringComparison.Ordinal)).Sum(x=>(long)x.Change);
            var metrics=new[]{M("Khách mới",customers.Count,"number"),M("Khách có mua",sales.Where(x=>x.CustomerId.HasValue).Select(x=>x.CustomerId).Distinct().Count(),"number"),
                M("Tỷ lệ đơn gắn khách",Rate(sales.Count(x=>x.CustomerId.HasValue),sales.Count),"percent"),M("Điểm đã cộng",Earned(points),"number"),
                M("Điểm đã thu hồi",Recovered(points),"number"),M("Điều chỉnh điểm",points.Where(x=>x.Kind=="DIEU_CHINH").Sum(x=>(long)x.Change),"number")};
            if(detailKey is not null)
            {
                if(!detailKey.StartsWith("customer:",StringComparison.Ordinal)||!long.TryParse(detailKey[9..],out var id)||!ids.Contains(id))
                    return Build([],[],[],[],"Dòng báo cáo không hợp lệ.");
                var rows=sales.Where(x=>x.CustomerId==id).Select(x=>R($"invoice:{x.Id}",x.Code,"Bán hàng",Dt(x.At),N(x.Net),""))
                    .Concat(returns.Where(x=>x.CustomerId==id).GroupBy(x=>x.Id).Select(g=>R($"return:{g.Key}",g.First().Code,"Hàng trả",Dt(g.First().At),N(-g.Sum(x=>x.Value)),"")))
                    .Concat(points.Where(x=>x.CustomerId==id).Select(x=>R($"points:{x.Id}",$"ĐIỂM-{x.Id}",x.Kind,Dt(x.At),"",N(x.Change)))).ToArray();
                return Build([], ["Chứng từ","Loại","Thời điểm (UTC+7)","Giá trị","Điểm"],rows,[]);
            }
            var data=ids.Select(id=>
            {
                var purchases=sales.Where(x=>x.CustomerId==id).ToArray();var refunds=returns.Where(x=>x.CustomerId==id).Sum(x=>x.Value);
                var events=points.Where(x=>x.CustomerId==id).ToArray();
                var name=customers.FirstOrDefault(x=>x.Id==id)?.Name ?? purchases.FirstOrDefault()?.CustomerName
                    ?? points.FirstOrDefault(x=>x.CustomerId==id)?.Name ?? returns.First(x=>x.CustomerId==id).CustomerName;
                return new {Id=id,Name=name,Count=purchases.Length,Gross=purchases.Sum(x=>x.Gross),Deductions=purchases.Sum(x=>x.Discount)+refunds,
                    Net=purchases.Sum(x=>x.Net)-refunds,Earned=Earned(events),Recovered=Recovered(events)};
            }).OrderByDescending(x=>x.Net).ThenBy(x=>x.Id).ToArray();
            var rowsMain=data.Select(x=>R($"customer:{x.Id}",$"KH-{x.Id:000000}",x.Name,N(x.Count),N(x.Gross),N(x.Deductions),N(x.Net),N(x.Earned),N(x.Recovered))).ToList();
            if(data.Length>0) rowsMain.Add(R("total","Tổng cộng","",N(data.Sum(x=>x.Count)),N(data.Sum(x=>x.Gross)),N(data.Sum(x=>x.Deductions)),N(data.Sum(x=>x.Net)),N(data.Sum(x=>x.Earned)),N(data.Sum(x=>x.Recovered))));
            return Build(metrics,["Mã khách","Khách hàng","Hóa đơn","Doanh thu gộp","Giảm trừ","Chi tiêu thuần","Điểm cộng","Điểm thu hồi"],rowsMain,
                data.OrderByDescending(x=>x.Net).Take(10).Select(x=>new ReportChartPoint(x.Name,x.Net)).ToArray());
        }

        var uses=await source.UsesAsync(from,to,ct);
        string PromotionName(long id)=>uses.FirstOrDefault(x=>x.PromotionId==id)?.PromotionName
            ?? returns.FirstOrDefault(x=>x.PromotionNames?.ContainsKey(id)==true)?.PromotionNames?[id] ?? $"KM #{id}";
        var selectedUses=uses.Where(x=>query.PromotionId is null||x.PromotionId==query.PromotionId).ToArray();
        var promoReturns=returns.Where(x=>x.Promotions.Count>0 && (query.PromotionId is null || x.Promotions.Contains(query.PromotionId.Value))).ToArray();
        var promotionIds=selectedUses.Select(x=>x.PromotionId).Concat(promoReturns.SelectMany(x=>x.Promotions).Where(x=>query.PromotionId is null||x==query.PromotionId)).Distinct().Order().ToArray();
        if(detailKey is not null)
        {
            if(!detailKey.StartsWith("promotion:",StringComparison.Ordinal)||!long.TryParse(detailKey[10..],out var id)||!promotionIds.Contains(id))
                return Build([],[],[],[],"Dòng báo cáo không hợp lệ.");
            var rows=selectedUses.Where(x=>x.PromotionId==id).Select(x=>R($"invoice:{x.InvoiceId}",x.InvoiceCode,"Sử dụng",Dt(x.PaidAt),x.CashierName,x.CustomerName,N(x.Discount),N(x.InvoiceNet)))
                .Concat(promoReturns.Where(x=>x.Promotions.Contains(id)).GroupBy(x=>x.Id).Select(g=>R($"return:{g.Key}",g.First().Code,"Hàng trả",Dt(g.First().At),g.First().CashierName,g.First().CustomerName,"",N(-g.Sum(x=>x.Value))))).ToArray();
            return Build([], ["Chứng từ","Loại","Thời điểm (UTC+7)","Thu ngân","Khách","Giảm giá CT","Giá trị"],rows,[]);
        }
        var usedInvoices=selectedUses.Select(x=>x.InvoiceId).ToHashSet();var allPromoInvoices=uses.Select(x=>x.InvoiceId).ToHashSet();
        var withPromo=sales.Where(x=>usedInvoices.Contains(x.Id)).ToArray();var noPromo=sales.Where(x=>!allPromoInvoices.Contains(x.Id)).ToArray();
        var grossPromo=withPromo.Sum(x=>x.Gross);var deductionsPromo=withPromo.Sum(x=>x.Discount)+promoReturns.Sum(x=>x.Value);
        var metricsPromo=new[]{M("Lượt dùng chương trình",selectedUses.Length,"number"),M("Tổng giảm của CT",selectedUses.Sum(x=>x.Discount)),
            M("Đơn có KM (không trùng)",usedInvoices.Count,"number"),M("Doanh thu gộp đơn có KM",grossPromo),M("Giảm trừ đơn có KM",deductionsPromo),
            M("Doanh thu thuần đơn có KM",grossPromo-deductionsPromo),M("TB đơn có KM khi bán",withPromo.Length==0?0:(long)Math.Round(withPromo.Average(x=>(decimal)x.Net))),
            M("TB đơn không KM khi bán",noPromo.Length==0?0:(long)Math.Round(noPromo.Average(x=>(decimal)x.Net)))};
        var rowsPromo=promotionIds.Select(id=>
        {
            var u=selectedUses.Where(x=>x.PromotionId==id).ToArray();var ids=u.Select(x=>x.InvoiceId).ToHashSet();
            var refunded=promoReturns.Where(x=>x.Promotions.Contains(id)).Sum(x=>x.Value);
            return R($"promotion:{id}",PromotionName(id),N(u.Length),N(u.Sum(x=>x.Discount)),
                N(sales.Where(x=>ids.Contains(x.Id)).Sum(x=>x.Net)-refunded));
        }).ToList();
        if(rowsPromo.Count>0) rowsPromo.Add(R("total","Tổng cộng (đơn không trùng)",N(selectedUses.Length),N(selectedUses.Sum(x=>x.Discount)),N(grossPromo-deductionsPromo)));
        return Build(metricsPromo,["Chương trình","Lượt dùng","Tiền giảm của CT","Doanh thu thuần đơn có CT"],rowsPromo,
            promotionIds.Select(id=>new ReportChartPoint(PromotionName(id),selectedUses.Count(x=>x.PromotionId==id))).ToArray());
    }
}

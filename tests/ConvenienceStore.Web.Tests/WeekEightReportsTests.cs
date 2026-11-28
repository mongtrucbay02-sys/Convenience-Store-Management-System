using System.IO.Compression;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Modules.Reports.Models;
using ConvenienceStore.Web.Modules.Reports.Services;

namespace ConvenienceStore.Web.Tests;

public sealed class WeekEightReportsTests
{
    private static readonly DateTime At=new(2026,11,23,10,0,0);
    private static ReportQuery Query()=>new(){Period="custom",From=new(2026,11,23),To=new(2026,11,23)};
    private sealed class Data : IWeekEightReportDataSource
    {
        public IReadOnlyList<WeekSale> Sales {get;set;}=[];
        public IReadOnlyList<WeekReturn> Returns {get;set;}=[];
        public IReadOnlyList<WeekCustomer> Customers {get;set;}=[];
        public IReadOnlyList<WeekPoints> Points {get;set;}=[];
        public IReadOnlyList<PromotionUse> Uses {get;set;}=[];
        public int Reads {get;private set;}
        public Task<IReadOnlyList<WeekSale>> SalesAsync(DateTime from,DateTime to,CancellationToken ct){Reads++;return Task.FromResult(Sales);}
        public Task<IReadOnlyList<WeekReturn>> ReturnsAsync(DateTime from,DateTime to,CancellationToken ct)=>Task.FromResult(Returns);
        public Task<IReadOnlyList<WeekCustomer>> CustomersAsync(DateTime from,DateTime to,CancellationToken ct)=>Task.FromResult(Customers);
        public Task<IReadOnlyList<WeekPoints>> PointsAsync(DateTime from,DateTime to,CancellationToken ct)=>Task.FromResult(Points);
        public Task<IReadOnlyList<PromotionUse>> UsesAsync(DateTime from,DateTime to,CancellationToken ct)=>Task.FromResult(Uses);
    }
    private static long Metric(OperationalReportResponse r,string label)=>r.Metrics.Single(x=>x.Label==label).Value;
    private static WeekEightReportsService Service(Data data)=>new(data,TimeProvider.System);
    private static Data Sample()=>new() {
        Sales=[new(1,"HD-1",At,1,"Khách A","TN",100000,10000),new(2,"HD-2",At,null,"Khách lẻ","TN",20000,0)],
        Customers=[new(1,"Khách A",At),new(2,"Khách mới",At)],
        Returns=[new(1,"PT-1",1,At,"TRA_HANG",5,"TN",1,"Khách A",10,"Sữa","LOI_SAN_PHAM",1,10000,10000,[1,2]),
            new(1,"PT-1",1,At,"TRA_HANG",5,"TN",1,"Khách A",11,"Bánh","KHAC",1,20000,20000,[1,2])],
        Points=[new(1,1,"Khách A",At,"TICH_DIEM",9,1),new(2,1,"Khách A",At,"THU_HOI_TRA_HANG",-3,1),new(3,1,"Khách A",At,"DIEU_CHINH",2,null),
            new(4,1,"Khách A",At,"THU_HOI_HUY_HOA_DON",-1,99)],
        Uses=[new(1,"KM A",1,"HD-1",At,"TN","Khách A",6000,90000),new(2,"KM B",1,"HD-1",At,"TN","Khách A",4000,90000)] };

    [Theory]
    [InlineData("BC-09",false)] [InlineData("BC-10",false)] [InlineData("BC-11",false)]
    [InlineData("BC-09",true)] [InlineData("BC-10",true)] [InlineData("BC-11",true)]
    public async Task InvalidRangesAreRejectedBeforeQuery(string code,bool tooLong)
    {
        var data=new Data();var q=Query();q.From=new(2025,1,1);q.To=tooLong?new(2026,1,2):new(2024,12,31);
        var r=await Service(data).GetAsync(code,q,default);
        Assert.NotNull(r.Message);Assert.Empty(r.Rows);Assert.Equal(0,data.Reads);
    }
    [Theory] [InlineData("BC-09")] [InlineData("BC-10")] [InlineData("BC-11")]
    public async Task EmptyPeriodHasExactMessageAndNoFakeTotal(string code)
    {
        var r=await Service(new()).GetAsync(code,Query(),default);
        Assert.Equal("Không có dữ liệu trong khoảng đã chọn",r.Message);Assert.Empty(r.Rows);Assert.Empty(r.Chart);
    }
    [Fact]
    public async Task ReturnReasonsCountOneSlipOnceInTotalAndFilterDetails()
    {
        var s=Service(Sample());var r=await s.GetAsync("BC-09",Query(),default);
        Assert.Equal(1,Metric(r,"Số phiếu trả"));Assert.Equal(30000,Metric(r,"Tiền hoàn"));
        Assert.Equal("1",r.Rows.Single(x=>x.Key=="total").Cells[1]);Assert.Equal(2,r.Chart.Sum(x=>x.Value));
        var q=Query();q.ReturnReason="KHAC";q.CashierId=5;
        var detail=await s.GetAsync("BC-09",q,default,"reason:KHAC");Assert.Single(detail.Rows);Assert.Equal("Bánh",detail.Rows[0].Cells[4]);
        q.CashierId=9;Assert.Empty((await s.GetAsync("BC-09",q,default)).Rows);
        var more=Sample();more.Returns=more.Returns.Append(more.Returns[0] with {Id=2,Reason="KHAC",Quantity=2}).ToArray();
        Assert.Equal(3,Metric(await Service(more).GetAsync("BC-09",Query(),default),"Trả nhiều nhất: Sữa"));
    }
    [Fact]
    public async Task CustomerReportUsesLedgerEventsAndKeepsNewCustomersWithoutSales()
    {
        var r=await Service(Sample()).GetAsync("BC-10",Query(),default);
        Assert.Equal(2,Metric(r,"Khách mới"));Assert.Equal(1,Metric(r,"Khách có mua"));Assert.Equal(500,Metric(r,"Tỷ lệ đơn gắn khách"));
        Assert.Equal(9,Metric(r,"Điểm đã cộng"));Assert.Equal(4,Metric(r,"Điểm đã thu hồi"));Assert.Equal(2,Metric(r,"Điều chỉnh điểm"));
        Assert.Equal("60.000",r.Rows.Single(x=>x.Key=="customer:1").Cells[5]);
        Assert.Equal("0",r.Rows.Single(x=>x.Key=="customer:2").Cells[2]);
    }
    [Fact]
    public async Task ExchangeValueReducesRevenueWithoutInventingCashRefund()
    {
        var data=Sample();data.Returns=data.Returns.Select(x=>x with {Kind="DOI_HANG",CashRefund=0}).ToArray();
        var r=await Service(data).GetAsync("BC-09",Query(),default);
        Assert.Equal(1,Metric(r,"Số phiếu đổi"));Assert.Equal(0,Metric(r,"Tiền hoàn"));Assert.Equal(30000,Metric(r,"Giá trị hàng trả"));
        Assert.Equal(80000,Metric(r,"Doanh thu thuần toàn kỳ"));
    }
    [Fact]
    public async Task MultipleCampaignsDoNotDoubleCountInvoiceRevenueOrRefundsInTotal()
    {
        var s=Service(Sample());var r=await s.GetAsync("BC-11",Query(),default);
        Assert.Equal(2,Metric(r,"Lượt dùng chương trình"));Assert.Equal(1,Metric(r,"Đơn có KM (không trùng)"));
        Assert.Equal(10000,Metric(r,"Tổng giảm của CT"));Assert.Equal(60000,Metric(r,"Doanh thu thuần đơn có KM"));
        Assert.Equal(90000,Metric(r,"TB đơn có KM khi bán"));Assert.Equal(20000,Metric(r,"TB đơn không KM khi bán"));
        Assert.Equal("60.000",r.Rows.Single(x=>x.Key=="total").Cells[3]);
        var q=Query();q.PromotionId=1;
        var filtered=await s.GetAsync("BC-11",q,default);Assert.Equal(6000,Metric(filtered,"Tổng giảm của CT"));
        Assert.Single(filtered.Chart);Assert.Equal(2,(await s.GetAsync("BC-11",q,default,"promotion:1")).Rows.Count);
    }
    [Fact]
    public async Task ReturnFromPriorSalePeriodStillReducesCurrentCustomerAndCampaignRevenue()
    {
        var data=Sample();data.Sales=[];data.Uses=[];data.Customers=[];data.Points=[];
        var s=Service(data);var customer=await s.GetAsync("BC-10",Query(),default);
        Assert.Equal("-30.000",customer.Rows.Single(x=>x.Key=="customer:1").Cells[5]);
        Assert.Equal(-30000,Metric(await s.GetAsync("BC-11",Query(),default),"Doanh thu thuần đơn có KM"));
    }
    [Theory] [InlineData("BC-09")] [InlineData("BC-10")] [InlineData("BC-11")]
    public async Task EveryNewReportHasAlignedRowsTotalAndRealExcelPackage(string code)
    {
        var query=Query();query.ReturnReason="KHAC";query.PromotionId=1;
        var r=await Service(Sample()).GetAsync(code,query,default);
        Assert.All(r.Rows,row=>Assert.Equal(r.Columns.Count,row.Cells.Count));Assert.Single(r.Rows,x=>x.Key=="total");
        var bytes=ReportExcelExporter.Create(r,"Smart Mart","Admin",DateTimeOffset.UtcNow);
        using var zip=new ZipArchive(new MemoryStream(bytes));
        using var sheet=new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml=await sheet.ReadToEndAsync();
        Assert.Contains("Chương trình ID=1",xml);Assert.Contains("Lý do trả=KHAC",xml);
        foreach(var metric in r.Metrics.Where(x=>x.Format=="percent"))
            Assert.Contains((metric.Value/10m).ToString("N1",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+"%",xml);
    }
}

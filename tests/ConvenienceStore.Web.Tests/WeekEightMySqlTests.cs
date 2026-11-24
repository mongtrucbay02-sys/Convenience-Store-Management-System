using System.Net;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Promotions.Services;
using ConvenienceStore.Web.Modules.Reports.Models;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.HaTang;
using ConvenienceStore.Web.Tests.Inventory;
using ConvenienceStore.Web.Tests.Modules.Products;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ConvenienceStore.Web.Tests;

public sealed class WeekEightMySqlTests(CsdlKhoFixture db) : IClassFixture<CsdlKhoFixture>
{
    private static int sequence;
    private sealed record Seeded(long Promo,long OtherPromo,long Unused,long Invoice,long Cancelled,long Return,long Customer,DateTime At)
    {
        public ReportQuery Query=>new(){Period="custom",From=DateOnly.FromDateTime(At),To=DateOnly.FromDateTime(At)};
        public string Url=>"Period=custom&From="+At.ToString("yyyy-MM-dd")+"&To="+At.ToString("yyyy-MM-dd");
    }
    private async Task<long> Scalar(string sql,params (string,object?)[] parameters)=>Convert.ToInt64(await db.DocMotGiaTriAsync(sql,parameters));
    private async Task<Seeded> Seed(bool returnNextDay=false)
    {
        var n=Interlocked.Increment(ref sequence);var at=new DateTime(2026,1,1,12,0,0).AddDays(n*3);
        var returnAt=returnNextDay?at.AddDays(1):at;
        var customer=await Scalar("INSERT INTO khach_hang(ho_ten,so_dien_thoai,ngay_tao,nguoi_tao_id) VALUES('Khách tuần 8',@phone,@at,@actor); SELECT LAST_INSERT_ID();",
            ("@phone","09"+n.ToString("D8")),("@at",at),("@actor",db.TaiKhoanAdminId));
        var first=await new DuLieuTestKho(db).TaoSanPhamAsync("W8A");var second=await new DuLieuTestKho(db).TaoSanPhamAsync("W8B");
        async Task<long> Promotion()=>await Scalar("""
            INSERT INTO chuong_trinh_khuyen_mai(ma,ten,loai_giam,gia_tri,bat_dau_luc,ket_thuc_luc,nguoi_tao_id)
            VALUES(@code,'Ưu đãi tuần 8','PHAN_TRAM',10,'2025-01-01','2027-01-01',@actor); SELECT LAST_INSERT_ID();
            """,("@code","W8-"+Guid.NewGuid().ToString("N")[..8]),("@actor",db.TaiKhoanAdminId));
        var promo=await Promotion();var other=await Promotion();var unused=await Promotion();
        async Task<long> Invoice(long? kh,bool guest=false)
        {
            var cart=await Scalar("INSERT INTO gio_hang(thu_ngan_id,trang_thai,khach_hang_id) VALUES(@tn,'DA_HOAN_TAT',@kh); SELECT LAST_INSERT_ID();",("@tn",db.TaiKhoanThuNganId),("@kh",kh));
            var invoice=await Scalar("""
                INSERT INTO hoa_don(ma_hoa_don,gio_hang_id,thu_ngan_id,khach_hang_id,thanh_toan_luc,tong_tien_hang,tong_giam,tong_thanh_toan,tien_khach_dua,tien_thua)
                VALUES(@code,@cart,@tn,@kh,@at,@gross,@discount,@net,@net,0); SELECT LAST_INSERT_ID();
                """,("@code","HDW8-"+Guid.NewGuid().ToString("N")[..12]),("@cart",cart),("@tn",db.TaiKhoanThuNganId),("@kh",kh),("@at",at),
                ("@gross",guest?10000:250000),("@discount",guest?0:25000),("@net",guest?10000:225000));
            await db.ChayAsync("""
                INSERT INTO ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,giam_don_vi,gia_von,khuyen_mai_id)
                VALUES(@id,@sp,'Tên snapshot A','Hộp',@qty,@price,@discount,5000,@promo)
                """,("@id",invoice),("@sp",first),("@qty",guest?1:2),("@price",guest?10000:100000),("@discount",guest?0:10000),("@promo",guest?null:promo));
            if(!guest) await db.ChayAsync("""
                INSERT INTO ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,giam_don_vi,gia_von,khuyen_mai_id)
                VALUES(@id,@sp,'Tên snapshot B','Hộp',1,50000,5000,5000,@promo)
                """,("@id",invoice),("@sp",second),("@promo",other));
            return invoice;
        }
        var paid=await Invoice(customer);var cancelled=await Invoice(customer);await Invoice(null,true);
        await db.ChayAsync("UPDATE hoa_don SET trang_thai='DA_HUY',nguoi_huy_id=@actor,huy_luc=@at,ly_do_huy='Test void' WHERE id=@id",
            ("@actor",db.TaiKhoanAdminId),("@at",at),("@id",cancelled));
        var returned=await Scalar("""
            INSERT INTO phieu_doi_tra(ma_phieu,loai,hoa_don_goc_id,khach_hang_id,chinh_sach_id,trang_thai,gia_tri_hang_tra,tien_hoan,thu_ngan_id,hoan_tat_luc)
            VALUES(@code,'TRA_HANG',@hd,@kh,(SELECT id FROM chinh_sach_doi_tra ORDER BY id LIMIT 1),'HOAN_TAT',135000,135000,@tn,@at); SELECT LAST_INSERT_ID();
            """,("@code","PTW8-"+Guid.NewGuid().ToString("N")[..12]),("@hd",paid),("@kh",customer),("@tn",db.TaiKhoanThuNganId),("@at",returnAt));
        await db.ChayAsync("""
            INSERT INTO ct_phieu_doi_tra(phieu_doi_tra_id,san_pham_id,hoa_don_id,so_luong,don_gia_thuc_tra,ly_do,ghi_chu_ly_do,tinh_trang,cach_xu_ly,gia_von)
            VALUES(@pt,@a,@hd,1,90000,'LOI_SAN_PHAM',NULL,'NGUYEN_VEN','NHAP_LAI',5000),(@pt,@b,@hd,1,45000,'KHAC','Lý do kiểm thử','NGUYEN_VEN','NHAP_LAI',5000);
            INSERT INTO lich_su_diem(khach_hang_id,loai,so_diem,diem_sau,hoa_don_id,phieu_doi_tra_id,ly_do,nguoi_thuc_hien_id,thoi_diem)
            VALUES(@kh,'TICH_DIEM',22,22,@hd,NULL,NULL,@actor,@at),(@kh,'THU_HOI_TRA_HANG',-13,9,@hd,@pt,NULL,@actor,@returnAt),
              (@kh,'DIEU_CHINH',2,11,NULL,NULL,'Bù điểm',@actor,@returnAt);
            """,("@pt",returned),("@a",first),("@b",second),("@hd",paid),("@kh",customer),("@actor",db.TaiKhoanAdminId),("@at",at),("@returnAt",returnAt));
        return new(promo,other,unused,paid,cancelled,returned,customer,at);
    }
    private WeekEightReportsService Service(ConvenienceStore.Web.Data.CuaHangDbContext context)=>
        new(new MySqlWeekEightReportDataSource(context,new PromotionUsageStore(context)),TimeProvider.System);
    private static long Metric(OperationalReportResponse report,string label)=>report.Metrics.Single(x=>x.Label==label).Value;

    [Fact]
    public async Task ReturnsAndPointRecoveryBelongToTheirCompletionDayNotOriginalSaleDay()
    {
        var seed=await Seed(true);await using var context=db.TaoDbContext();var service=Service(context);
        Assert.Equal(0,Metric(await service.GetAsync("BC-09",seed.Query,default),"Tiền hoàn"));
        var next=seed.Query;next.From=next.To=DateOnly.FromDateTime(seed.At.AddDays(1));
        var returned=await service.GetAsync("BC-09",next,default);Assert.Equal(135000,Metric(returned,"Tiền hoàn"));
        var customer=await service.GetAsync("BC-10",next,default);Assert.Equal(0,Metric(customer,"Điểm đã cộng"));Assert.Equal(13,Metric(customer,"Điểm đã thu hồi"));
        var promo=await service.GetAsync("BC-11",next,default);Assert.Equal(-135000,Metric(promo,"Doanh thu thuần đơn có KM"));
        Assert.Contains(promo.Rows,x=>x.Key==$"promotion:{seed.Promo}" && x.Cells[0]=="Ưu đãi tuần 8");
        var data=new MySqlWeekEightReportDataSource(context,new PromotionUsageStore(context));
        var facts=await data.ReturnsAsync(seed.At.Date.AddDays(1),seed.At.Date.AddDays(2),default);
        Assert.Equal(new long[]{90000,45000},facts.OrderBy(x=>x.CashRefund).Reverse().Select(x=>x.CashRefund));
    }

    [Fact]
    public async Task UsageUsesSnapshotDiscountExcludesCancelledAndDeletePreservesReferences()
    {
        var seed=await Seed();await using var context=db.TaoDbContext();var store=new PromotionUsageStore(context);
        var history=await store.HistoryAsync(seed.Promo,default);Assert.Single(history.Uses);Assert.Equal(20000,history.TotalDiscount);
        Assert.Equal(seed.Invoice,history.Uses[0].InvoiceId);
        Assert.Equal(409,(await Assert.ThrowsAsync<LoiNghiepVu>(()=>store.DeleteAsync(seed.Promo,default))).MaHttp);
        await db.ChayAsync("UPDATE hoa_don SET trang_thai='DA_HUY',nguoi_huy_id=@actor,huy_luc=@at,ly_do_huy='Test' WHERE id=@id",
            ("@actor",db.TaiKhoanAdminId),("@at",seed.At),("@id",seed.Invoice));
        Assert.Empty((await store.HistoryAsync(seed.Promo,default)).Uses);
        Assert.Equal(409,(await Assert.ThrowsAsync<LoiNghiepVu>(()=>store.DeleteAsync(seed.Promo,default))).MaHttp);
        Assert.Equal(2,await Scalar("SELECT COUNT(*) FROM ct_hoa_don WHERE khuyen_mai_id=@id",("@id",seed.Promo)));
    }
    [Fact]
    public async Task UnusedPromotionCanBeDeletedAndMissingPromotionReturns404()
    {
        var seed=await Seed();await using var context=db.TaoDbContext();var store=new PromotionUsageStore(context);
        Assert.Empty((await store.HistoryAsync(seed.Unused,default)).Uses);await store.DeleteAsync(seed.Unused,default);
        Assert.Equal(404,(await Assert.ThrowsAsync<LoiNghiepVu>(()=>store.HistoryAsync(seed.Unused,default))).MaHttp);
    }
    [Fact]
    public async Task LiveReportsReconcileMoneyPointsReasonsAndMultiCampaignTotals()
    {
        var seed=await Seed();await using var context=db.TaoDbContext();var service=Service(context);
        var returned=await service.GetAsync("BC-09",seed.Query,default);
        Assert.Equal(1,Metric(returned,"Số phiếu trả"));Assert.Equal(135000,Metric(returned,"Tiền hoàn"));Assert.Equal(100000,Metric(returned,"Doanh thu thuần toàn kỳ"));
        Assert.Equal("1",returned.Rows.Single(x=>x.Key=="total").Cells[1]);Assert.Equal(2,returned.Chart.Sum(x=>x.Value));
        var customers=await service.GetAsync("BC-10",seed.Query,default);
        Assert.Equal(1,Metric(customers,"Khách có mua"));Assert.Equal(22,Metric(customers,"Điểm đã cộng"));Assert.Equal(13,Metric(customers,"Điểm đã thu hồi"));
        Assert.Equal("90.000",customers.Rows.Single(x=>x.Key==$"customer:{seed.Customer}").Cells[5]);
        var promotions=await service.GetAsync("BC-11",seed.Query,default);
        Assert.Equal(2,Metric(promotions,"Lượt dùng chương trình"));Assert.Equal(1,Metric(promotions,"Đơn có KM (không trùng)"));
        Assert.Equal(90000,Metric(promotions,"Doanh thu thuần đơn có KM"));Assert.Equal(25000,Metric(promotions,"Tổng giảm của CT"));
    }

    private Task<Microsoft.AspNetCore.Builder.WebApplication> Host()=>ListFeatureHost.Start(db,s=>
    {
        s.AddHttpContextAccessor();s.AddModuleInventory();s.AddSingleton(TimeProvider.System);
        s.AddAuthorization(o=>o.AddPolicy("AdminOnly",p=>p.RequireAuthenticatedUser().RequireRole("AD")));
        s.AddDataProtection().UseEphemeralDataProtectionProvider();
        s.AddControllersWithViews().AddRazorOptions(o=>o.AreaViewLocationFormats.Insert(0,"/Modules/{2}/Views/{1}/{0}.cshtml"));
        s.AddScoped<PromotionUsageStore>();s.AddScoped<IWeekEightReportDataSource,MySqlWeekEightReportDataSource>();s.AddScoped<WeekEightReportsService>();
        s.AddScoped<ReportFilterService>();s.AddScoped<IOperationalReportDataSource,MySqlTransactionalReportsDataSource>();
        s.AddScoped<IOperationalReportsService,OperationalReportsService>();s.AddScoped<IDashboardDataSource,MySqlDashboardDataSource>();s.AddScoped<IDashboardService,DashboardService>();
        s.AddScoped<IPromotionService,PromotionService>();s.AddScoped<IPromotionDataSource,MySqlPromotionDataSource>();s.AddScoped<IProductLookup,MySqlProductLookup>();
    });
    [Theory] [InlineData(null,401)] [InlineData("TN",403)] [InlineData("NK",403)] [InlineData("FORCED",403)]
    public async Task OnlyAdminCanAccessEveryWeekEightEndpoint(string? actor,int status)
    {
        await using var app=await Host();using var client=await ListFeatureHost.Client(app,db,actor);
        foreach(var path in new[]{"/api/reports/returns","/api/reports/customers","/api/reports/promotions","/api/promotions/1/usage",
            "/Reports/Reports/Returns","/Reports/Reports/Customers","/Reports/Reports/Promotions","/Promotions/Promotions/Usage/1","/Promotions/Promotions/Delete/1"})
            Assert.Equal(status,(int)(await client.GetAsync(path)).StatusCode);
        Sp08CategoryHttpTests.AddXsrf(client);Assert.Equal(status,(int)(await client.DeleteAsync("/api/promotions/1")).StatusCode);
    }
    [Fact]
    public async Task AdminPagesDetailsExportsCsrfAndApiErrorsWorkAgainstRealMySql()
    {
        var seed=await Seed();await using var app=await Host();using var client=await ListFeatureHost.Client(app,db,"AD");
        foreach(var report in new[]{("Returns","BC-09","reason:KHAC"),("Customers","BC-10",$"customer:{seed.Customer}"),("Promotions","BC-11",$"promotion:{seed.Promo}")})
        {
            var html=await client.GetStringAsync($"/Reports/Reports/{report.Item1}?{seed.Url}");Assert.Contains("Biểu đồ cột",html);Assert.Contains("Xuất Excel",html);
            Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/Reports/Reports/Detail?code={report.Item2}&key={report.Item3}&{seed.Url}")).StatusCode);
            var export=await client.GetAsync($"/Reports/Reports/Export?code={report.Item2}&{seed.Url}");Assert.Equal(HttpStatusCode.OK,export.StatusCode);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",export.Content.Headers.ContentType?.MediaType);
        }
        Assert.Contains("Lịch sử sử dụng",await client.GetStringAsync($"/Promotions/Promotions/Usage/{seed.Promo}"));
        Assert.Contains("__RequestVerificationToken",await client.GetStringAsync($"/Promotions/Promotions/Delete/{seed.Unused}"));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.DeleteAsync($"/api/promotions/{seed.Unused}")).StatusCode);
        Sp08CategoryHttpTests.AddXsrf(client);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync($"/api/promotions/{seed.Unused}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync($"/api/promotions/{seed.Promo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/api/promotions/4294967295/usage")).StatusCode);
        foreach(var report in new[]{"returns","customers","promotions"})
        {
            Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync($"/api/reports/{report}?Period=custom&From=2026-02-02&To=2026-01-01")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync($"/api/reports/{report}?Period=custom&From=2025-01-01&To=2026-02-01")).StatusCode);
        }
    }
}

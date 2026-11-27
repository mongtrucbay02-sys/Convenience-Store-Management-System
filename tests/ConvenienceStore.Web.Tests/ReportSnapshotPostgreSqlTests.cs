using ConvenienceStore.Web.Modules.Reports.Models;
using ConvenienceStore.Web.Modules.Reports.Services;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests;

public sealed class ReportSnapshotPostgreSqlTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    private static long sequence;
    private static readonly DateTimeOffset At = new(2026, 10, 4, 10, 0, 0, TimeSpan.FromHours(7));
    private sealed record Fixture(long Actor, long CategoryA, long CategoryB, long Supplier,
        long ProductA, long ProductB, long Invoice, long Receipt, long Return, long WriteOff, DateTimeOffset Time)
    {
        public ReportQuery Query => new() { Period = "custom", From = DateOnly.FromDateTime(Time.Date), To = DateOnly.FromDateTime(Time.Date) };
    }
    private async Task<Fixture> Seed(int quantity = 1, int returned = 0, bool destroyReturn = false,
        decimal costA = 10416.67m, decimal costB = 7000.25m)
    {
        var id = 5_000_010_000L + Interlocked.Increment(ref sequence) * 100;
        var f = new Fixture(id+1,id+2,id+3,id+4,id+5,id+6,id+8,id+10,id+11,id+12,At.AddDays(sequence));
        await database.ChayAsync("""
            INSERT INTO tai_khoan(id,ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
            VALUES(@actor,@code,'unused','Snapshot test',1,FALSE);
            INSERT INTO danh_muc(id,ten) VALUES(@ca,@namea),(@cb,@nameb);
            INSERT INTO nha_cung_cap(id,ma,ten,so_dien_thoai) VALUES(@supplier,@code,'Snapshot supplier',@phone);
            INSERT INTO san_pham(id,ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id)
            VALUES(@a,@namea,'Product A',@ca,'Hộp',@actor),(@b,@nameb,'Product B',@cb,'Hộp',@actor);
            INSERT INTO chuong_trinh_khuyen_mai(id,ma,ten,loai_giam,gia_tri,bat_dau_luc,ket_thuc_luc,nguoi_tao_id)
            VALUES(@promo,@code,'Only A discounted','PHAN_TRAM',20,'2026-01-01','2027-01-01',@actor);
            INSERT INTO gio_hang(id,thu_ngan_id,trang_thai) VALUES(@cart,@actor,'DA_HOAN_TAT');
            INSERT INTO hoa_don(id,ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,
                tong_tien_hang,tong_giam,tong_thanh_toan,tien_khach_dua,tien_thua)
            VALUES(@invoice,@code,@cart,@actor,@at,(@qty+1)*100000,@qty*20000,@qty*80000+100000,@qty*80000+100000,0);
            INSERT INTO ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,khuyen_mai_id,giam_don_vi,gia_von)
            VALUES(@invoice,@a,'Product A','Hộp',@qty,100000,@promo,20000,@costA),
                  (@invoice,@b,'Product B','Hộp',1,100000,NULL,0,@costB);
            INSERT INTO phieu_nhap(id,ma_phieu,nha_cung_cap_id,ngay_nhap,trang_thai,tong_tien,nguoi_lap_id,nguoi_xac_nhan_id,xac_nhan_luc)
            VALUES(@receipt,@code,@supplier,@date,'DA_NHAP_KHO',30000,@actor,@actor,@at);
            INSERT INTO ct_phieu_nhap(phieu_nhap_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,gia_nhap)
            VALUES(@receipt,@a,'Product A','Hộp',3,10000);
            INSERT INTO phieu_xuat_huy(id,ma_phieu,trang_thai,nguoi_lap_id,nguoi_duyet_id,duyet_luc)
            VALUES(@writeoff,@code,'DA_XUAT_HUY',@actor,@actor,@at);
            INSERT INTO ct_xuat_huy(phieu_xuat_huy_id,san_pham_id,so_luong,ly_do,gia_von_luc_duyet)
            VALUES(@writeoff,@a,3,'HONG',@costA);
            """, ("@actor",f.Actor),("@ca",f.CategoryA),("@cb",f.CategoryB),("@supplier",f.Supplier),
            ("@a",f.ProductA),("@b",f.ProductB),("@promo",id+9),("@cart",id+7),("@invoice",f.Invoice),
            ("@receipt",f.Receipt),("@writeoff",f.WriteOff),("@qty",quantity),("@at",f.Time),
            ("@date",DateOnly.FromDateTime(f.Time.Date)),("@code","R"+id),("@namea","A"+id),("@nameb","B"+id),
            ("@phone","09"+sequence.ToString("D8")),("@costA",costA),("@costB",costB));
        if (returned > 0)
            await database.ChayAsync("""
                INSERT INTO phieu_doi_tra(id,ma_phieu,loai,hoa_don_goc_id,chinh_sach_id,trang_thai,
                    gia_tri_hang_tra,tien_hoan,thu_ngan_id,hoan_tat_luc)
                VALUES(@return,@code,'TRA_HANG',@invoice,(SELECT MIN(id) FROM chinh_sach_doi_tra),'HOAN_TAT',
                    @qty*80000,@qty*80000,@actor,@at);
                INSERT INTO ct_phieu_doi_tra(phieu_doi_tra_id,san_pham_id,hoa_don_id,so_luong,don_gia_thuc_tra,ly_do,cach_xu_ly,gia_von)
                VALUES(@return,@a,@invoice,@qty,80000,'LOI_SAN_PHAM',@disposition,@costA);
                """,("@return",f.Return),("@code","R"+id),("@invoice",f.Invoice),("@actor",f.Actor),
                ("@at",f.Time),("@qty",returned),("@a",f.ProductA),("@disposition",destroyReturn?"HUY":"NHAP_LAI"),("@costA",costA));
        return f;
    }
    private static long Metric(OperationalReportResponse report,string name) => report.Metrics.Single(x=>x.Label==name).Value;

    [Fact]
    public async Task UnequalLineDiscountsMatchGeneratedInvoiceTotalsAcrossReportsAndDetails()
    {
        var f=await Seed();
        await using var db=database.TaoDbContext();
        var source=new PostgreSqlTransactionalReportsDataSource(db);
        var service=new OperationalReportsService(source,TimeProvider.System);
        var stored=await db.CtHoaDons.AsNoTracking().Where(x=>x.HoaDonId==f.Invoice)
            .ToDictionaryAsync(x=>x.SanPhamId,x=>x.ThanhTien);
        var products=await service.GetProductsAsync(f.Query,default);
        Assert.Equal("80.000 ₫",products.Rows.Single(x=>x.Key==$"product:{f.ProductA}").Cells[4]);
        Assert.Equal("100.000 ₫",products.Rows.Single(x=>x.Key==$"product:{f.ProductB}").Cells[4]);
        foreach(var (product,category,expected) in new[]{(f.ProductA,f.CategoryA,80000L),(f.ProductB,f.CategoryB,100000L)})
        {
            Assert.Equal(expected,stored[product]);
            var q=f.Query;q.CategoryId=category;q.ProductId=product;
            Assert.Equal(expected,Metric(await service.GetProductsAsync(q,default),"Doanh thu thuần"));
            Assert.Equal(expected,Metric(await service.GetCategoriesAsync(q,default),"Doanh thu thuần"));
            Assert.Equal(expected,Metric(await service.GetGrossProfitAsync(q,default),"Doanh thu thuần"));
            foreach(var (code,key) in new[]{("BC-03",$"product:{product}"),("BC-04",$"category:{category}"),
                ("BC-08","day:"+f.Time.ToString("yyyy-MM-dd"))})
            {
                var detail=await service.GetDetailsAsync(code,key,q,default);
                Assert.Equal(expected,Metric(detail,"Doanh thu thuần"));
                Assert.All(detail.Rows,row=>Assert.Equal(detail.Columns.Count,row.Cells.Count));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FractionalCostSurvivesLargeSalesReturnsAndWriteoffs(bool destroyed)
    {
        var f=await Seed(2001,1000,destroyed);
        await using var db=database.TaoDbContext();
        var source=new PostgreSqlTransactionalReportsDataSource(db);
        var from=new DateTimeOffset(f.Time.Date,TimeSpan.FromHours(7));
        var invoices=await source.GetPaidInvoicesAsync(from,from.AddDays(1),default);
        Assert.Equal(10416.67m,Convert.ToDecimal(Assert.Single(invoices).Lines.Single(x=>x.ProductId==f.ProductA).UnitCostSnapshot));
        var returns=await source.GetReturnsAsync(from,from.AddDays(1),default);
        Assert.Equal(10416.67m,Convert.ToDecimal(Assert.Single(returns).Lines[0].UnitCostSnapshot));
        var report=await new OperationalReportsService(source,TimeProvider.System).GetGrossProfitAsync(f.Query,default);
        var cost=(destroyed?2001m:1001m)*10416.67m+7000.25m;
        Assert.Equal((long)Math.Round(cost,0,MidpointRounding.AwayFromZero),Metric(report,"Giá vốn hàng bán"));
        Assert.Equal((long)Math.Round(80180000m-cost,0,MidpointRounding.AwayFromZero),Metric(report,"Lợi nhuận gộp"));
        Assert.Equal(31250,Metric(report,"Giá trị xuất hủy"));
        Assert.Equal(destroyed?10416670:0,Metric(report,"Hàng trả bị hủy"));
        var detail=await new OperationalReportsService(source,TimeProvider.System)
            .GetDetailsAsync("BC-08","day:"+f.Time.ToString("yyyy-MM-dd"),f.Query,default);
        Assert.Equal(Metric(report,"Giá vốn hàng bán"),Metric(detail,"Giá vốn hàng bán"));
        Assert.Equal(Metric(report,"Doanh thu thuần"),Metric(detail,"Doanh thu thuần"));
        Assert.Contains(detail.Rows,row=>row.Cells[8]=="10.416,67 ₫");
    }

    [Fact]
    public async Task PublishedWholeDongCostAndProfitReconcileAtHalfDong()
    {
        var f=await Seed(costA:10416.50m,costB:7000m);
        await using var db=database.TaoDbContext();
        var report=await new OperationalReportsService(new PostgreSqlTransactionalReportsDataSource(db),TimeProvider.System)
            .GetGrossProfitAsync(f.Query,default);
        Assert.Equal(17417,Metric(report,"Giá vốn hàng bán"));
        Assert.Equal(Metric(report,"Doanh thu thuần")-Metric(report,"Giá vốn hàng bán"),Metric(report,"Lợi nhuận gộp"));
    }

    [Fact]
    public async Task BigintIdsSurviveEveryTransactionalAdapterAndSupplierFilter()
    {
        var f=await Seed(2,1);
        await using var db=database.TaoDbContext();
        var source=new PostgreSqlTransactionalReportsDataSource(db);
        var from=new DateTimeOffset(f.Time.Date,TimeSpan.FromHours(7));var to=from.AddDays(1);
        Assert.Equal(f.CategoryA,Assert.Single(await source.GetPaidInvoicesAsync(from,to,default)).Lines.Single(x=>x.ProductId==f.ProductA).CategoryId);
        Assert.Equal(f.CategoryA,Assert.Single(await source.GetReturnsAsync(from,to,default)).Lines[0].CategoryId);
        Assert.Equal(f.Supplier,Assert.Single(await source.GetPurchaseReceiptsAsync(from,to,default)).SupplierId);
        Assert.Equal(f.CategoryA,Assert.Single(await source.GetWriteOffsAsync(from,to,default)).CategoryId);
        var q=f.Query;q.SupplierId=f.Supplier;q.ProductId=f.ProductA;
        var report=await new OperationalReportsService(source,TimeProvider.System).GetPurchasesAsync(q,default);
        Assert.Equal(30000,Metric(report,"Tổng giá trị nhập"));
        Assert.Equal(f.Supplier,report.Query.SupplierId);
        Assert.Contains(await new ReportFilterService(db).GetSuppliersAsync(f.Supplier,default),x=>x.Value==f.Supplier.ToString()&&x.Selected);
        q.SupplierId=f.Supplier+999;
        Assert.Equal(0,Metric(await new OperationalReportsService(source,TimeProvider.System).GetPurchasesAsync(q,default),"Tổng giá trị nhập"));
    }

    [Fact]
    public async Task HistoricalCategoryCurrentlyFollowsCatalogChangeAndHasNoInvoiceSnapshot()
    {
        var f=await Seed();
        await using var db=database.TaoDbContext();
        var source=new PostgreSqlTransactionalReportsDataSource(db);
        var service=new OperationalReportsService(source,TimeProvider.System);
        var q=f.Query;q.CategoryId=f.CategoryA;q.ProductId=f.ProductA;
        Assert.True(Metric(await service.GetCategoriesAsync(q,default),"Doanh thu thuần")>0);
        await database.ChayAsync("UPDATE san_pham SET danh_muc_id=@category WHERE id=@product",("@category",f.CategoryB),("@product",f.ProductA));
        Assert.Equal(0,Metric(await service.GetCategoriesAsync(q,default),"Doanh thu thuần"));
        Assert.Equal(0,Metric(await service.GetProductsAsync(q,default),"Doanh thu thuần"));
        q.CategoryId=f.CategoryB;
        Assert.Equal(80000,Metric(await service.GetGrossProfitAsync(q,default),"Doanh thu thuần"));
        Assert.Equal(80000,Metric(await service.GetProductsAsync(q,default),"Doanh thu thuần"));
        Assert.DoesNotContain(db.Model.FindEntityType(typeof(ConvenienceStore.Web.Data.Entities.CtHoaDon))!.GetProperties(),x=>x.GetColumnName()=="danh_muc_id");
    }
}

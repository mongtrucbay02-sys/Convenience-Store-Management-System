using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Returns.Models;
using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Tests.Returns;

[Collection(CsdlKhoCollection.Ten)]
public sealed class ReturnOrderMySqlTests(CsdlKhoFixture fixture)
{
    private static readonly DateTimeOffset Now=new(2026,11,5,15,0,0,TimeSpan.FromHours(7));
    private readonly TimeProvider clock=new InvoiceLookupServiceTests.FixedTime(Now);
    private System.Security.Claims.ClaimsPrincipal User=>InvoiceLookupServiceTests.Cashier(id:fixture.TaiKhoanThuNganId.ToString());
    private ReturnOrderService Service(CuaHangDbContext db,IReturnPointsGateway? gateway=null)=>
        new(db,BoTroGoiDichVuKho.TaoDichVu(db),gateway??new PendingKh06ReturnPointsGateway(),clock);
    private async Task<long> Insert(string sql,params (string,object?)[] args)=>Convert.ToInt64(await fixture.DocMotGiaTriAsync(sql,args));
    private sealed record Seed(long Invoice,int Product,long Price,int Quantity,int? Customer);
    private async Task<Seed> Setup(long price=25000,int quantity=10,int age=1,bool forbidden=false,int points=0,bool withStock=true,long discount=0)
    {
        int product=withStock?await new BoTroGoiDichVuKho(fixture).TaoSanPhamCoTonAsync("DT02",2,5000)
            :await new DuLieuTestKho(fixture).TaoSanPhamAsync("DT02");
        if(forbidden) {
            int category=(int)await Insert("INSERT INTO danh_muc (ten,khong_doi_tra) VALUES (@n,TRUE); SELECT LAST_INSERT_ID();",("@n","DT02"+Guid.NewGuid().ToString("N")));
            await fixture.ChayAsync("UPDATE san_pham SET danh_muc_id=@dm WHERE id=@p",("@dm",category),("@p",product));
        }
        await fixture.ChayAsync("INSERT INTO chinh_sach_doi_tra (hieu_luc_tu,nguong_tien_can_duyet,so_ngay_doi_tra) VALUES (@now,500000,7)",("@now",Now.DateTime.AddHours(-1)));
        int? customer=null;
        if(points>0)customer=(int)await Insert("INSERT INTO khach_hang (ho_ten,so_dien_thoai,diem_hien_co,nguoi_tao_id) VALUES ('Khách DT02',@phone,@d,@tn); SELECT LAST_INSERT_ID();",("@phone","0"+Random.Shared.Next(100000000,999999999)),("@d",points),("@tn",fixture.TaiKhoanThuNganId));
        long cart=await Insert("INSERT INTO gio_hang (thu_ngan_id,trang_thai,hoan_tat_luc,khach_hang_id) VALUES (@tn,'DA_HOAN_TAT',@time,@kh); SELECT LAST_INSERT_ID();",("@tn",fixture.TaiKhoanThuNganId),("@time",Now.DateTime.AddDays(-age)),("@kh",customer));
        long invoice=await Insert("""
            INSERT INTO hoa_don (ma_hoa_don,gio_hang_id,thu_ngan_id,khach_hang_id,thanh_toan_luc,
                tong_tien_hang,tong_giam,tong_thanh_toan,tien_khach_dua,tien_thua,diem_cong,so_tien_mot_diem)
            VALUES (@ma,@g,@tn,@kh,@time,@subtotal,@giam,@total,@total,0,@d,@rate); SELECT LAST_INSERT_ID();
            """,("@ma","HD"+Guid.NewGuid().ToString("N")[..12]),("@g",cart),("@tn",fixture.TaiKhoanThuNganId),
            ("@kh",customer),("@time",Now.DateTime.AddDays(-age)),("@subtotal",price*quantity),("@giam",discount*quantity),("@total",(price-discount)*quantity),("@d",points),("@rate",points>0?(price-discount)*quantity/points:null));
        long? promo=null;
        if(discount>0)promo=await Insert("""
            INSERT INTO chuong_trinh_khuyen_mai (ma,ten,loai_giam,gia_tri,bat_dau_luc,ket_thuc_luc,nguoi_tao_id)
            VALUES (@ma,'KM snapshot','SO_TIEN',@d,'2026-01-01','2026-01-02',@ad); SELECT LAST_INSERT_ID();
            """,("@ma","KM"+Guid.NewGuid().ToString("N")[..12]),("@d",discount),("@ad",fixture.TaiKhoanAdminId));
        await fixture.ChayAsync("""
            INSERT INTO ct_hoa_don (hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,gia_von,giam_don_vi,khuyen_mai_id)
            VALUES (@hd,@p,'Sản phẩm DT02','Hộp',@sl,@gia,8000,@giam,@km)
            """,("@hd",invoice),("@p",product),("@sl",quantity),("@gia",price),("@giam",discount),("@km",promo));
        return new(invoice,product,price,quantity,customer);
    }
    private static ReturnOrderRequest Request(Seed seed,int quantity=1,string handling="NHAP_LAI")=>
        new() { InvoiceId=seed.Invoice,CashConfirmed=true,Lines=[new() { ProductId=seed.Product,Quantity=quantity,
            Reason="KHONG_DUNG_NHU_CAU",Condition="Còn nguyên bao bì",Handling=handling }] };
    private async Task<long> Create(ReturnOrderService service,ReturnOrderRequest request)
    {
        request.ConfirmationToken=(await service.PreviewAsync(request,User)).ConfirmationToken;
        return await service.CreateAsync(request,User);
    }
    private async Task<long> CountSlips(long invoice)=>Convert.ToInt64(await fixture.DocMotGiaTriAsync("SELECT COUNT(*) FROM phieu_doi_tra WHERE hoa_don_goc_id=@hd",("@hd",invoice)));

    [Fact]
    public async Task CompletesRefund_RestocksAtOriginalCost_AndPrintsImmutableSnapshot()
    {
        var seed=await Setup();await using var db=fixture.TaoDbContext();var service=Service(db);
        long id=await Create(service,Request(seed));var receipt=await service.ReceiptAsync(id,User);
        Assert.Equal("HOAN_TAT",receipt.Status);Assert.Equal(25000,receipt.Refund);Assert.StartsWith("TH-20261105-",receipt.Code);
        Assert.Equal(8000,Assert.Single(receipt.Lines).Cost);
        var stock=await new DuLieuTestKho(fixture).DocTonKhoAsync(seed.Product);
        Assert.Equal(3,stock!.SoLuongTon);Assert.Equal(6000m,stock.GiaVonBinhQuan);
        Assert.Equal(1,await new DuLieuTestKho(fixture).DemTheKhoAsync(seed.Product,"TRA_HANG"));
        Assert.Equal(0,receipt.PointsRecovered);
    }
    [Fact]
    public async Task RefundUsesSavedDiscountEvenWhenPromotionExpired()
    {
        var seed=await Setup(discount:1000);await using var db=fixture.TaoDbContext();var service=Service(db);
        long id=await Create(service,Request(seed,2,"HUY"));Assert.Equal(48000,(await service.ReceiptAsync(id,User)).Refund);
    }
    [Fact]
    public async Task DestroyedReturn_DoesNotIncreaseStock_AndCannotOverRefund()
    {
        var seed=await Setup();await using var db=fixture.TaoDbContext();var service=Service(db);
        long id=await Create(service,Request(seed,2,"HUY"));var receipt=await service.ReceiptAsync(id,User);
        Assert.Equal(50000,receipt.Refund);Assert.Equal(2,(await new DuLieuTestKho(fixture).DocTonKhoAsync(seed.Product))!.SoLuongTon);
        Assert.Equal(0,await new DuLieuTestKho(fixture).DemTheKhoAsync(seed.Product,"TRA_HANG"));
    }
    [Theory]
    [InlineData(8,false,25000)]
    [InlineData(1,true,25000)]
    [InlineData(1,false,500100)]
    public async Task ExceptionsAndOverThreshold_CreatePendingWithoutEffects(int age,bool forbidden,long price)
    {
        var seed=await Setup(price:price,age:age,forbidden:forbidden);
        await using var db=fixture.TaoDbContext();var service=Service(db);var request=Request(seed);request.CashConfirmed=false;
        long id=await Create(service,request);Assert.Equal("CHO_DUYET",(await service.ReceiptAsync(id,User)).Status);
        Assert.Equal(2,(await new DuLieuTestKho(fixture).DocTonKhoAsync(seed.Product))!.SoLuongTon);
        Assert.Equal(0,await new DuLieuTestKho(fixture).DemTheKhoAsync(seed.Product,"TRA_HANG"));
    }
    [Fact]
    public async Task ExactlyThresholdAndLastCalendarDay_CompleteWithoutApproval()
    {
        var seed=await Setup(price:500000,age:7);await using var db=fixture.TaoDbContext();
        long id=await Create(Service(db),Request(seed));Assert.Equal("HOAN_TAT",(await Service(db).ReceiptAsync(id,User)).Status);
    }
    [Fact]
    public async Task PendingReservesQuantity_RejectedReleasesIt_WithoutRestocking()
    {
        var seed=await Setup(age:8);await using var db=fixture.TaoDbContext();var service=Service(db);
        long id=await Create(service,Request(seed,8));
        var error=await Assert.ThrowsAsync<LoiNghiepVu>(()=>service.PreviewAsync(Request(seed,3),User));Assert.Equal(422,error.MaHttp);
        // DT-06 is external; simulate its rejected state without adding an approval endpoint here.
        await fixture.ChayAsync("UPDATE phieu_doi_tra SET trang_thai='BI_TU_CHOI',nguoi_duyet_id=@ad,duyet_luc=@now WHERE id=@id",("@ad",fixture.TaiKhoanAdminId),("@now",Now.DateTime),("@id",id));
        Assert.Equal("BI_TU_CHOI",(await service.ReceiptAsync(id,User)).Status);
        Assert.NotNull(await service.PreviewAsync(Request(seed,10),User));
        Assert.Equal(2,(await new DuLieuTestKho(fixture).DocTonKhoAsync(seed.Product))!.SoLuongTon);
    }
    [Theory]
    [InlineData("reason")]
    [InlineData("quantity")]
    [InlineData("condition")]
    [InlineData("handling")]
    [InlineData("other-note")]
    [InlineData("duplicate")]
    public async Task InvalidLinesAreRejectedWithoutWrites(string invalid)
    {
        var seed=await Setup();var request=Request(seed);
        switch(invalid) {
            case "reason":request.Lines[0].Reason="";break;
            case "quantity":request.Lines[0].Quantity=11;break;
            case "condition":request.Lines[0].Condition="";break;
            case "handling":request.Lines[0].Handling="INVALID";break;
            case "other-note":request.Lines[0].Reason="KHAC";break;
            case "duplicate":request.Lines.Add(request.Lines[0]);break;
        }
        await using var db=fixture.TaoDbContext();
        Assert.Equal(422,(await Assert.ThrowsAsync<LoiNghiepVu>(()=>Service(db).PreviewAsync(request,User))).MaHttp);
        Assert.Equal(0,await CountSlips(seed.Invoice));
    }
    [Fact]
    public async Task CancelledInvoiceIsRejected()
    {
        var seed=await Setup();await fixture.ChayAsync("UPDATE hoa_don SET trang_thai='DA_HUY',nguoi_huy_id=@ad,huy_luc=@now,ly_do_huy='Test' WHERE id=@id",("@ad",fixture.TaiKhoanAdminId),("@now",Now.DateTime),("@id",seed.Invoice));
        await using var db=fixture.TaoDbContext();Assert.Equal(409,(await Assert.ThrowsAsync<LoiNghiepVu>(()=>Service(db).FormAsync(seed.Invoice,User))).MaHttp);
    }
    [Fact]
    public async Task StaleConfirmationAndDoubleSubmitCannotCreateDuplicateReturn()
    {
        var seed=await Setup();await using var db=fixture.TaoDbContext();var service=Service(db);var request=Request(seed,7);
        await Create(service,request);
        await Assert.ThrowsAsync<LoiNghiepVu>(()=>service.CreateAsync(request,User));Assert.Equal(1,await CountSlips(seed.Invoice));
    }
    [Fact]
    public async Task ConcurrentReturnsCannotExceedPurchasedQuantity()
    {
        var seed=await Setup();var a=Request(seed,7);var b=Request(seed,7);
        await using var dba=fixture.TaoDbContext();await using var dbb=fixture.TaoDbContext();
        var sa=Service(dba);var sb=Service(dbb);
        a.ConfirmationToken=(await sa.PreviewAsync(a,User)).ConfirmationToken;b.ConfirmationToken=(await sb.PreviewAsync(b,User)).ConfirmationToken;
        async Task<bool> Try(ReturnOrderService s,ReturnOrderRequest q) { try { await s.CreateAsync(q,User);return true; } catch(LoiNghiepVu) { return false; } }
        var results=await Task.WhenAll(Try(sa,a),Try(sb,b));Assert.Single(results,x=>x);Assert.Equal(1,await CountSlips(seed.Invoice));
    }
    [Fact]
    public async Task MissingKh06BlocksMemberCompletionWithoutPartialRefundOrStock()
    {
        var seed=await Setup(points:5);await using var db=fixture.TaoDbContext();var service=Service(db);var request=Request(seed);
        var preview=await service.PreviewAsync(request,User);Assert.True(preview.PointsBlocked);request.ConfirmationToken=preview.ConfirmationToken;
        Assert.Equal("CHUA_TICH_HOP_KH06",(await Assert.ThrowsAsync<LoiNghiepVu>(()=>service.CreateAsync(request,User))).Ma);
        Assert.Equal(0,await CountSlips(seed.Invoice));Assert.Equal(2,(await new DuLieuTestKho(fixture).DocTonKhoAsync(seed.Product))!.SoLuongTon);
    }
    private sealed class ThrowingPoints : IReturnPointsGateway
    {
        public Task<int> RecoverAsync(CuaHangDbContext db,long invoice,long slip,int actor,long refund,CancellationToken ct)=>throw new InvalidOperationException("KH06 failed");
    }
    [Fact]
    public async Task PointsFailureAfterRestockRollsBackSlipAndStockTogether()
    {
        var seed=await Setup(points:5);await using var db=fixture.TaoDbContext();var service=Service(db,new ThrowingPoints());var request=Request(seed);
        request.ConfirmationToken=(await service.PreviewAsync(request,User)).ConfirmationToken;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CreateAsync(request,User));
        Assert.Equal(0,await CountSlips(seed.Invoice));Assert.Equal(2,(await new DuLieuTestKho(fixture).DocTonKhoAsync(seed.Product))!.SoLuongTon);
        Assert.Equal(0,await new DuLieuTestKho(fixture).DemTheKhoAsync(seed.Product,"TRA_HANG"));
    }
    [Fact]
    public async Task MissingStockRollsBackWholeReturn()
    {
        var seed=await Setup(withStock:false);await using var db=fixture.TaoDbContext();var service=Service(db);var request=Request(seed);
        request.ConfirmationToken=(await service.PreviewAsync(request,User)).ConfirmationToken;
        await Assert.ThrowsAnyAsync<LoiNghiepVu>(()=>service.CreateAsync(request,User));Assert.Equal(0,await CountSlips(seed.Invoice));
    }
    [Fact]
    public async Task CompletedDetailsCannotBeEditedOrDeleted()
    {
        var seed=await Setup();await using var db=fixture.TaoDbContext();long id=await Create(Service(db),Request(seed));
        await Assert.ThrowsAnyAsync<System.Data.Common.DbException>(()=>fixture.ChayAsync("UPDATE ct_phieu_doi_tra SET so_luong=2 WHERE phieu_doi_tra_id=@id",("@id",id)));
        await Assert.ThrowsAnyAsync<System.Data.Common.DbException>(()=>fixture.ChayAsync("DELETE FROM ct_phieu_doi_tra WHERE phieu_doi_tra_id=@id",("@id",id)));
    }
    [Theory]
    [InlineData("AD")]
    [InlineData("NK")]
    public async Task OnlyCashierCanCreate(string role)
    {
        await using var db=fixture.TaoDbContext();var user=InvoiceLookupServiceTests.Cashier(role);
        Assert.Equal(403,(await Assert.ThrowsAsync<LoiNghiepVu>(()=>Service(db).PreviewAsync(new(),user))).MaHttp);
    }
}


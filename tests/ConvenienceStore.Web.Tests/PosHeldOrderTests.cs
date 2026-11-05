using System.Security.Claims;
using ConvenienceStore.Web.Controllers;
using ConvenienceStore.Web.Modules.Auth.Security;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ConvenienceStore.Web.Tests;

[Collection(CsdlKhoCollection.Ten)]
public class PosHeldOrderTests(CsdlKhoFixture fixture)
{
    private async Task<int> Cashier()
    {
        string name = "hold" + Guid.NewGuid().ToString("N")[..12];
        await fixture.ChayAsync("""
            INSERT INTO tai_khoan (ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
            VALUES (@n,'test-only','Thu ngân POS08',(SELECT id FROM vai_tro WHERE ma='TN'),FALSE)
            """, ("@n",name));
        return Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM tai_khoan WHERE ten_dang_nhap=@n",("@n",name)));
    }
    private PosHeldOrderService Service(int id, string role="TN", bool changePassword=false)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier,id.ToString()),new Claim(ClaimTypes.Role,role),
            new Claim(ThongTinXacThuc.ClaimBatBuocDoiMatKhau,changePassword?"true":"false") }, "Test")) };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["ConnectionStrings:CuaHang"]=fixture.ChuoiKetNoi }).Build();
        return new(new MySqlPosCatalog(config),new HttpContextAccessor { HttpContext=context });
    }
    private async Task<int> Product(int stock=10)
    {
        int product=await new BoTroGoiDichVuKho(fixture).TaoSanPhamCoTonAsync("POS08",stock,10000);
        await fixture.ChayAsync("INSERT INTO lich_su_gia (san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id) VALUES (@p,25000,NOW()-INTERVAL 1 DAY,@a)",("@p",product),("@a",fixture.TaiKhoanAdminId));
        return product;
    }
    private async Task<int> Cart(int cashier,int? product=null,int quantity=2,bool held=false)
    {
        await using var connection=await fixture.MoKetNoiAsync();
        await using var command=CsdlKhoFixture.TaoLenh(connection,"""
            INSERT INTO gio_hang (thu_ngan_id,trang_thai,tam_giu_luc) VALUES (@t,@s,@time)
            """,("@t",cashier),("@s",held?"TAM_GIU":"DANG_BAN"),("@time",held?DateTime.UtcNow.AddHours(7):null));
        await command.ExecuteNonQueryAsync();int id=(int)command.LastInsertedId;
        if(product.HasValue)await fixture.ChayAsync("INSERT INTO ct_gio_hang (gio_hang_id,san_pham_id,so_luong,don_gia) VALUES (@g,@p,@q,25000)",("@g",id),("@p",product),("@q",quantity));
        return id;
    }
    private async Task<string?> State(int id)=>Convert.ToString(await fixture.DocMotGiaTriAsync("SELECT trang_thai FROM gio_hang WHERE id=@g",("@g",id)));

    [Fact]
    public async Task HoldAndRecall_PreservesLines_NoStockOrPointsChanges()
    {
        int cashier=await Cashier(), product=await Product(), cart=await Cart(cashier,product);
        var service=Service(cashier);
        long ledgerBefore=Convert.ToInt64(await fixture.DocMotGiaTriAsync("SELECT COUNT(*) FROM the_kho WHERE san_pham_id=@p",("@p",product)));
        Assert.True((await service.HoldAsync("  Khách lấy thêm đồ  ")).Success);
        var held=Assert.Single((await service.ListAsync()).Orders);
        Assert.Equal(cart,held.Id);Assert.Equal("Khách lấy thêm đồ",held.Note);Assert.Equal(50000,held.Total);
        Assert.Equal(1,Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT COUNT(*) FROM gio_hang WHERE thu_ngan_id=@t AND trang_thai='DANG_BAN'",("@t",cashier))));
        Assert.True((await service.RecallAsync(cart)).Success);
        Assert.Equal("DANG_BAN",await State(cart));Assert.Empty((await service.ListAsync()).Orders);
        Assert.Equal(10,(await new DuLieuTestKho(fixture).DocTonKhoAsync(product))!.SoLuongTon);
        Assert.Equal(ledgerBefore,Convert.ToInt64(await fixture.DocMotGiaTriAsync("SELECT COUNT(*) FROM the_kho WHERE san_pham_id=@p",("@p",product))));
        Assert.Equal(0,Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT COUNT(*) FROM hoa_don WHERE gio_hang_id=@g",("@g",cart))));
    }
    [Fact]
    public async Task FifthHeldOrderAllowed_SixthRefused_PreservesActiveCart()
    {
        int cashier=await Cashier(),product=await Product();var service=Service(cashier);
        for(int i=0;i<4;i++)await Cart(cashier,product,held:true);
        int cart=await Cart(cashier,product);
        Assert.True((await service.HoldAsync(null)).Success);
        int current=Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM gio_hang WHERE thu_ngan_id=@t AND trang_thai='DANG_BAN'",("@t",cashier)));
        await fixture.ChayAsync("INSERT INTO ct_gio_hang (gio_hang_id,san_pham_id,so_luong,don_gia) VALUES (@g,@p,1,25000)",("@g",current),("@p",product));
        var result=await service.HoldAsync(null);Assert.False(result.Success);Assert.Contains("5 đơn",result.Message);
        Assert.Equal(5,(await service.ListAsync()).Orders.Count);Assert.Equal("DANG_BAN",await State(current));
    }
    [Fact]
    public async Task ExpiredOrdersCancelled_OnlyForCurrentCashier()
    {
        int cashier=await Cashier(),other=await Cashier(),product=await Product();
        int old=await Cart(cashier,product,held:true),foreign=await Cart(other,product,held:true);
        await fixture.ChayAsync("UPDATE gio_hang SET tam_giu_luc=CURDATE()-INTERVAL 1 SECOND WHERE id IN (@g,@f)",("@g",old),("@f",foreign));
        var result=await Service(cashier).ListAsync();Assert.Equal(1,result.ExpiredCount);Assert.Empty(result.Orders);
        Assert.Equal("DA_HUY",await State(old));Assert.Equal("TAM_GIU",await State(foreign));
        Assert.Equal("HET_HAN_TAM_GIU",await fixture.DocMotGiaTriAsync("SELECT ly_do_huy FROM gio_hang WHERE id=@g",("@g",old)));
        Assert.Equal(50000,Convert.ToInt64(await fixture.DocMotGiaTriAsync("SELECT gia_tri_khi_huy FROM gio_hang WHERE id=@g",("@g",old))));
    }
    [Fact]
    public async Task Recall_RejectsOtherCashier_AndPreservesCurrentNonemptyCart()
    {
        int cashier=await Cashier(),other=await Cashier(),product=await Product();
        int target=await Cart(cashier,product,held:true);
        Assert.False((await Service(other).RecallAsync(target)).Success);
        int current=await Cart(cashier,product);
        Assert.False((await Service(cashier).RecallAsync(target)).Success);
        Assert.Equal("TAM_GIU",await State(target));Assert.Equal("DANG_BAN",await State(current));
    }
    [Fact]
    public async Task Recall_InsufficientStock_LeavesOrderHeld_WithExplicitError()
    {
        int cashier=await Cashier(),product=await Product(),target=await Cart(cashier,product,quantity:8,held:true);
        await new BoTroGoiDichVuKho(fixture).GhiVaCommitAsync(LoaiBienDong.BAN_HANG,new DongBienDong(product,SoLuong:5));
        var result=await Service(cashier).RecallAsync(target);
        Assert.False(result.Success);Assert.Contains("cần 8, tồn hiện tại 5",result.Message);
        Assert.Equal("TAM_GIU",await State(target));
        Assert.Equal(5,(await new DuLieuTestKho(fixture).DocTonKhoAsync(product))!.SoLuongTon);
    }
    [Fact]
    public async Task Recall_RepricesAndRecalculatesPromotion_ReportsChanges()
    {
        int cashier=await Cashier(),product=await Product(),target=await Cart(cashier,product,held:true);
        await fixture.ChayAsync("INSERT INTO lich_su_gia (san_pham_id,gia_ban,hieu_luc_tu,nguoi_tao_id) VALUES (@p,30000,NOW(),@a)",("@p",product),("@a",fixture.TaiKhoanAdminId));
        string code="HOLD"+Guid.NewGuid().ToString("N")[..10];
        await fixture.ChayAsync("""
            INSERT INTO chuong_trinh_khuyen_mai (ma,ten,loai_giam,gia_tri,bat_dau_luc,ket_thuc_luc,trang_thai,nguoi_tao_id)
            VALUES (@ma,'KM POS08','PHAN_TRAM',15,NOW()-INTERVAL 1 DAY,NOW()+INTERVAL 1 DAY,'DA_KICH_HOAT',@a)
            """,("@ma",code),("@a",fixture.TaiKhoanAdminId));
        int promotion=Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM chuong_trinh_khuyen_mai WHERE ma=@ma",("@ma",code)));
        await fixture.ChayAsync("INSERT INTO khuyen_mai_san_pham VALUES (@km,@p)",("@km",promotion),("@p",product));
        var result=await Service(cashier).RecallAsync(target);Assert.True(result.Success);Assert.Contains("giá",result.Message);Assert.Contains("giảm mỗi đơn vị",result.Message);
        Assert.Equal(30000,Convert.ToInt64(await fixture.DocMotGiaTriAsync("SELECT don_gia FROM ct_gio_hang WHERE gio_hang_id=@g",("@g",target))));
        Assert.Equal(4500,Convert.ToInt64(await fixture.DocMotGiaTriAsync("SELECT giam_don_vi FROM ct_gio_hang WHERE gio_hang_id=@g",("@g",target))));
        Assert.Equal(10,(await new DuLieuTestKho(fixture).DocTonKhoAsync(product))!.SoLuongTon);
    }
    [Fact]
    public async Task Hold_RejectsEmptyCartAndLongNote()
    {
        int cashier=await Cashier();var service=Service(cashier);
        Assert.False((await service.HoldAsync(null)).Success);
        await Cart(cashier);Assert.False((await service.HoldAsync("note")).Success);
        Assert.False((await service.HoldAsync(new string('a',101))).Success);
    }
    [Fact]
    public async Task ParallelHoldAtLimit_CannotExceedFive()
    {
        int cashier=await Cashier(),product=await Product();
        for(int i=0;i<4;i++)await Cart(cashier,product,held:true);
        await Cart(cashier,product);
        var results=await Task.WhenAll(Service(cashier).HoldAsync(null),Service(cashier).HoldAsync(null));
        Assert.Single(results,x=>x.Success);Assert.Equal(5,(await Service(cashier).ListAsync()).Orders.Count);
    }
    [Fact]
    public async Task Recall_PausedMemberPromotionRemoved_AndDoesNotCreateStockMovements()
    {
        int cashier=await Cashier(),product=await Product(),target=await Cart(cashier,product,held:true);
        string code="HOLD"+Guid.NewGuid().ToString("N")[..10];
        await fixture.ChayAsync("""
            INSERT INTO chuong_trinh_khuyen_mai (ma,ten,loai_giam,gia_tri,bat_dau_luc,ket_thuc_luc,trang_thai,nguoi_tao_id)
            VALUES (@ma,'KM cũ','SO_TIEN',1000,NOW()-INTERVAL 1 DAY,NOW()+INTERVAL 1 DAY,'TAM_DUNG',@a)
            """,("@ma",code),("@a",fixture.TaiKhoanAdminId));
        int promotion=Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM chuong_trinh_khuyen_mai WHERE ma=@ma",("@ma",code)));
        await fixture.ChayAsync("INSERT INTO khuyen_mai_san_pham VALUES (@km,@p)",("@km",promotion),("@p",product));
        await fixture.ChayAsync("UPDATE ct_gio_hang SET khuyen_mai_id=@k,giam_don_vi=1000 WHERE gio_hang_id=@g",("@k",promotion),("@g",target));
        var result=await Service(cashier).RecallAsync(target);Assert.True(result.Success);Assert.Contains("giảm mỗi đơn vị",result.Message);
        Assert.Null(await fixture.DocMotGiaTriAsync("SELECT khuyen_mai_id FROM ct_gio_hang WHERE gio_hang_id=@g",("@g",target)));
        Assert.Equal(0,Convert.ToInt64(await fixture.DocMotGiaTriAsync("SELECT giam_don_vi FROM ct_gio_hang WHERE gio_hang_id=@g",("@g",target))));
        Assert.Equal(10,(await new DuLieuTestKho(fixture).DocTonKhoAsync(product))!.SoLuongTon);
    }
    [Fact]
    public async Task DuplicateRecall_DoesNotOpenTwoActiveOrders()
    {
        int cashier=await Cashier(),product=await Product(),target=await Cart(cashier,product,held:true);
        var results=await Task.WhenAll(Service(cashier).RecallAsync(target),Service(cashier).RecallAsync(target));
        Assert.Single(results,x=>x.Success);
        Assert.Equal(1,Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT COUNT(*) FROM gio_hang WHERE thu_ngan_id=@t AND trang_thai='DANG_BAN'",("@t",cashier))));
    }
    [Fact]
    public async Task FakeCashierClaimCannotUseAdminDatabaseAccount()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service(fixture.TaiKhoanAdminId).ListAsync());
    }
    [Theory]
    [InlineData("AD",false)]
    [InlineData("NK",false)]
    [InlineData("TN",true)]
    public async Task RequiresCashierAndPasswordChanged(string role,bool changePassword)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service(fixture.TaiKhoanThuNganId,role,changePassword).ListAsync());
    }
    [Fact]
    public void MutationsRequirePostAndAntiforgery()
    {
        foreach(var method in new[]{nameof(PosController.Hold),nameof(PosController.Recall)})
        {
            var action=typeof(PosController).GetMethod(method)!;
            Assert.NotNull(Attribute.GetCustomAttribute(action,typeof(HttpPostAttribute)));
            Assert.NotNull(Attribute.GetCustomAttribute(action,typeof(ValidateAntiForgeryTokenAttribute)));
        }
    }
}

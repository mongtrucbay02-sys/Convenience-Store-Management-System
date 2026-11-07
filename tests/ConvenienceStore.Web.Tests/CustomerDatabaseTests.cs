using ConvenienceStore.Web.Modules.Customers;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ConvenienceStore.Web.Tests;

[Collection(CsdlKhoCollection.Ten)]
public sealed class CustomerDatabaseTests(CsdlKhoFixture fixture)
{
    private static CustomerInput Input() => new() { Name = "Khách kiểm thử", Phone = "0" + Random.Shared.NextInt64(100000000,999999999) };

    [Fact]
    public async Task CreateSearchEdit_RejectsDuplicateAndPreservesPoints()
    {
        await using var db = fixture.TaoDbContext();
        var service = new CustomerService(db);
        var input = Input();
        var id = await service.SaveAsync(null, input, fixture.TaiKhoanAdminId, default);
        var row = Assert.Single(await service.SearchAsync(input.Phone, false, default));
        Assert.Equal(0u, row.Points);
        Assert.Equal(CustomerRules.MaskPhone(input.Phone), row.Phone);
        await Assert.ThrowsAsync<LoiNghiepVu>(() => service.SaveAsync(null, input, fixture.TaiKhoanAdminId, default));
        var edit = await service.GetInputAsync(id, default);
        edit.Name = "Tên đã sửa";
        await service.SaveAsync(id, edit, fixture.TaiKhoanAdminId, default);
        await Assert.ThrowsAsync<LoiNghiepVu>(() => service.SaveAsync(id, edit, fixture.TaiKhoanAdminId, default));
        Assert.Equal("Tên đã sửa", (await service.HistoryAsync(id, true, default)).Customer.Name);
    }

    [Fact]
    public async Task Adjust_RollbackNegative_AndAnonymizeWithoutDeletingLedger()
    {
        await using var db = fixture.TaoDbContext();
        var service = new CustomerService(db);
        var id = await service.SaveAsync(null, Input(), fixture.TaiKhoanAdminId, default);
        await service.AdjustAsync(id, new() { Points = 5, Reason = "Bù điểm" }, fixture.TaiKhoanAdminId, default);
        await Assert.ThrowsAsync<LoiNghiepVu>(() => service.AdjustAsync(id, new() { Points = -6, Reason = "Sai" }, fixture.TaiKhoanAdminId, default));
        var history = await service.HistoryAsync(id, true, default);
        Assert.Equal(5u, history.Customer.Points);
        Assert.Single(history.Points);
        await service.DeleteAsync(id, default);
        var stored = await db.KhachHangs.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("DA_AN", stored.TrangThai);
        Assert.Null(stored.HoTen); Assert.Null(stored.SoDienThoai); Assert.Null(stored.Email);
        Assert.Single(await db.LichSuDiems.Where(x => x.KhachHangId == id).ToListAsync());
    }

    [Fact]
    public async Task AttachAndRemove_RepricesMemberPromotions_AndBlocksDeleteWhileAttached()
    {
        await using var db = fixture.TaoDbContext();
        var customers = new CustomerService(db);
        var customerId = await customers.SaveAsync(null, Input(), fixture.TaiKhoanAdminId, default);
        var login = "kh03" + Guid.NewGuid().ToString("N")[..10];
        await fixture.ChayAsync("""
            INSERT INTO tai_khoan (ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk)
            VALUES (@n,'test-only','Thu ngân test',(SELECT id FROM vai_tro WHERE ma='TN'),FALSE)
            """, ("@n", login));
        var cashier = Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM tai_khoan WHERE ten_dang_nhap=@n", ("@n", login)));
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext {
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                new(System.Security.Claims.ClaimTypes.NameIdentifier, cashier.ToString()),
                new(System.Security.Claims.ClaimTypes.Role, "TN")], "Test")) };
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["ConnectionStrings:CuaHang"] = fixture.ChuoiKetNoi }).Build();
        var cart = new ConvenienceStore.Web.Services.Pos.PosCartService(new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = http },
            new ConvenienceStore.Web.Services.Pos.MySqlPosCatalog(config));
        await cart.GetCartAsync();
        int product = await new DuLieuTestKho(fixture).TaoSanPhamAsync("POS03");
        await fixture.ChayAsync("""
            INSERT INTO ct_gio_hang (gio_hang_id,san_pham_id,so_luong,don_gia)
            SELECT id,@p,1,25000 FROM gio_hang WHERE thu_ngan_id=@t AND trang_thai='DANG_BAN'
            """, ("@p", product), ("@t", cashier));
        var code = "KH" + Guid.NewGuid().ToString("N")[..12];
        await fixture.ChayAsync("""
            INSERT INTO chuong_trinh_khuyen_mai
            (ma,ten,loai_giam,gia_tri,bat_dau_luc,ket_thuc_luc,trang_thai,chi_thanh_vien,nguoi_tao_id)
            VALUES (@ma,'Thành viên','SO_TIEN',1000,NOW()-INTERVAL 1 DAY,NOW()+INTERVAL 1 DAY,'DA_KICH_HOAT',TRUE,@a)
            """, ("@ma", code), ("@a", fixture.TaiKhoanAdminId));
        await fixture.ChayAsync("INSERT INTO khuyen_mai_san_pham SELECT id,@p FROM chuong_trinh_khuyen_mai WHERE ma=@ma", ("@p", product), ("@ma", code));
        Assert.Equal(0, (await cart.GetCartAsync()).Discount);
        Assert.Null(await cart.SetCustomerAsync(checked((int)customerId)));
        Assert.Equal(1000, (await cart.GetCartAsync()).Discount);
        await Assert.ThrowsAsync<LoiNghiepVu>(() => customers.DeleteAsync(customerId, default));
        Assert.Null(await cart.SetCustomerAsync(null));
        Assert.Equal(0, (await cart.GetCartAsync()).Discount);
        Assert.Null((await cart.GetCartAsync()).CustomerId);
    }

    [Fact]
    public async Task DeleteUnusedCustomer_RemovesRow()
    {
        await using var db = fixture.TaoDbContext();
        var service = new CustomerService(db);
        var id = await service.SaveAsync(null, Input(), fixture.TaiKhoanAdminId, default);
        await service.DeleteAsync(id, default);
        Assert.False(await db.KhachHangs.AnyAsync(x => x.Id == id));
    }
}

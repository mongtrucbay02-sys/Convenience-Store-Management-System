using Microsoft.Extensions.Configuration;
using ConvenienceStore.Web.Services.Pos;
using ConvenienceStore.Web.Tests.Inventory;

namespace ConvenienceStore.Web.Tests;

public class PosPromotionCalculatorTests
{
    private static PosPromotion P(int id = 1, string type = "PHAN_TRAM", long value = 15,
        long? cap = null, bool member = false, int days = 1) =>
        new(id, 1, "Ưu đãi", type, value, cap, member, new DateTime(2026, 11, 5).AddDays(days));

    [Theory]
    [InlineData(25000, 15, null, 3700)]
    [InlineData(25000, 15, 3000L, 3000)]
    [InlineData(100, 1, null, 0)]
    [InlineData(10000, 100, null, 10000)]
    public void Percentage_RoundsDownPerUnit_ThenCaps(long price, long value, long? cap, long expected) =>
        Assert.Equal(expected, PosPromotionCalculator.Discount(price, P(value: value, cap: cap)));

    [Fact]
    public void FixedAmount_CannotExceedPrice() =>
        Assert.Equal(10000, PosPromotionCalculator.Discount(10000, P(type: "SO_TIEN", value: 20000)));

    [Fact]
    public void Best_UsesEffectiveDiscount_ThenEndDate_ThenId()
    {
        var candidates = new[] { P(5, value: 30, cap: 2000), P(4, value: 20, days: 2), P(3, value: 20), P(2, value: 20) };
        Assert.Equal(2, PosPromotionCalculator.Best(candidates, 1, 20000, false)!.Id);
    }

    [Fact]
    public void MemberRemoved_RemovesMemberPromotion()
    {
        var candidates = new[] { P(member: true) };
        Assert.NotNull(PosPromotionCalculator.Best(candidates, 1, 25000, true));
        Assert.Null(PosPromotionCalculator.Best(candidates, 1, 25000, false));
    }

    [Fact]
    public void NoProductScope_OrNoBenefit_DoesNotApply()
    {
        Assert.Null(PosPromotionCalculator.Best(new[] { P() }, 2, 25000, true));
        Assert.Null(PosPromotionCalculator.Best(new[] { P(value: 1) }, 1, 100, true));
        Assert.Null(PosPromotionCalculator.Best(Array.Empty<PosPromotion>(), 1, 25000, true));
    }
}

[Collection(CsdlKhoCollection.Ten)]
public class PosPromotionDatabaseTests(CsdlKhoFixture fixture)
{
    [Fact]
    public async Task Cart_AppliesPromotion_AndRemovesPausedPromotion()
    {
        int product = await new DuLieuTestKho(fixture).TaoSanPhamAsync("POS04");
        await fixture.ChayAsync("""
            INSERT INTO chuong_trinh_khuyen_mai
            (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, trang_thai, nguoi_tao_id)
            VALUES ('POS04-TEST', 'Giảm 15%', 'PHAN_TRAM', 15, NOW()-INTERVAL 1 DAY,
                    NOW()+INTERVAL 1 DAY, 'DA_KICH_HOAT', @a)
            """, ("@a", fixture.TaiKhoanAdminId));
        int promotion = Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM chuong_trinh_khuyen_mai WHERE ma='POS04-TEST'"));
        await fixture.ChayAsync("INSERT INTO khuyen_mai_san_pham VALUES (@k,@p)", ("@k", promotion), ("@p", product));
        await fixture.ChayAsync("INSERT INTO gio_hang (thu_ngan_id) VALUES (@t)", ("@t", fixture.TaiKhoanThuNganId));
        int cartId = Convert.ToInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM gio_hang WHERE thu_ngan_id=@t AND trang_thai='DANG_BAN'", ("@t", fixture.TaiKhoanThuNganId)));
        await fixture.ChayAsync("INSERT INTO ct_gio_hang (gio_hang_id,san_pham_id,so_luong,don_gia) VALUES (@g,@p,2,25000)", ("@g", cartId), ("@p", product));
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, fixture.TaiKhoanThuNganId.ToString()) }, "Test"));
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["ConnectionStrings:CuaHang"] = fixture.ChuoiKetNoi }).Build();
        var service = new PosCartService(new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = context }, new MySqlPosCatalog(config));
        var cart = await service.GetCartAsync();
        Assert.Equal(7400, cart.Discount);
        Assert.Equal("Giảm 15%", Assert.Single(cart.Items).PromotionName);
        Assert.Equal(42600, cart.Total);
        await fixture.ChayAsync("UPDATE chuong_trinh_khuyen_mai SET trang_thai='TAM_DUNG' WHERE id=@k", ("@k", promotion));
        cart = await service.GetCartAsync();
        Assert.Equal(0, cart.Discount);
        Assert.Null(Assert.Single(cart.Items).PromotionId);
        Assert.True(cart.PromotionsChanged);
    }
}


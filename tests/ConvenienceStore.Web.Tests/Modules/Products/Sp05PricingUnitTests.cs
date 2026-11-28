using ConvenienceStore.Web.Modules.Products.DTOs;
using ConvenienceStore.Web.Modules.Products.Services;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Tests.Modules.Products;

public sealed class Sp05PricingUnitTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(-100L)]
    [InlineData(0L)]
    [InlineData(101L)]
    [InlineData(4294967300L)]
    public void InvalidMoneyIsRejected(long? value) => Assert.Equal(422, Assert.Throws<LoiNghiepVu>(() => ProductPriceRules.Normalize(new() { GiaBan = value, PhienBan = 1 })).MaHttp);

    [Fact]
    public void PrecisionTimeZonesAndPastBoundaryFollowDatabaseSeconds()
    {
        var now = new DateTime(2026, 11, 16, 0, 0, 0);
        Assert.Equal(now.AddHours(-7), ProductPriceRules.ResolveEffectiveAt(null, now));
        Assert.Equal(now.AddHours(-7), ProductPriceRules.ResolveEffectiveAt("2026-11-15T17:00:00Z", now));
        Assert.Equal(now.AddDays(1).AddHours(-7).AddTicks(1234567), ProductPriceRules.ResolveEffectiveAt("2026-11-17T00:00:00.1234567+07:00", now));
        Assert.Equal(422, Assert.Throws<LoiNghiepVu>(() => ProductPriceRules.ResolveEffectiveAt("2026-11-15T16:59:59Z", now)).MaHttp);
        foreach (var text in new[] { "not-date", "2026-02-31T12:00:00+07:00", "2026-11-17T12:00:00", "9999-12-31T23:59:59Z" })
            Assert.Throws<LoiNghiepVu>(() => ProductPriceRules.Normalize(new() { GiaBan = 100, PhienBan = 1, HieuLucTu = text }));
        Assert.Throws<LoiNghiepVu>(() => ProductPriceRules.Normalize(new() { GiaBan = 100, PhienBan = 0 }));
        Assert.Equal(4294967200L, ProductPriceRules.Normalize(new() { GiaBan = 4294967200L, PhienBan = 1 }).GiaBan);
    }

    [Theory]
    [InlineData(7900L, 8000, false, false)]
    [InlineData(7900L, 8000, true, true)]
    [InlineData(8000L, 8000, false, true)]
    [InlineData(8100L, 8000, false, true)]
    public void OnlyPriceBelowCostRequiresConfirmation(long price, int cost, bool confirmed, bool allowed)
    {
        if (allowed) ProductPriceRules.EnsureCostConfirmed(price, cost, confirmed);
        else Assert.Equal("CAN_XAC_NHAN_GIA_DUOI_VON", Assert.Throws<LoiNghiepVu>(() => ProductPriceRules.EnsureCostConfirmed(price, cost, confirmed)).Ma);
    }
}

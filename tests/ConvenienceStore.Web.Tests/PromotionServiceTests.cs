using ConvenienceStore.Web.Modules.Promotions;
using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;

namespace ConvenienceStore.Web.Tests;

public class PromotionServiceTests
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 10, 0, 0, Vn);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }

    private static PromotionRecord Promo(
        long id, PromotionStatus status, DateTimeOffset start, DateTimeOffset end) =>
        new(id, $"KM-{id:0000}", $"CT {id}", null, DiscountType.Percent, 10, null,
            new[] { new ProductRefDto(1, "SKU", "SP") }, start, end, false, status, 0);

    private static PromotionService Create(params PromotionRecord[] records) =>
        new(new InMemoryPromotionDataSource(records), new SampleProductLookup(), new FixedTime(Now));

    private static PromotionQuery Q(
        PromotionDisplayStatus? s = null, DateOnly? from = null, DateOnly? to = null,
        int page = 1, int size = 20) => new(s, from, to, page, size);

    [Theory]
    [InlineData(PromotionStatus.Active, -1, 1, PromotionDisplayStatus.Running)]
    [InlineData(PromotionStatus.Active, 1, 2, PromotionDisplayStatus.Scheduled)]
    [InlineData(PromotionStatus.Active, -5, -1, PromotionDisplayStatus.Ended)]
    [InlineData(PromotionStatus.Draft, -1, 1, PromotionDisplayStatus.Draft)]
    [InlineData(PromotionStatus.Paused, -1, 1, PromotionDisplayStatus.Paused)]
    [InlineData(PromotionStatus.Ended, -1, 1, PromotionDisplayStatus.Ended)]
    public void DisplayStatus_IsDerivedFromStatusAndTime_BR_KM_07(
        PromotionStatus status, int startDays, int endDays, PromotionDisplayStatus expected)
    {
        var actual = PromotionService.GetDisplayStatus(
            status, Now.AddDays(startDays), Now.AddDays(endDays), Now);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DisplayStatus_AtExactStartAndEnd_IsRunning()
    {
        Assert.Equal(PromotionDisplayStatus.Running,
            PromotionService.GetDisplayStatus(PromotionStatus.Active, Now, Now.AddHours(1), Now));
        Assert.Equal(PromotionDisplayStatus.Running,
            PromotionService.GetDisplayStatus(PromotionStatus.Active, Now.AddHours(-1), Now, Now));
    }

    [Fact]
    public async Task NoPromotions_ReturnsEmptyList_E1()
    {
        var r = await Create().ListAsync(Q(), isAdmin: true, default);

        Assert.Empty(r.Items);
        Assert.Equal(0, r.TotalItems);
        Assert.Equal(0, r.TotalPages);
    }

    [Fact]
    public async Task Admin_SeesAll_AndCanFilterByDisplayStatus()
    {
        var svc = Create(
            Promo(1, PromotionStatus.Active, Now.AddDays(-1), Now.AddDays(1)),
            Promo(2, PromotionStatus.Draft, Now.AddDays(-1), Now.AddDays(1)),
            Promo(3, PromotionStatus.Active, Now.AddDays(-9), Now.AddDays(-2)));

        var all = await svc.ListAsync(Q(), true, default);
        var ended = await svc.ListAsync(Q(PromotionDisplayStatus.Ended), true, default);

        Assert.Equal(3, all.TotalItems);
        Assert.Equal(3, ended.Items.Single().Id);
    }

    [Fact]
    public async Task Cashier_OnlySeesRunning_RegardlessOfFilter()
    {
        var svc = Create(
            Promo(1, PromotionStatus.Active, Now.AddDays(-1), Now.AddDays(1)),
            Promo(2, PromotionStatus.Draft, Now.AddDays(-1), Now.AddDays(1)),
            Promo(3, PromotionStatus.Paused, Now.AddDays(-1), Now.AddDays(1)));

        var r = await svc.ListAsync(Q(PromotionDisplayStatus.Draft), isAdmin: false, default);

        Assert.Equal(1, r.Items.Single().Id);
        Assert.Equal(PromotionDisplayStatus.Running, r.Items.Single().DisplayStatus);
    }

    [Fact]
    public async Task PeriodFilter_ReturnsPromotionsOverlappingTheRange()
    {
        var svc = Create(
            Promo(1, PromotionStatus.Active, Now.AddDays(-10), Now.AddDays(-8)),
            Promo(2, PromotionStatus.Active, Now.AddDays(-1), Now.AddDays(1)),
            Promo(3, PromotionStatus.Active, Now.AddDays(5), Now.AddDays(6)));

        var r = await svc.ListAsync(
            Q(from: new DateOnly(2026, 10, 19), to: new DateOnly(2026, 10, 21)), true, default);

        Assert.Equal(new long[] { 2 }, r.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Paging_SplitsItemsAndReportsTotals()
    {
        var records = Enumerable.Range(1, 5)
            .Select(i => Promo(i, PromotionStatus.Active, Now.AddDays(-i), Now.AddDays(1)))
            .ToArray();

        var r = await Create(records).ListAsync(Q(page: 2, size: 2), true, default);

        Assert.Equal(5, r.TotalItems);
        Assert.Equal(3, r.TotalPages);
        Assert.Equal(2, r.Items.Count);
        Assert.Equal(2, r.Page);
    }

    [Fact]
    public async Task SampleDataSource_ShowsZeroUsage_AndAutoEndedPromotion()
    {
        var svc = Create(SamplePromotions.Create(Now).ToArray());

        var r = await svc.ListAsync(Q(), true, default);

        Assert.All(r.Items, p => Assert.Equal(0, p.UsageCount));
        Assert.Contains(r.Items, p => p.Status == PromotionStatus.Active && p.DisplayStatus == PromotionDisplayStatus.Ended);
        Assert.Contains(r.Items, p => p.DisplayStatus == PromotionDisplayStatus.Scheduled);
    }

    [Fact]
    public void Controllers_AllowOnlyAdminAndCashier()
    {
        foreach (var t in new[]
        {
            typeof(ConvenienceStore.Web.Modules.Promotions.Controllers.PromotionsApiController),
            typeof(ConvenienceStore.Web.Modules.Promotions.Controllers.PromotionsController),
        })
        {
            var attr = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute?)Attribute.GetCustomAttribute(
                t, typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute));
            Assert.Equal(PromotionRoles.AdminOrCashier, attr?.Roles);
        }
    }

    [Fact]
    public void Enums_SerializeAsUpperSnake()
    {
        Assert.Equal("\"FIXED_AMOUNT\"", System.Text.Json.JsonSerializer.Serialize(DiscountType.FixedAmount));
        Assert.Equal("\"RUNNING\"", System.Text.Json.JsonSerializer.Serialize(PromotionDisplayStatus.Running));
    }
}

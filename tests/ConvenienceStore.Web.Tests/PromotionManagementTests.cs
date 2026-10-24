using ConvenienceStore.Web.Modules.Promotions;
using ConvenienceStore.Web.Modules.Promotions.Controllers;
using ConvenienceStore.Web.Modules.Promotions.Models;
using ConvenienceStore.Web.Modules.Promotions.Services;
using Microsoft.AspNetCore.Authorization;

namespace ConvenienceStore.Web.Tests;

/// <summary>KM-02, KM-03.</summary>
public class PromotionManagementTests
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 10, 0, 0, Vn);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }

    private static (PromotionService Service, InMemoryPromotionDataSource Source) Create(
        params PromotionRecord[] seed)
    {
        var source = new InMemoryPromotionDataSource(seed);
        return (new PromotionService(source, new SampleProductLookup(), new FixedTime(Now)), source);
    }

    private static CreatePromotionRequest Valid(
        bool activate = false,
        DiscountType type = DiscountType.Percent,
        long value = 10,
        long? max = null,
        IReadOnlyList<long>? products = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        string? imageUrl = null) =>
        new("Giảm sữa", "mô tả", type, value, max, products ?? new long[] { 101 },
            start ?? Now.AddDays(1), end ?? Now.AddDays(3), false, activate, imageUrl);

    private static PromotionRecord Record(
        long id, PromotionStatus status, int usage = 0,
        DateTimeOffset? start = null, DateTimeOffset? end = null) =>
        new(id, $"KM-{id:0000}", "CT", null, DiscountType.Percent, 10, 5_000,
            new[] { SampleProductLookup.All[0] }, start ?? Now.AddDays(-1), end ?? Now.AddDays(5),
            false, status, usage);

    private static string[] Fields(PromotionResult<PromotionDto> r, string field) =>
        r.Error!.Fields![field];

    // ---------------- KM-02 ----------------

    [Fact]
    public async Task Create_SaveAsDraft_GeneratesCode_AndDraftStatus()
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(), default);

        Assert.True(r.IsSuccess);
        Assert.Equal("KM-0001", r.Value!.Code);
        Assert.Equal(PromotionStatus.Draft, r.Value.Status);
        Assert.Equal(PromotionDisplayStatus.Draft, r.Value.DisplayStatus);
        Assert.Equal(0, r.Value.UsageCount);
    }

    [Fact]
    public async Task Create_SaveAndActivate_IsScheduledUntilStartTime_A1()
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(activate: true), default);

        Assert.Equal(PromotionStatus.Active, r.Value!.Status);
        Assert.Equal(PromotionDisplayStatus.Scheduled, r.Value.DisplayStatus);
    }

    [Theory]
    [InlineData(DiscountType.Percent, 0)]
    [InlineData(DiscountType.Percent, 101)]
    [InlineData(DiscountType.Percent, -5)]
    [InlineData(DiscountType.FixedAmount, 0)]
    [InlineData(DiscountType.FixedAmount, 1_050)]
    public async Task Create_InvalidDiscountValue_Fails_E1(DiscountType type, long value)
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(type: type, value: value), default);

        Assert.Equal(PromotionErrorKind.Validation, r.Error!.Kind);
        Assert.NotEmpty(Fields(r, "discountValue"));
    }

    [Theory]
    [InlineData(DiscountType.Percent, 100)]
    [InlineData(DiscountType.Percent, 1)]
    [InlineData(DiscountType.FixedAmount, 1_000)]
    public async Task Create_BoundaryDiscountValues_AreAccepted(DiscountType type, long value)
    {
        var (svc, _) = Create();
        Assert.True((await svc.CreateAsync(Valid(type: type, value: value), default)).IsSuccess);
    }

    [Fact]
    public async Task Create_EndBeforeStart_Fails_E2()
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(start: Now.AddDays(3), end: Now.AddDays(1)), default);

        Assert.NotEmpty(Fields(r, "endAt"));
    }

    [Fact]
    public async Task Create_NoProducts_Fails_E3()
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(products: Array.Empty<long>()), default);

        Assert.NotEmpty(Fields(r, "productIds"));
    }

    [Fact]
    public async Task Create_UnknownProduct_Fails()
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(products: new long[] { 101, 99999 }), default);

        Assert.Contains("99999", Fields(r, "productIds")[0]);
    }

    [Fact]
    public async Task Create_MaxDiscountOnlyForPercent()
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(type: DiscountType.FixedAmount, value: 1_000, max: 5_000), default);

        Assert.NotEmpty(Fields(r, "maxDiscountAmount"));
    }

    [Fact]
    public async Task Create_ActivateWithEndInThePast_Fails()
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(
            Valid(activate: true, start: Now.AddDays(-5), end: Now.AddDays(-1)), default);

        Assert.Equal("PROMOTION_PAST_END_TIME", r.Error!.Code);
        Assert.Equal(PromotionErrorKind.Unprocessable, r.Error.Kind);
    }

    [Theory]
    [InlineData("https://example.com/a.png", true)]
    [InlineData("http://example.com/a.png", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:image/png;base64,AAAA", false)]
    [InlineData("ftp://example.com/a.png", false)]
    [InlineData("/relative/a.png", false)]
    [InlineData("not a url", false)]
    public async Task Create_ImageUrl_OnlyAcceptsHttpOrHttps(string url, bool ok)
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(imageUrl: url), default);

        Assert.Equal(ok, r.IsSuccess);
        if (ok) Assert.Equal(url, r.Value!.ImageUrl);
    }

    [Fact]
    public async Task Create_ImageUrlIsOptional()
    {
        var (svc, _) = Create();

        var r = await svc.CreateAsync(Valid(imageUrl: "  "), default);

        Assert.True(r.IsSuccess);
        Assert.Null(r.Value!.ImageUrl);
    }

    // ---------------- KM-03 ----------------

    private static UpdatePromotionRequest Update(
        string? name = null, DiscountType? type = null, long? value = null,
        DateTimeOffset? end = null, string? imageUrl = null, long? max = null) =>
        new(name, null, type, value, max, null, null, end, null, imageUrl);

    [Fact]
    public async Task Update_ChangesOnlyProvidedFields()
    {
        var (svc, _) = Create(Record(1, PromotionStatus.Draft));

        var r = await svc.UpdateAsync(1, Update(name: "Tên mới", value: 20), default);

        Assert.Equal("Tên mới", r.Value!.Name);
        Assert.Equal(20, r.Value.DiscountValue);
        Assert.Equal(5_000, r.Value.MaxDiscountAmount);
        Assert.Equal(DiscountType.Percent, r.Value.DiscountType);
    }

    [Fact]
    public async Task Update_AppliesSameValidationAsCreate()
    {
        var (svc, _) = Create(Record(1, PromotionStatus.Draft));

        var r = await svc.UpdateAsync(1, Update(value: 150), default);

        Assert.NotEmpty(Fields(r, "discountValue"));
    }

    [Fact]
    public async Task Update_SwitchToFixedAmount_DropsMaxDiscount()
    {
        var (svc, _) = Create(Record(1, PromotionStatus.Draft));

        var r = await svc.UpdateAsync(1, Update(type: DiscountType.FixedAmount, value: 2_000), default);

        Assert.True(r.IsSuccess);
        Assert.Null(r.Value!.MaxDiscountAmount);
    }

    [Fact]
    public async Task Update_AfterUsage_RejectsDiscountChange_BR_KM_03()
    {
        var (svc, source) = Create(Record(1, PromotionStatus.Active, usage: 3));

        var r = await svc.UpdateAsync(1, Update(value: 20), default);

        Assert.Equal("PROMOTION_HAS_USAGE", r.Error!.Code);
        Assert.Equal(PromotionErrorKind.Conflict, r.Error.Kind);
        Assert.Equal(10, (await source.GetByIdAsync(1, default))!.DiscountValue);
    }

    [Fact]
    public async Task Update_AfterUsage_AllowsNameDescriptionEndAndSameValues()
    {
        var (svc, _) = Create(Record(1, PromotionStatus.Active, usage: 3));
        var newEnd = Now.AddDays(9);

        // gửi lại cùng giá trị giảm không bị coi là thay đổi
        var r = await svc.UpdateAsync(
            1, new UpdatePromotionRequest("Tên mới", "mô tả mới", DiscountType.Percent, 10, 5_000,
                null, null, newEnd, null, null), default);

        Assert.True(r.IsSuccess);
        Assert.Equal("Tên mới", r.Value!.Name);
        Assert.Equal(newEnd, r.Value.EndAt);
    }

    [Fact]
    public async Task Update_EndedPromotion_IsRejected_IncludingAutoExpired()
    {
        var (svc, _) = Create(
            Record(1, PromotionStatus.Ended),
            Record(2, PromotionStatus.Active, start: Now.AddDays(-9), end: Now.AddDays(-1)));

        Assert.Equal("PROMOTION_ENDED", (await svc.UpdateAsync(1, Update(name: "x"), default)).Error!.Code);
        Assert.Equal("PROMOTION_ENDED", (await svc.UpdateAsync(2, Update(name: "x"), default)).Error!.Code);
    }

    [Fact]
    public async Task Update_NotFound()
    {
        var (svc, _) = Create();

        var r = await svc.UpdateAsync(99, Update(name: "x"), default);

        Assert.Equal(PromotionErrorKind.NotFound, r.Error!.Kind);
    }

    [Fact]
    public async Task Update_ImageUrl_CanBeSetAndCleared()
    {
        var (svc, _) = Create(Record(1, PromotionStatus.Draft));

        var set = await svc.UpdateAsync(1, Update(imageUrl: "https://example.com/x.jpg"), default);
        var cleared = await svc.UpdateAsync(1, Update(imageUrl: ""), default);
        var bad = await svc.UpdateAsync(1, Update(imageUrl: "javascript:alert(1)"), default);

        Assert.Equal("https://example.com/x.jpg", set.Value!.ImageUrl);
        Assert.Null(cleared.Value!.ImageUrl);
        Assert.NotEmpty(Fields(bad, "imageUrl"));
    }

    // ---------------- Quyền và chi tiết ----------------

    [Fact]
    public async Task Get_Cashier_OnlySeesRunningPromotion()
    {
        var (svc, _) = Create(
            Record(1, PromotionStatus.Active),
            Record(2, PromotionStatus.Draft));

        Assert.True((await svc.GetAsync(1, isAdmin: false, default)).IsSuccess);
        Assert.Equal(PromotionErrorKind.NotFound, (await svc.GetAsync(2, isAdmin: false, default)).Error!.Kind);
        Assert.True((await svc.GetAsync(2, isAdmin: true, default)).IsSuccess);
    }

    [Theory]
    [InlineData(nameof(PromotionsApiController.Create))]
    [InlineData(nameof(PromotionsApiController.Update))]
    public void ApiWriteActions_AreAdminOnly(string action)
    {
        var method = typeof(PromotionsApiController).GetMethod(action)!;
        var attr = (AuthorizeAttribute?)Attribute.GetCustomAttribute(method, typeof(AuthorizeAttribute));
        Assert.Equal(PromotionRoles.Admin, attr?.Roles);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Edit")]
    public void MvcWriteActions_AreAdminOnly(string action)
    {
        var methods = typeof(PromotionsController).GetMethods().Where(m => m.Name == action);
        Assert.All(methods, m =>
        {
            var attr = (AuthorizeAttribute?)Attribute.GetCustomAttribute(m, typeof(AuthorizeAttribute));
            Assert.Equal(PromotionRoles.Admin, attr?.Roles);
        });
    }

    // ---------------- Chọn sản phẩm (hàng nghìn sản phẩm) ----------------

    [Fact]
    public async Task ProductSearch_Pages_AndNeverReturnsEverything()
    {
        var lookup = new SampleProductLookup();

        var page = await lookup.SearchAsync(null, 1, 10, default);

        Assert.Equal(1000, page.TotalItems);
        Assert.Equal(100, page.TotalPages);
        Assert.Equal(10, page.Items.Count);
    }

    [Theory]
    [InlineData("sua", 101)]
    [InlineData("SỮA", 101)]
    [InlineData("nuoc-500", 102)]
    [InlineData("mì gói", 103)]
    public async Task ProductSearch_MatchesNameOrSku_IgnoringCaseAndAccents(string keyword, long expectedId)
    {
        var page = await new SampleProductLookup().SearchAsync(keyword, 1, 10, default);

        Assert.Contains(page.Items, p => p.Id == expectedId);
    }

    [Fact]
    public async Task ProductSearch_PageSizeIsCapped_AndNoMatchIsEmpty()
    {
        var lookup = new SampleProductLookup();

        var big = await lookup.SearchAsync(null, 1, 100_000, default);
        var none = await lookup.SearchAsync("khong-co-san-pham-nay", 1, 10, default);

        Assert.Equal(50, big.Items.Count);
        Assert.Empty(none.Items);
        Assert.Equal(0, none.TotalPages);
    }

    [Fact]
    public async Task Create_WithManyProducts_Works()
    {
        var (svc, _) = Create();
        var ids = Enumerable.Range(101, 500).Select(i => (long)i).ToArray();

        var r = await svc.CreateAsync(Valid(products: ids), default);

        Assert.True(r.IsSuccess);
        Assert.Equal(500, r.Value!.Products.Count);
    }
}

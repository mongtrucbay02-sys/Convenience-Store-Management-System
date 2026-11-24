using System.Net;
using System.Text.RegularExpressions;
using ConvenienceStore.Web.Modules.Inventory;
using ConvenienceStore.Web.Modules.Purchasing;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.Extensions.DependencyInjection;
namespace ConvenienceStore.Web.Tests.PostgreSql;

public sealed class PurchasingParityTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    private async Task<(long Supplier,long Other,long Actor,string Prefix)> Seed()
    {
        var suffix=Guid.NewGuid().ToString("N")[..8];
        var actor=Convert.ToInt64(await database.DocMotGiaTriAsync("INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id,bat_buoc_doi_mk) VALUES(@name,'unused','Purchasing test',3,FALSE) RETURNING id",("@name","purchasing_"+suffix)));
        async Task<long> Supplier(string code,string name,string phone)=>Convert.ToInt64(await database.DocMotGiaTriAsync("INSERT INTO nha_cung_cap(ma,ten,so_dien_thoai) VALUES(@code,@name,@phone) RETURNING id",("@code",code),("@name",name),("@phone",phone)));
        var supplier=await Supplier("NA"+suffix,"Công ty Sữa Ánh "+suffix,"0"+Random.Shared.Next(100000000,999999999));
        var other=await Supplier("NB"+suffix,"Khác "+suffix,"0"+Random.Shared.Next(100000000,999999999));
        return (supplier,other,actor,"NF"+suffix);
    }
    private static void Register(IServiceCollection services)
    {services.AddModuleInventory();services.AddScoped<PurchaseReceiptService>();}
    private async Task Receipt(string code,long supplier,long actor,string date)=>
        await database.ChayAsync("INSERT INTO phieu_nhap(ma_phieu,nha_cung_cap_id,ngay_nhap,nguoi_lap_id,tong_tien) VALUES(@code,@supplier,CAST(@date AS date),@actor,0)",("@code",code),("@supplier",supplier),("@date",date),("@actor",actor));

    [Fact]
    public async Task SM08_SupplierAndInclusiveDateFilters_CombineWithStatusAndStableOrder()
    {
        var seed=await Seed();
        await Receipt(seed.Prefix+"OLD",seed.Supplier,seed.Actor,"2026-10-31");
        await Receipt(seed.Prefix+"LOW",seed.Supplier,seed.Actor,"2026-11-01");
        await Receipt(seed.Prefix+"MID",seed.Supplier,seed.Actor,"2026-11-01");
        await Receipt(seed.Prefix+"END",seed.Supplier,seed.Actor,"2026-11-03");
        await Receipt(seed.Prefix+"NEW",seed.Supplier,seed.Actor,"2026-11-04");
        await Receipt(seed.Prefix+"OTH",seed.Other,seed.Actor,"2026-11-01");
        await using var app=await ListFeatureHost.Start(database,Register);
        using var client=await ListFeatureHost.Client(app,database,"NK");
        var path=$"/NhapHang?q={seed.Prefix}&supplierId={seed.Supplier}&from=2026-11-01&to=2026-11-03&status=%20NHAP%20";
        var html=await client.GetStringAsync(path);
        foreach(var code in new[]{"LOW","MID","END"})Assert.Contains(seed.Prefix+code,html);
        foreach(var code in new[]{"OLD","NEW","OTH"})Assert.DoesNotContain(seed.Prefix+code,html);
        Assert.True(html.IndexOf(seed.Prefix+"END",StringComparison.Ordinal)<html.IndexOf(seed.Prefix+"MID",StringComparison.Ordinal));
        Assert.True(html.IndexOf(seed.Prefix+"MID",StringComparison.Ordinal)<html.IndexOf(seed.Prefix+"LOW",StringComparison.Ordinal));
        var sameDay=await client.GetStringAsync(path.Replace("to=2026-11-03","to=2026-11-01"));
        Assert.Contains(seed.Prefix+"LOW",sameDay);Assert.Contains(seed.Prefix+"MID",sameDay);Assert.DoesNotContain(seed.Prefix+"END",sameDay);
        var cancelled=await client.GetStringAsync(path.Replace("%20NHAP%20","DA_HUY"));
        Assert.DoesNotContain(seed.Prefix+"LOW",cancelled);
    }

    [Theory]
    [InlineData("/NhapHang?supplierId=0")]
    [InlineData("/NhapHang?supplierId=-1")]
    [InlineData("/NhapHang?from=2026-11-03&to=2026-11-01")]
    [InlineData("/NhapHang?from=bad-date")]
    [InlineData("/NhapHang?status=INVALID")]
    [InlineData("/NhaCungCap?status=INVALID")]
    public async Task SM08_SM13_InvalidFiltersAreRejected(string path)
    {
        await using var app=await ListFeatureHost.Start(database,Register);
        using var client=await ListFeatureHost.Client(app,database,"AD");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("AD",200)]
    [InlineData("NK",200)]
    [InlineData("TN",403)]
    [InlineData("FORCED",403)]
    [InlineData(null,401)]
    public async Task SM08_SM13_RealSessionAndRoleGuardBothLists(string? role,int expected)
    {
        await using var app=await ListFeatureHost.Start(database,Register);
        using var client=await ListFeatureHost.Client(app,database,role);
        foreach(var path in new[]{"/NhapHang","/NhaCungCap"})Assert.Equal(expected,(int)(await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task SM13_SupplierSearchPreservesVietnameseCaseAndAccentInsensitiveBehavior()
    {
        var seed=await Seed();var suffix=seed.Prefix[2..];
        await using var app=await ListFeatureHost.Start(database,Register);
        using var client=await ListFeatureHost.Client(app,database,"AD");
        var html=await client.GetStringAsync("/NhaCungCap?q="+Uri.EscapeDataString("CONG TY SUA ANH "+suffix)+"&status=%20HOAT_DONG%20");
        Assert.Contains("NA"+suffix,html);Assert.DoesNotContain("NB"+suffix,html);
        var receipts=await client.GetAsync("/NhapHang?q="+Uri.EscapeDataString("CONG TY SUA ANH "+suffix));
        Assert.Equal(HttpStatusCode.OK,receipts.StatusCode);
    }

    [Fact]
    public async Task SM08_NextPagePreservesSupplierDateStatusAndSearchFilters()
    {
        var seed=await Seed();
        await database.ChayAsync("INSERT INTO phieu_nhap(ma_phieu,nha_cung_cap_id,ngay_nhap,nguoi_lap_id,tong_tien) SELECT @prefix||lpad(g::text,2,'0'),@supplier,'2026-11-02'::date,@actor,0 FROM generate_series(0,50) g",("@prefix",seed.Prefix),("@supplier",seed.Supplier),("@actor",seed.Actor));
        await Receipt(seed.Prefix+"OUT",seed.Other,seed.Actor,"2026-11-02");
        await using var app=await ListFeatureHost.Start(database,Register);
        using var client=await ListFeatureHost.Client(app,database,"AD");
        var html=await client.GetStringAsync($"/NhapHang?q={seed.Prefix}&supplierId={seed.Supplier}&from=2026-11-01&to=2026-11-03&status=NHAP");
        var link=Regex.Matches(html,"href=\"([^\"]+)\"").Select(m=>WebUtility.HtmlDecode(m.Groups[1].Value)).Single(h=>h.Contains("page=2",StringComparison.Ordinal));
        var next=await client.GetStringAsync(link);
        Assert.Contains(seed.Prefix+"00",next);Assert.DoesNotContain(seed.Prefix+"01",next);Assert.DoesNotContain(seed.Prefix+"OUT",next);
    }
}

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ConvenienceStore.Web.Tests.HaTang;

public sealed class GeneratedColumnsMetadataTests
{
    [Theory]
    [InlineData(typeof(CtPhieuNhap), nameof(CtPhieuNhap.ThanhTien), "so_luong * gia_nhap")]
    [InlineData(typeof(CtHoaDon), nameof(CtHoaDon.ThanhTien), "so_luong * (don_gia - giam_don_vi)")]
    [InlineData(typeof(CtPhieuDoiTra), nameof(CtPhieuDoiTra.ThanhTien), "so_luong * don_gia_thuc_tra")]
    [InlineData(typeof(CtPhieuDoiTra), nameof(CtPhieuDoiTra.GiaTriHuy), "(CASE WHEN cach_xu_ly = 'HUY' THEN  so_luong * gia_von ELSE  0 END)")]
    [InlineData(typeof(GioHang), nameof(GioHang.DangBanCuaThuNgan), "(CASE WHEN trang_thai = 'DANG_BAN' THEN  thu_ngan_id ELSE  NULL END)")]
    public void GeneratedProperty_IsDatabaseGenerated(Type entityType, string propertyName, string sql)
    {
        var options = new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=metadata_only;Username=unused")
            .Options;
        using var db = new CuaHangDbContext(options);

        var property = db.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
        Assert.Equal(PropertySaveBehavior.Ignore, property.GetBeforeSaveBehavior());
        Assert.Equal(PropertySaveBehavior.Ignore, property.GetAfterSaveBehavior());
        Assert.NotEmpty(sql);
        Assert.NotNull(property.GetComputedColumnSql());
        Assert.DoesNotContain("IF(", property.GetComputedColumnSql()!);
        Assert.True(property.GetIsStored());
    }
}

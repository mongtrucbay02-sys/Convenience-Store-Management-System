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
    [InlineData(typeof(CtPhieuDoiTra), nameof(CtPhieuDoiTra.GiaTriHuy), "IF(cach_xu_ly = 'HUY', so_luong * gia_von, 0)")]
    [InlineData(typeof(GioHang), nameof(GioHang.DangBanCuaThuNgan), "IF(trang_thai = 'DANG_BAN', thu_ngan_id, NULL)")]
    public void GeneratedProperty_IsDatabaseGenerated(Type entityType, string propertyName, string sql)
    {
        var options = new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseMySQL("server=localhost;database=metadata_only;user=test")
            .Options;
        using var db = new CuaHangDbContext(options);

        var property = db.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
        Assert.Equal(PropertySaveBehavior.Ignore, property.GetBeforeSaveBehavior());
        Assert.Equal(PropertySaveBehavior.Ignore, property.GetAfterSaveBehavior());
        Assert.Equal(sql, property.GetComputedColumnSql());
        Assert.True(property.GetIsStored());
    }
}

using System.Security.Claims;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Inventory.Controllers;
using ConvenienceStore.Web.Modules.Purchasing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConvenienceStore.Web.Tests.Inventory;

[Collection(CsdlKhoCollection.Ten)]
public sealed class PurchaseReceiptFlowTests(CsdlKhoFixture fixture)
{
    [Fact]
    public async Task ApprovingCountAndDisposalChangesStockOnlyAfterApproval()
    {
        var helper = new BoTroGoiDichVuKho(fixture);
        var productId = checked((uint)await helper.TaoSanPhamCoTonAsync("KHOAPP", 10, 8000m));
        var data = new DuLieuTestKho(fixture);

        var countId = await Act(async controller => GetId(await controller.CreateCount("CHON_SAN_PHAM", null, [productId], default)));
        await Act(async controller => Assert.IsType<RedirectToActionResult>(
            await controller.SaveCount(countId, 1, [new(productId, 8, "Đếm lại")], true, default)));
        Assert.Equal(10, (await data.DocTonKhoAsync((int)productId))!.SoLuongTon);

        await Act(async controller => Assert.IsType<RedirectToActionResult>(await controller.ReviewCount(countId, true, null, default)));
        Assert.Equal(new TonKhoDoc(8, 8000m, 5), await data.DocTonKhoAsync((int)productId));
        await Act(async controller => Assert.IsType<ConflictObjectResult>(await controller.ReviewCount(countId, true, null, default)));

        var disposalId = await Act(async controller => GetId(await controller.CreateDisposal([new(productId, 2, "HONG", null)], true, default)));
        Assert.Equal(8, (await data.DocTonKhoAsync((int)productId))!.SoLuongTon);
        await Act(async controller => Assert.IsType<RedirectToActionResult>(await controller.ReviewDisposal(disposalId, true, null, default)));
        Assert.Equal(new TonKhoDoc(6, 8000m, 5), await data.DocTonKhoAsync((int)productId));
        await Act(async controller => Assert.IsType<ConflictObjectResult>(await controller.ReviewDisposal(disposalId, true, null, default)));
        Assert.Equal(1, await data.DemTheKhoAsync((int)productId, "KIEM_KE"));
        Assert.Equal(1, await data.DemTheKhoAsync((int)productId, "XUAT_HUY"));
    }

    private static uint GetId(IActionResult result) => Convert.ToUInt32(
        Assert.IsType<RedirectToActionResult>(result).RouteValues!["id"]);

    private async Task<T> Act<T>(Func<StockDocumentsController, Task<T>> action)
    {
        await using CuaHangDbContext db = fixture.TaoDbContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, fixture.TaiKhoanAdminId.ToString()), new Claim(ClaimTypes.Role, "AD")],
            "test"));
        var controller = new StockDocumentsController(db, BoTroGoiDichVuKho.TaoDichVu(db), TimeProvider.System)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
        return await action(controller);
    }

    [Fact]
    public async Task ConfirmingDraftUpdatesStockOnceAndRecalculatesAverageCost()
    {
        var stock = new BoTroGoiDichVuKho(fixture);
        var productId = await stock.TaoSanPhamCoTonAsync("NHREAL", 10, 8000m);
        await fixture.ChayAsync("""
            INSERT INTO nha_cung_cap (ma, ten, so_dien_thoai)
            VALUES ('NCC-NHREAL','Nhà cung cấp kiểm thử','0901234567')
            """);
        var supplierId = Convert.ToUInt32(await fixture.DocMotGiaTriAsync(
            "SELECT id FROM nha_cung_cap WHERE ma='NCC-NHREAL'"));

        uint receiptId;
        await using (var db = fixture.TaoDbContext())
        {
            var service = new PurchaseReceiptService(db, BoTroGoiDichVuKho.TaoDichVu(db), TimeProvider.System);
            receiptId = await service.CreateAsync(new PurchaseReceiptInput
            {
                SupplierId = supplierId,
                Lines = [new((uint)productId, 12, 9500), new((uint)productId, 8, 9500)]
            }, fixture.TaiKhoanKhoId, default);
        }

        await using (var db = fixture.TaoDbContext())
        {
            var receipt = await db.PhieuNhaps.Include(row => row.CtPhieuNhaps).SingleAsync(row => row.Id == receiptId);
            Assert.Equal("NHAP", receipt.TrangThai);
            Assert.Equal(20u, Assert.Single(receipt.CtPhieuNhaps).SoLuong);
            Assert.Equal(190000ul, receipt.TongTien);
        }

        await using (var db = fixture.TaoDbContext())
        {
            var service = new PurchaseReceiptService(db, BoTroGoiDichVuKho.TaoDichVu(db), TimeProvider.System);
            await service.ConfirmAsync(receiptId, fixture.TaiKhoanKhoId, default);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ConfirmAsync(receiptId, fixture.TaiKhoanKhoId, default));
        }

        var after = await new DuLieuTestKho(fixture).DocTonKhoAsync(productId);
        Assert.NotNull(after);
        Assert.Equal(30, after.SoLuongTon);
        Assert.Equal(9000m, after.GiaVonBinhQuan);
        Assert.Equal("DA_NHAP_KHO", await fixture.DocMotGiaTriAsync(
            "SELECT trang_thai FROM phieu_nhap WHERE id=@id", ("@id", receiptId)));
        Assert.Equal(1L, Convert.ToInt64(await fixture.DocMotGiaTriAsync(
            "SELECT COUNT(*) FROM the_kho WHERE loai_bien_dong='NHAP_HANG' AND chung_tu_id=@id",
            ("@id", receiptId))));
    }
}

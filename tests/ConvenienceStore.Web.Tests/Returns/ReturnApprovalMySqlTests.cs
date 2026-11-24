using ConvenienceStore.Web.Modules.Returns.Services;
using ConvenienceStore.Web.Shared.Errors;
using ConvenienceStore.Web.Tests.Inventory;

namespace ConvenienceStore.Web.Tests.Returns;

[Collection(CsdlKhoCollection.Ten)]
public sealed class ReturnApprovalMySqlTests(CsdlKhoFixture fixture)
{
    [Fact]
    public async Task ConcurrentApprovals_OneDecisionSucceedsAndTheOtherConflicts()
    {
        long id = await SeedPendingExchangeAsync();
        var outcomes = await Task.WhenAll(ApproveAsync(id), ApproveAsync(id));

        Assert.Single(outcomes, error => error is null);
        Assert.Equal(409, Assert.Single(outcomes, error => error is not null)!.MaHttp);
        Assert.Equal("DA_DUYET", await fixture.DocMotGiaTriAsync(
            "SELECT trang_thai FROM phieu_doi_tra WHERE id=@id", ("@id", id)));
        Assert.Equal(fixture.TaiKhoanAdminId, Convert.ToInt32(await fixture.DocMotGiaTriAsync(
            "SELECT nguoi_duyet_id FROM phieu_doi_tra WHERE id=@id", ("@id", id))));
    }

    [Fact]
    public async Task Reject_RemovesSlipFromPendingListAndPersistsNote()
    {
        long id = await SeedPendingExchangeAsync();
        await using var db = fixture.TaoDbContext();
        var service = new ReturnApprovalService(db, null!, null!, null!, null!, TimeProvider.System);

        await service.DecideAsync(id, fixture.TaiKhoanAdminId, false, "Từ chối phiếu mẫu", false, default);

        Assert.DoesNotContain(await service.PendingAsync(default), slip => slip.Id == id);
        var details = await service.DetailsAsync(id, null, default);
        Assert.Equal("BI_TU_CHOI", details.Slip.Status);
        Assert.Equal("Từ chối phiếu mẫu", details.Slip.ApprovalNote);
    }

    private async Task<LoiNghiepVu?> ApproveAsync(long id)
    {
        await using var db = fixture.TaoDbContext();
        var service = new ReturnApprovalService(db, null!, null!, null!, null!, TimeProvider.System);
        try
        {
            await service.DecideAsync(id, fixture.TaiKhoanAdminId, true, "Duyệt phiếu mẫu", false, default);
            return null;
        }
        catch (LoiNghiepVu error)
        {
            return error;
        }
    }

    private async Task<long> SeedPendingExchangeAsync()
    {
        var cashier = fixture.TaiKhoanThuNganId;
        var code = Guid.NewGuid().ToString("N")[..12];
        long cart = Convert.ToInt64(await fixture.DocMotGiaTriAsync("""
            INSERT INTO gio_hang (thu_ngan_id, trang_thai, hoan_tat_luc)
            VALUES (@cashier, 'DA_HOAN_TAT', NOW());
            SELECT LAST_INSERT_ID();
            """, ("@cashier", cashier)));
        long invoice = Convert.ToInt64(await fixture.DocMotGiaTriAsync("""
            INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, thanh_toan_luc,
                tong_tien_hang, tong_giam, tong_thanh_toan, tien_khach_dua, tien_thua)
            VALUES (@code, @cart, @cashier, NOW(), 10000, 0, 10000, 10000, 0);
            SELECT LAST_INSERT_ID();
            """, ("@code", $"HD{code}"), ("@cart", cart), ("@cashier", cashier)));
        long policy = Convert.ToInt64(await fixture.DocMotGiaTriAsync("""
            INSERT INTO chinh_sach_doi_tra (hieu_luc_tu) VALUES (NOW());
            SELECT LAST_INSERT_ID();
            """));
        return Convert.ToInt64(await fixture.DocMotGiaTriAsync("""
            INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, chinh_sach_id, trang_thai,
                can_duyet_danh_muc, gia_tri_hang_tra, tien_thu_them, thu_ngan_id)
            VALUES (@code, 'DOI_HANG', @invoice, @policy, 'CHO_DUYET', TRUE, 10000, 5000, @cashier);
            SELECT LAST_INSERT_ID();
            """, ("@code", $"DT{code}"), ("@invoice", invoice), ("@policy", policy), ("@cashier", cashier)));
    }
}

using ConvenienceStore.Web.Modules.Promotions.Models;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

public interface IPromotionService
{
    /// <param name="isAdmin">true: Admin/Quản lý xem tất cả; false: Thu ngân chỉ thấy chương trình đang chạy.</param>
    Task<PromotionListResponse> ListAsync(
        PromotionQuery query, bool isAdmin, CancellationToken cancellationToken);
}

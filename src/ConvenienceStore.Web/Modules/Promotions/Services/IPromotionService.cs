using ConvenienceStore.Web.Modules.Promotions.Models;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

public interface IPromotionService
{
    /// <param name="isAdmin">true: Admin/Quản lý xem tất cả; false: Thu ngân chỉ thấy chương trình đang chạy.</param>
    Task<PromotionListResponse> ListAsync(
        PromotionQuery query, bool isAdmin, CancellationToken cancellationToken);

    /// <summary>Chi tiết một chương trình. Thu ngân chỉ xem được chương trình đang chạy.</summary>
    Task<PromotionResult<PromotionDto>> GetAsync(
        long id, bool isAdmin, CancellationToken cancellationToken);

    /// <summary>KM-02. Chỉ Admin (kiểm tra quyền ở controller).</summary>
    Task<PromotionResult<PromotionDto>> CreateAsync(
        CreatePromotionRequest request, CancellationToken cancellationToken);

    /// <summary>KM-03. Chỉ Admin.</summary>
    Task<PromotionResult<PromotionDto>> UpdateAsync(
        long id, UpdatePromotionRequest request, CancellationToken cancellationToken);

    /// <summary>KM-04. Chỉ Admin.</summary>
    Task<PromotionResult<PromotionDto>> ChangeStatusAsync(
        long id, PromotionTargetStatus target, CancellationToken cancellationToken);
}

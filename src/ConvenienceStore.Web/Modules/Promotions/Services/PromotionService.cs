using ConvenienceStore.Web.Modules.Promotions.Models;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

public sealed class PromotionService : IPromotionService
{
    // Ngày theo giờ Việt Nam (UTC+7), cùng quy ước với module báo cáo (BR-BC-03).
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private const int MaxImageUrlLength = 2048;

    private readonly IPromotionDataSource _dataSource;
    private readonly IProductLookup _products;
    private readonly TimeProvider _timeProvider;

    public PromotionService(
        IPromotionDataSource dataSource, IProductLookup products, TimeProvider timeProvider)
    {
        _dataSource = dataSource;
        _products = products;
        _timeProvider = timeProvider;
    }

    // ---------------- KM-01 ----------------

    public async Task<PromotionListResponse> ListAsync(
        PromotionQuery query, bool isAdmin, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var records = await _dataSource.GetAllAsync(cancellationToken);

        var items = records
            .Select(r => ToDto(r, now))
            .Where(p => MatchesStatus(p, query.DisplayStatus, isAdmin))
            .Where(p => MatchesPeriod(p, query.From, query.To))
            .OrderByDescending(p => p.StartAt)
            .ThenBy(p => p.Code, StringComparer.Ordinal)
            .ToList();

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(items.Count / (double)pageSize);

        return new PromotionListResponse(
            page,
            pageSize,
            items.Count,
            totalPages,
            items.Skip((page - 1) * pageSize).Take(pageSize).ToList());
    }

    public async Task<PromotionResult<PromotionDto>> GetAsync(
        long id, bool isAdmin, CancellationToken cancellationToken)
    {
        var record = await _dataSource.GetByIdAsync(id, cancellationToken);
        if (record is null) return NotFound();

        var dto = ToDto(record, _timeProvider.GetUtcNow());
        // Thu ngân không biết chương trình chưa chạy tồn tại.
        if (!isAdmin && dto.DisplayStatus != PromotionDisplayStatus.Running) return NotFound();
        return PromotionResult<PromotionDto>.Ok(dto);
    }

    /// <summary>BR-KM-07, PA-KM-05.A: suy ra trạng thái hiển thị khi truy vấn, không dùng tác vụ nền.</summary>
    public static PromotionDisplayStatus GetDisplayStatus(
        PromotionStatus status, DateTimeOffset startAt, DateTimeOffset endAt, DateTimeOffset now) =>
        status switch
        {
            PromotionStatus.Draft => PromotionDisplayStatus.Draft,
            PromotionStatus.Paused => PromotionDisplayStatus.Paused,
            PromotionStatus.Ended => PromotionDisplayStatus.Ended,
            _ when now < startAt => PromotionDisplayStatus.Scheduled,
            _ when now <= endAt => PromotionDisplayStatus.Running,
            _ => PromotionDisplayStatus.Ended,
        };

    // ---------------- KM-02 ----------------

    public async Task<PromotionResult<PromotionDto>> CreateAsync(
        CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var draft = new Draft(
            request.Name, request.Description, request.DiscountType, request.DiscountValue,
            request.MaxDiscountAmount, request.ProductIds, request.StartAt, request.EndAt, request.ImageUrl);

        var errors = new Errors();
        var products = await ValidateAsync(draft, errors, cancellationToken);
        if (errors.Any) return ValidationFail(errors);

        var now = _timeProvider.GetUtcNow();
        // Lưu và kích hoạt một chương trình đã quá hạn là vô nghĩa (cùng E1 của KM-04).
        if (request.Activate && request.EndAt!.Value <= now) return PastEndTime();

        var created = await _dataSource.CreateAsync(
            new PromotionRecord(
                0, string.Empty, request.Name!.Trim(), Clean(request.Description),
                request.DiscountType!.Value, request.DiscountValue!.Value,
                request.DiscountType == DiscountType.Percent ? request.MaxDiscountAmount : null,
                products!, request.StartAt!.Value, request.EndAt!.Value, request.MembersOnly,
                request.Activate ? PromotionStatus.Active : PromotionStatus.Draft, 0,
                Clean(request.ImageUrl)),
            cancellationToken);

        return PromotionResult<PromotionDto>.Ok(ToDto(created, now));
    }

    // ---------------- KM-03 ----------------

    public async Task<PromotionResult<PromotionDto>> UpdateAsync(
        long id, UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        var record = await _dataSource.GetByIdAsync(id, cancellationToken);
        if (record is null) return NotFound();

        var now = _timeProvider.GetUtcNow();
        if (GetDisplayStatus(record.Status, record.StartAt, record.EndAt, now) == PromotionDisplayStatus.Ended)
            return Ended();

        var newType = request.DiscountType ?? record.DiscountType;
        var newValue = request.DiscountValue ?? record.DiscountValue;
        var newStart = request.StartAt ?? record.StartAt;
        var newEnd = request.EndAt ?? record.EndAt;
        var newMembersOnly = request.MembersOnly ?? record.MembersOnly;
        var newProductIds = request.ProductIds ?? record.Products.Select(p => p.Id).ToList();

        // maxDiscountAmount: null = giữ nguyên, 0 = bỏ; đổi sang loại số tiền thì tự bỏ mức giảm tối đa.
        long? newMax = request.MaxDiscountAmount switch
        {
            null => record.MaxDiscountAmount,
            0 => null,
            var v => v,
        };
        if (newType == DiscountType.FixedAmount && request.MaxDiscountAmount is null) newMax = null;

        // BR-KM-03: sau khi có lượt dùng, chỉ được sửa tên, mô tả, thời gian kết thúc (và ảnh minh họa).
        if (record.UsageCount > 0)
        {
            var restrictedChanged =
                newType != record.DiscountType
                || newValue != record.DiscountValue
                || newMax != record.MaxDiscountAmount
                || newStart != record.StartAt
                || newMembersOnly != record.MembersOnly
                || !newProductIds.OrderBy(x => x).SequenceEqual(record.Products.Select(p => p.Id).OrderBy(x => x));
            if (restrictedChanged)
            {
                return PromotionResult<PromotionDto>.Fail(new PromotionError(
                    PromotionErrorKind.Conflict,
                    "PROMOTION_HAS_USAGE",
                    "Chương trình đã có lượt sử dụng, chỉ được sửa tên, mô tả và thời gian kết thúc. " +
                    "Hãy kết thúc chương trình này và tạo chương trình mới."));
            }
        }

        var name = request.Name ?? record.Name;
        var description = request.Description is null ? record.Description : Clean(request.Description);
        var imageUrl = request.ImageUrl is null ? record.ImageUrl : Clean(request.ImageUrl);

        var errors = new Errors();
        var products = await ValidateAsync(
            new Draft(name, description, newType, newValue, newMax, newProductIds, newStart, newEnd, imageUrl),
            errors, cancellationToken);
        if (errors.Any) return ValidationFail(errors);

        var updated = record with
        {
            Name = name.Trim(),
            Description = description,
            DiscountType = newType,
            DiscountValue = newValue,
            MaxDiscountAmount = newMax,
            Products = products!,
            StartAt = newStart,
            EndAt = newEnd,
            MembersOnly = newMembersOnly,
            ImageUrl = imageUrl,
        };

        if (!await _dataSource.UpdateAsync(updated, cancellationToken)) return NotFound();
        return PromotionResult<PromotionDto>.Ok(ToDto(updated, now));
    }

    // ---------------- Kiểm tra dữ liệu (KM-02, dùng lại ở KM-03) ----------------

    private sealed record Draft(
        string? Name,
        string? Description,
        DiscountType? Type,
        long? Value,
        long? MaxDiscount,
        IReadOnlyList<long>? ProductIds,
        DateTimeOffset? StartAt,
        DateTimeOffset? EndAt,
        string? ImageUrl);

    private sealed class Errors
    {
        private readonly Dictionary<string, List<string>> _items = new();
        public bool Any => _items.Count > 0;

        public void Add(string field, string message)
        {
            if (!_items.TryGetValue(field, out var list)) _items[field] = list = new List<string>();
            list.Add(message);
        }

        public IReadOnlyDictionary<string, string[]> ToDictionary() =>
            _items.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }

    private async Task<IReadOnlyList<ProductRefDto>?> ValidateAsync(
        Draft d, Errors errors, CancellationToken cancellationToken)
    {
        var name = d.Name?.Trim();
        if (string.IsNullOrEmpty(name)) errors.Add("name", "Tên không được để trống.");
        else if (name.Length > 200) errors.Add("name", "Tên tối đa 200 ký tự.");

        if (d.Type is null) errors.Add("discountType", "Chọn loại giảm.");

        // E1
        if (d.Value is null || d.Value < 1) errors.Add("discountValue", "Giá trị phải lớn hơn 0.");
        else if (d.Type == DiscountType.Percent && d.Value > 100)
            errors.Add("discountValue", "Giá trị phần trăm phải trong khoảng (0, 100].");
        else if (d.Type == DiscountType.FixedAmount && d.Value % 100 != 0)
            errors.Add("discountValue", "Số tiền phải là bội số của 100 đồng.");

        if (d.MaxDiscount is not null)
        {
            if (d.Type != DiscountType.Percent)
                errors.Add("maxDiscountAmount", "Mức giảm tối đa chỉ dùng cho loại phần trăm.");
            else if (d.MaxDiscount < 1)
                errors.Add("maxDiscountAmount", "Mức giảm tối đa phải lớn hơn 0.");
        }

        // E3
        IReadOnlyList<ProductRefDto>? products = null;
        if (d.ProductIds is null || d.ProductIds.Count == 0)
        {
            errors.Add("productIds", "Chọn ít nhất một sản phẩm.");
        }
        else if (d.ProductIds.Distinct().Count() != d.ProductIds.Count)
        {
            errors.Add("productIds", "Danh sách sản phẩm bị trùng.");
        }
        else
        {
            var found = await _products.GetByIdsAsync(d.ProductIds, cancellationToken);
            var foundIds = found.Select(p => p.Id).ToHashSet();
            var missing = d.ProductIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missing.Count > 0)
                errors.Add("productIds", $"Sản phẩm không tồn tại: {string.Join(", ", missing)}.");
            else
                products = found;
        }

        if (d.StartAt is null) errors.Add("startAt", "Nhập thời gian bắt đầu.");
        if (d.EndAt is null) errors.Add("endAt", "Nhập thời gian kết thúc.");
        // E2
        if (d.StartAt is { } s && d.EndAt is { } e && e <= s)
            errors.Add("endAt", "Thời gian kết thúc phải sau thời gian bắt đầu.");

        if (!string.IsNullOrWhiteSpace(d.ImageUrl) && !IsValidImageUrl(d.ImageUrl.Trim()))
            errors.Add("imageUrl", "Liên kết ảnh phải là địa chỉ http hoặc https hợp lệ (tối đa 2048 ký tự).");

        return products;
    }

    /// <summary>Chỉ nhận http/https tuyệt đối; chặn javascript:, data:, file: ...</summary>
    public static bool IsValidImageUrl(string url) =>
        url.Length <= MaxImageUrlLength
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && !string.IsNullOrEmpty(uri.Host);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ---------------- Lỗi và ánh xạ ----------------

    private static PromotionResult<PromotionDto> NotFound() =>
        PromotionResult<PromotionDto>.Fail(new PromotionError(
            PromotionErrorKind.NotFound, "PROMOTION_NOT_FOUND", "Không tìm thấy chương trình khuyến mãi."));

    private static PromotionResult<PromotionDto> Ended() =>
        PromotionResult<PromotionDto>.Fail(new PromotionError(
            PromotionErrorKind.Conflict, "PROMOTION_ENDED", "Chương trình đã kết thúc, không thể thay đổi."));

    private static PromotionResult<PromotionDto> PastEndTime() =>
        PromotionResult<PromotionDto>.Fail(new PromotionError(
            PromotionErrorKind.Unprocessable, "PROMOTION_PAST_END_TIME",
            "Thời gian kết thúc đã qua. Hãy sửa thời gian trước khi kích hoạt."));

    private static PromotionResult<PromotionDto> ValidationFail(Errors errors) =>
        PromotionResult<PromotionDto>.Fail(new PromotionError(
            PromotionErrorKind.Validation, "VALIDATION_ERROR", "Dữ liệu không hợp lệ.", errors.ToDictionary()));

    private static PromotionDto ToDto(PromotionRecord r, DateTimeOffset now) => new(
        r.Id, r.Code, r.Name, r.Description, r.DiscountType, r.DiscountValue, r.MaxDiscountAmount,
        r.Products, r.StartAt, r.EndAt, r.MembersOnly, r.Status,
        GetDisplayStatus(r.Status, r.StartAt, r.EndAt, now),
        r.UsageCount, r.ImageUrl);

    // TN luôn chỉ thấy chương trình đang chạy, bất kể bộ lọc gửi lên.
    private static bool MatchesStatus(PromotionDto p, PromotionDisplayStatus? filter, bool isAdmin) =>
        !isAdmin
            ? p.DisplayStatus == PromotionDisplayStatus.Running
            : filter is null || p.DisplayStatus == filter;

    // Lấy chương trình có thời gian hiệu lực giao với [from, to] (cả ngày to).
    private static bool MatchesPeriod(PromotionDto p, DateOnly? from, DateOnly? to)
    {
        if (from is { } f && p.EndAt < StartOfDay(f)) return false;
        if (to is { } t && p.StartAt >= StartOfDay(t.AddDays(1))) return false;
        return true;
    }

    private static DateTimeOffset StartOfDay(DateOnly d) =>
        new(d.ToDateTime(TimeOnly.MinValue), VietnamOffset);
}

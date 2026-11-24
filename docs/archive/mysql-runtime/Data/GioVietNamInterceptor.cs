using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ConvenienceStore.Web.Data;

/// <summary>
/// Đặt múi giờ phiên MySQL là giờ Việt Nam (+07:00) mỗi khi EF Core mở kết nối.
/// </summary>
/// <remarks>
/// CSDL lưu cột DATETIME theo giờ Việt Nam (SRS 6.4, BR-BC-03, ADR-001 mục 3.3).
/// Các giá trị mặc định CURRENT_TIMESTAMP, NOW() trong view và trigger dùng múi giờ
/// của phiên, nên mọi kết nối phải chạy SET time_zone = '+07:00' trước khi dùng.
/// </remarks>
public sealed class GioVietNamInterceptor : DbConnectionInterceptor
{
    /// <summary>Câu lệnh đặt múi giờ; dùng chung cho mã kiểm thử mở kết nối trực tiếp.</summary>
    public const string CauLenhDatMuiGio = "SET time_zone = '+07:00'";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var lenh = connection.CreateCommand();
        lenh.CommandText = CauLenhDatMuiGio;
        lenh.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var lenh = connection.CreateCommand();
        lenh.CommandText = CauLenhDatMuiGio;
        await lenh.ExecuteNonQueryAsync(cancellationToken);
    }
}

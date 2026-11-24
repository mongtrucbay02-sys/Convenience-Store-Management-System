namespace ConvenienceStore.Web.Shared.Api;

public static class GioVietNam
{
    public static readonly TimeSpan DoLech = TimeSpan.FromHours(7);

    public static DateTimeOffset ThanhOffset(DateTime giaTri) =>
        giaTri.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(giaTri).ToOffset(DoLech)
            : new(DateTime.SpecifyKind(giaTri, DateTimeKind.Unspecified), DoLech);

    public static string MaNgayChungTu(DateTime timestamp) =>
        ThanhOffset(timestamp).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

    public static DateTimeOffset? ThanhOffset(DateTime? giaTri) =>
        giaTri is null ? null : ThanhOffset(giaTri.Value);
}

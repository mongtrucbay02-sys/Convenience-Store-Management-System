namespace ConvenienceStore.Web.Shared.Api;

public static class GioVietNam
{
    public static readonly TimeSpan DoLech = TimeSpan.FromHours(7);

    public static DateTimeOffset ThanhOffset(DateTime giaTri) =>
        new(DateTime.SpecifyKind(giaTri, DateTimeKind.Unspecified), DoLech);

    public static DateTimeOffset? ThanhOffset(DateTime? giaTri) =>
        giaTri is null ? null : ThanhOffset(giaTri.Value);
}

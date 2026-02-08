namespace Libs.Extensions;

public static class DateTimeExtensions
{
    public static string ToEntsoeDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyyMMddHHmm");
    }

}

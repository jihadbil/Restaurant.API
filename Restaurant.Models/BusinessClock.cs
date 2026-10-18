namespace Restaurant.Models;

/// <summary>Database timestamps are local restaurant wall time; JWT expiry remains UTC.</summary>
public static class BusinessClock
{
    private static TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Tripoli");
    public static void Configure(string timeZoneId) => zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
    public static DateTime Today => Now.Date;
}

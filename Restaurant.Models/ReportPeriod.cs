namespace Restaurant.Models;

public readonly record struct ReportPeriod(DateTime Start, DateTime EndExclusive)
{
    public static ReportPeriod Create(DateTime start, DateTime end)
    {
        start = start.Date;
        end = end.Date;
        if (start > end || end >= DateTime.MaxValue.Date || (end - start).TotalDays >= 366)
            throw new ArgumentException("اختر فترة صحيحة لا تتجاوز 366 يوماً، ونهاية قبل 9999-12-31.");
        return new(start, end.AddDays(1));
    }
}

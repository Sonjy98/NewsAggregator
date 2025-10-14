using NewsFeedBackend.Enums;

namespace NewsFeedBackend.Extensions;

public static class TimeWindowExtensions
{
    public static string ToCanonical(this TimeWindow w) => w switch
    {
        TimeWindow.Day   => "24h",
        TimeWindow.Week  => "7d",
        TimeWindow.Month => "30d",
        _ => throw new ArgumentOutOfRangeException(nameof(w))
    };

    public static string ToFromDate(this TimeWindow w, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return w switch
        {
            TimeWindow.Day   => now.AddDays(-1).ToString("yyyy-MM-dd"),
            TimeWindow.Week  => now.AddDays(-7).ToString("yyyy-MM-dd"),
            TimeWindow.Month => now.AddDays(-30).ToString("yyyy-MM-dd"),
            _ => throw new ArgumentOutOfRangeException(nameof(w))
        };
    }
    public static bool TryParse(string? input, out TimeWindow window)
    {
        window = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var t = input.Trim().ToLowerInvariant();
        window = t switch
        {
            "24h" or "1d" or "day"      => TimeWindow.Day,
            "7d" or "week" or "7days"   => TimeWindow.Week,
            "30d" or "month"            => TimeWindow.Month,
            _ => default
        };
        return t is "24h" or "1d" or "day" or "7d" or "week" or "7days" or "30d" or "month";
    }
}

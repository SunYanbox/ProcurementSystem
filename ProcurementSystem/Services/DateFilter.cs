namespace ProcurementSystem.Services;

// 统一解析日期过滤参数：DateTime.TryParse 会受服务器本地时区影响，
// 且纯日期结束值只解析到当天 00:00，导致当天其余记录被漏掉。
public static class DateFilter
{
    public static bool TryParseFrom(string? value, out DateTime utcStart)
    {
        utcStart = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (DateOnly.TryParse(value, out var dateOnly))
        {
            utcStart = DateTime.SpecifyKind(dateOnly.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            return true;
        }

        if (DateTimeOffset.TryParse(value, out var dto))
        {
            utcStart = dto.UtcDateTime;
            return true;
        }

        return false;
    }

    public static bool TryParseTo(string? value, out DateTime utcEnd)
    {
        utcEnd = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (DateOnly.TryParse(value, out var dateOnly))
        {
            // 纯日期结束边界应包含当天全部时间，而不是当天 00:00
            utcEnd = DateTime.SpecifyKind(dateOnly.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
            return true;
        }

        if (DateTimeOffset.TryParse(value, out var dto))
        {
            utcEnd = dto.UtcDateTime;
            return true;
        }

        return false;
    }
}

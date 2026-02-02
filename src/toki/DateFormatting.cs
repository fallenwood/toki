namespace Toki;

internal static class DateFormatting {
  internal static string FormatDate(DateTimeOffset date, DateOptions options) {
    try {
      var culture = System.Globalization.CultureInfo.GetCultureInfo(options.Locale);
      return date.ToString(options.Format, culture);
    } catch {
      return date.ToString(options.Format);
    }
  }

  internal static string? FormatRelativeDate(DateTimeOffset date, DateOptions options) {
    if (options.RelativeDays <= 0) {
      return null;
    }

    var now = DateTimeOffset.Now;
    var span = now - date;
    if (span.TotalSeconds < 0) {
      return null;
    }

    if (span.TotalDays > options.RelativeDays) {
      return null;
    }

    return options.Locale switch {
      "zh-CN" or "zh-Hans" => FormatRelativeZh(span),
      _ => FormatRelativeEn(span)
    };
  }

  private static string FormatRelativeZh(TimeSpan span) {
    if (span.TotalSeconds < 60) {
      var seconds = Math.Max(1, (int)span.TotalSeconds);
      return $"{seconds}秒前";
    }
    if (span.TotalMinutes < 60) {
      return $"{(int)span.TotalMinutes}分钟前";
    }
    if (span.TotalHours < 24) {
      return $"{(int)span.TotalHours}小时前";
    }
    return $"{(int)span.TotalDays}天前";
  }

  private static string FormatRelativeEn(TimeSpan span) {
    if (span.TotalSeconds < 60) {
      var seconds = Math.Max(1, (int)span.TotalSeconds);
      return $"{seconds} seconds ago";
    }
    if (span.TotalMinutes < 60) {
      return $"{(int)span.TotalMinutes} minutes ago";
    }
    if (span.TotalHours < 24) {
      return $"{(int)span.TotalHours} hours ago";
    }
    return $"{(int)span.TotalDays} days ago";
  }
}

namespace Toki;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ZLinq;

internal static partial class SearchIndexGenerator {
  // Reuse compiled regexes to strip HTML tags & whitespace
  [GeneratedRegex("<[^>]+>")]
  private static partial Regex HtmlTagRegex();
  private static readonly Regex htmlTagRegex = HtmlTagRegex();

  internal static void Generate(string publicDir, IEnumerable<ContentItem> posts, IEnumerable<ContentItem> pages, string indexPath = "search-index.json") {
    var searchEntries = posts
      .AsValueEnumerable()
      .Concat(pages)
      .Select(ToSearchEntry)
      // Deterministic ordering improves diffing & tests
      .OrderByDescending(entry => entry.Date)
      .ThenBy(entry => entry.Url, StringComparer.Ordinal)
      .ToList();

    var safeIndexPath = string.IsNullOrWhiteSpace(indexPath) ? "search-index.json" : indexPath.TrimStart('/', '\\');
    var destination = Path.Combine(publicDir, safeIndexPath);
    var directory = Path.GetDirectoryName(destination);
    if (!string.IsNullOrWhiteSpace(directory)) {
      Directory.CreateDirectory(directory);
    }

    using var stream = File.Create(destination);
    // Use source-generated context to avoid reflection (AOT/trimming safe)
    JsonSerializer.Serialize(stream, searchEntries, SearchIndexJsonContext.Default.ListSearchIndexEntry);
  }

  private static SearchIndexEntry ToSearchEntry(ContentItem item) {
    // Prefer description (already plain text) and fall back to stripped HTML
    var plainDescription = string.IsNullOrWhiteSpace(item.Description)
      ? ToPlainText(item.Excerpt ?? item.Html)
      : item.Description;

    // Use excerpt for indexing; fallback to full content trimmed to a reasonable size
    var excerptText = ToPlainText(item.Excerpt ?? string.Empty);
    var contentText = excerptText.Length > 0
      ? excerptText
      : TrimTo(ToPlainText(item.Html), 8_000);

    return new SearchIndexEntry(
      Title: item.Title,
      Url: item.Url,
      Description: plainDescription,
      Date: item.Date,
      Tags: item.Tags,
      Categories: item.Categories,
      Content: contentText,
      Hash: item.Hash
    );
  }

  private static string ToPlainText(string html) {
    if (string.IsNullOrWhiteSpace(html)) return string.Empty;
    var noTags = htmlTagRegex.Replace(html, " ");
    var decoded = System.Net.WebUtility.HtmlDecode(noTags);
    var normalized = Regex.Replace(decoded, "\\s+", " ").Trim();
    return normalized;
  }

  private static string TrimTo(string input, int maxLength) {
    if (string.IsNullOrEmpty(input)) return string.Empty;
    if (input.Length <= maxLength) return input;
    return input[..maxLength].TrimEnd() + "…";
  }
}

internal sealed record SearchIndexEntry(
  string Title,
  string Url,
  string Description,
  DateTimeOffset Date,
  List<string> Tags,
  List<string> Categories,
  string Content,
  string Hash
);

namespace Toki;

using Markdig;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using ZLinq;

internal static partial class ContentLoader {
  internal static List<ContentItem> LoadPosts(string postsDir, MarkdownPipeline pipeline, DateOptions dateOptions, ILogger logger) {
    var results = new List<ContentItem>();
    if (!Directory.Exists(postsDir)) {
      return results;
    }

    foreach (var file in Directory.GetFiles(postsDir, "*.md", SearchOption.AllDirectories)) {
      var item = LoadMarkdownItem(file, pipeline, dateOptions, logger, isPost: true);
      results.Add(item);
    }

    return results
      .AsValueEnumerable()
      .OrderByDescending(p => p.Date)
      .ToList();
  }

  internal static List<ContentItem> LoadPages(string sourceDir, string postsDir, MarkdownPipeline pipeline, DateOptions dateOptions, ILogger logger) {
    var results = new List<ContentItem>();
    if (!Directory.Exists(sourceDir)) {
      return results;
    }

    foreach (var file in Directory.GetFiles(sourceDir, "*.md", SearchOption.AllDirectories)) {
      if (Path.GetFullPath(file).StartsWith(Path.GetFullPath(postsDir), StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      var item = LoadMarkdownItem(file, pipeline, dateOptions, logger, isPost: false);
      results.Add(item);
    }

    return results;
  }

  [GeneratedRegex(@"<!--\s*more\s*-->", RegexOptions.IgnoreCase)]
  private static partial Regex MoreTagRegex();
  private static readonly Regex moreTagRegex = MoreTagRegex();

  internal static ContentItem LoadMarkdownItem(string file, MarkdownPipeline pipeline, DateOptions dateOptions, ILogger logger, bool isPost) {
    var raw = File.ReadAllText(file);
    var (frontMatter, body) = FrontMatter.ParseFrontMatter(raw, logger);

    var title = frontMatter.Title ?? Path.GetFileNameWithoutExtension(file);
    var layout = frontMatter.Layout ?? (isPost ? "post" : "page");
    var slug = frontMatter.Slug ?? Slugify(title);
    var date = frontMatter.Date ?? File.GetLastWriteTimeUtc(file);
    var tags = frontMatter.Tags ?? [];
    var categories = frontMatter.Categories ?? [];

    var html = Markdown.ToHtml(body, pipeline);

    string excerptHtml;
    var match = moreTagRegex.Match(body);
    if (match.Success) {
      var excerptMarkdown = body.Substring(0, match.Index);
      excerptHtml = Markdown.ToHtml(excerptMarkdown, pipeline);
    } else {
      excerptHtml = html;
    }

    var dateDisplay = DateFormatting.FormatDate(date, dateOptions);
    var dateRelative = DateFormatting.FormatRelativeDate(date, dateOptions);
    logger.LogInformation("DateRelative for '{Title}': {DateRelative}", title, dateRelative ?? "(none)");
    var outputPath = isPost
      ? Path.Combine(date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"), slug, "index.html")
      : ResolvePageOutput(file, slug);

    var url = isPost
      ? $"/{date:yyyy/MM/dd}/{slug}/"
      : ResolvePageUrl(file, slug);

    var hash = Hasher.ComputeMd5(url);

    return new ContentItem(
      SourcePath: file,
      Title: title,
      Slug: slug,
      Layout: layout,
      Date: date,
      DateDisplay: dateDisplay,
      DateRelative: dateRelative,
      Html: html,
      Excerpt: excerptHtml,
      OutputPath: outputPath,
      Url: url,
      Tags: tags,
      Categories: categories,
      Hash: hash
    );
  }

  private static string Slugify(string input) {
    var lower = input.Trim().ToLowerInvariant();
    var slug = System.Text.RegularExpressions.Regex.Replace(lower, "[^a-z0-9]+", "-");
    slug = System.Text.RegularExpressions.Regex.Replace(slug, "-+", "-");
    return slug.Trim('-');
  }

  private static string ResolvePageOutput(string file, string slug) {
    var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "source");
    var relative = Path.GetRelativePath(sourceDir, file);
    var directory = Path.GetDirectoryName(relative) ?? string.Empty;
    var fileName = Path.GetFileNameWithoutExtension(relative);

    if (string.Equals(fileName, "index", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(directory)) {
      return "index.html";
    }

    var segments = new List<string>();
    if (!string.IsNullOrEmpty(directory)) {
      segments.Add(directory);
    }

    // If the file is named "index.md" and is in a subdirectory, don't add the filename again
    if (!string.Equals(fileName, "index", StringComparison.OrdinalIgnoreCase)) {
      segments.Add(string.IsNullOrEmpty(slug) ? fileName : slug);
    }

    segments.Add("index.html");
    return Path.Combine(segments.ToArray());
  }

  private static string ResolvePageUrl(string file, string slug) {
    var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "source");
    var relative = Path.GetRelativePath(sourceDir, file);
    var directory = Path.GetDirectoryName(relative) ?? string.Empty;
    var fileName = Path.GetFileNameWithoutExtension(relative);

    if (string.Equals(fileName, "index", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(directory)) {
      return "/";
    }

    var segments = new List<string>();
    if (!string.IsNullOrEmpty(directory)) {
      segments.Add(directory.Replace("\\", "/"));
    }

    // If the file is named "index.md" and is in a subdirectory, don't add the filename again
    if (!string.Equals(fileName, "index", StringComparison.OrdinalIgnoreCase)) {
      segments.Add(string.IsNullOrEmpty(slug) ? fileName : slug);
    }

    return $"/{string.Join("/", segments)}/";
  }
}

internal record ContentItem(
  string SourcePath,
  string Title,
  string Slug,
  string Layout,
  DateTimeOffset Date,
  string DateDisplay,
  string? DateRelative,
  string Html,
  string? Excerpt,
  string OutputPath,
  string Url,
  List<string> Tags,
  List<string> Categories,
  string Hash) {
  public ContentPageViewModel ToPageModel() => new ContentPageViewModel {
    Title = Title,
    Slug = Slug,
    Layout = Layout,
    Date = DateDisplay,
    DateIso = Date.ToString("O"),
    DateRelative = DateRelative ?? string.Empty,
    Url = Url,
    Tags = Tags,
    Categories = Categories,
    Content = Html,
    Excerpt = Excerpt ?? string.Empty,
    Hash = Hash,
  };
}

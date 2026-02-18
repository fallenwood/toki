namespace Toki;

using System.Text;
using System.Xml;
using ZLinq;

internal static class SiteGenerator {
  internal static void GenerateIndexPages(MiniJinja.Environment env, List<ContentItem> posts, SiteViewModel siteModel, string publicDir, PagingOptions paging) {
    if (!TemplateEngine.TemplateExists(env, "index.html")) {
      return;
    }

    var totalPages = GetTotalPages(posts.Count, paging.PerPage);
    for (var page = 1; page <= totalPages; page++) {
      var pagePosts = GetPageItems(posts, page, paging.PerPage)
        .AsValueEnumerable()
        .Select(p => p.ToPageModel())
        .ToList();
      var outputPath = ResolvePagedOutputPath(publicDir, string.Empty, page);
      TemplateEngine.RenderToFile(env, "index.html", outputPath, new IndexPageModel {
        Site = siteModel,
        Posts = pagePosts,
        Pagination = BuildPaginationViewModel("/", page, totalPages)
      });
    }
  }
  internal static void CopyStaticAssets(string sourceDir, string postsDir, string publicDir) {
    foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)) {
      if (file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      if (Path.GetFullPath(file).StartsWith(Path.GetFullPath(postsDir), StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      var relative = Path.GetRelativePath(sourceDir, file);
      var destination = Path.Combine(publicDir, relative);
      var directory = Path.GetDirectoryName(destination);
      if (!string.IsNullOrWhiteSpace(directory)) {
        Directory.CreateDirectory(directory);
      }
      File.Copy(file, destination, overwrite: true);
    }
  }

  internal static void CopyThemeAssets(string themeDistDir, string publicDir) {
    if (!Directory.Exists(themeDistDir)) {
      return;
    }

    foreach (var file in Directory.GetFiles(themeDistDir, "*", SearchOption.AllDirectories)) {
      var relative = Path.GetRelativePath(themeDistDir, file);
      var destination = Path.Combine(publicDir, "assets", relative);
      var directory = Path.GetDirectoryName(destination);
      if (!string.IsNullOrWhiteSpace(directory)) {
        Directory.CreateDirectory(directory);
      }
      File.Copy(file, destination, overwrite: true);
    }
  }

  internal static void GenerateAtomFeed(List<ContentItem> posts, SiteConfig siteConfig, string publicDir) {
    var feedPath = Path.Combine(publicDir, "atom.xml");
    var baseUrl = NormalizeBaseUrl(siteConfig.BaseUrl);

    var settings = new XmlWriterSettings {
      Indent = true,
      Encoding = Encoding.UTF8
    };

    using var writer = XmlWriter.Create(feedPath, settings);
    writer.WriteStartDocument();
    writer.WriteStartElement("feed", "http://www.w3.org/2005/Atom");

    writer.WriteElementString("title", siteConfig.Title);
    writer.WriteElementString("id", baseUrl);
    writer.WriteElementString("updated", (posts.AsValueEnumerable().FirstOrDefault()?.Date ?? DateTimeOffset.UtcNow).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"));
    writer.WriteStartElement("link");
    writer.WriteAttributeString("href", baseUrl);
    writer.WriteEndElement();
    writer.WriteStartElement("link");
    writer.WriteAttributeString("href", BuildAbsoluteUrl(baseUrl, "/atom.xml"));
    writer.WriteAttributeString("rel", "self");
    writer.WriteEndElement();

    writer.WriteStartElement("author");
    writer.WriteElementString("name", siteConfig.Author);
    writer.WriteEndElement();

    writer.WriteStartElement("generator");
    writer.WriteAttributeString("uri", "");
    writer.WriteString("Toki");
    writer.WriteEndElement();

    if (!string.IsNullOrWhiteSpace(siteConfig.Description)) {
      writer.WriteElementString("subtitle", siteConfig.Description);
    }

    foreach (var post in posts) {
      writer.WriteStartElement("entry");
      writer.WriteElementString("title", post.Title);
      var absoluteUrl = BuildAbsoluteUrl(baseUrl, post.Url);
      writer.WriteElementString("id", absoluteUrl);
      writer.WriteStartElement("link");
      writer.WriteAttributeString("href", absoluteUrl);
      writer.WriteEndElement();
      writer.WriteElementString("updated", post.Date.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"));
      writer.WriteStartElement("summary");
      writer.WriteAttributeString("type", "html");
      writer.WriteString(post.Excerpt);
      writer.WriteEndElement();

      foreach (var tag in post.Tags) {
        writer.WriteStartElement("category");
        writer.WriteAttributeString("term", tag);
        writer.WriteAttributeString("scheme", BuildAbsoluteUrl(baseUrl, $"/tags/{Slugify(tag)}/"));
        writer.WriteEndElement();
      }

      writer.WriteEndElement();
    }

    writer.WriteEndElement();
    writer.WriteEndDocument();
  }

  internal static void GenerateTagPages(MiniJinja.Environment env, List<ContentItem> posts, SiteViewModel siteModel, string publicDir, PagingOptions paging) {
    var tagGroups = posts
      .AsValueEnumerable()
      .SelectMany(post => post.Tags.AsValueEnumerable().Select(tag => (tag, post)))
      .GroupBy(pair => pair.tag, StringComparer.OrdinalIgnoreCase)
      .OrderBy(group => group.Key)
      .ToList();

    if (tagGroups.Count == 0) {
      return;
    }

    var tagsIndexPath = Path.Combine(publicDir, "tags", "index.html");
    if (TemplateEngine.TemplateExists(env, "tags.html")) {
      TemplateEngine.RenderToFile(env, "tags.html", tagsIndexPath, new TagsIndexModel {
        Site = siteModel,
        Tags = tagGroups.AsValueEnumerable().Select(group => new TagViewModel {
          Name = group.Key,
          Count = group.AsValueEnumerable().Count(),
          Url = $"/tags/{Slugify(group.Key)}/"
        }).ToList()
      });
    } else {
      var tags = tagGroups.AsValueEnumerable().Select(group => new TagInfo(
        Name: group.Key,
        Count: group.AsValueEnumerable().Count(),
        Url: $"/tags/{Slugify(group.Key)}/"
      )).ToList();
      RenderTagIndexFallback(tagsIndexPath, siteModel.I18n.Tags, tags);
    }

    foreach (var group in tagGroups) {
      var tagSlug = Slugify(group.Key);
      var tagPosts = group.AsValueEnumerable().Select(pair => pair.post).ToList();
      var totalPages = GetTotalPages(tagPosts.Count, paging.PerPage);
      for (var page = 1; page <= totalPages; page++) {
        var baseUrl = $"/tags/{tagSlug}/";
        var outputPath = ResolvePagedOutputPath(publicDir, Path.Combine("tags", tagSlug), page);
        var pagePosts = GetPageItems(tagPosts, page, paging.PerPage);

        if (TemplateEngine.TemplateExists(env, "tag.html")) {
          TemplateEngine.RenderToFile(env, "tag.html", outputPath, new TagPageModel {
            Site = siteModel,
            Tag = group.Key,
            Posts = pagePosts.AsValueEnumerable().Select(p => p.ToPageModel()).ToList(),
            Pagination = BuildPaginationViewModel(baseUrl, page, totalPages)
          });
        } else {
          var postItems = pagePosts.AsValueEnumerable().Select(p => new PostInfo(
            Title: p.Title,
            Url: p.Url,
            Date: p.Date.ToString("o")
          )).ToList();
          RenderTagPostsFallback(outputPath, $"{siteModel.I18n.TagPrefix}: {group.Key}", postItems, BuildPagination(baseUrl, page, totalPages));
        }
      }
    }
  }

  internal static void GenerateSearchPage(MiniJinja.Environment env, SiteViewModel siteModel, string publicDir, SearchPluginConfig searchConfig) {
    if (!searchConfig.Enabled) {
      return;
    }

    var outputPath = Path.Combine(publicDir, "search", "index.html");
    var searchModel = new SearchPageModel {
      Site = siteModel,
      Search = new SearchPageConfigViewModel {
        Provider = searchConfig.Provider,
        IndexPath = searchConfig.IndexPath,
        MinChars = searchConfig.MinChars,
        Limit = searchConfig.Limit,
        Fuzzy = searchConfig.Fuzzy
      }
    };

    if (TemplateEngine.TemplateExists(env, "search.html")) {
      TemplateEngine.RenderToFile(env, "search.html", outputPath, searchModel);
      return;
    }

    RenderSearchPageFallback(outputPath, searchModel);
  }

  private static void RenderSearchPageFallback(string outputPath, SearchPageModel model) {
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory)) {
      Directory.CreateDirectory(directory);
    }

    var builder = new StringBuilder();
    builder.AppendLine("<!DOCTYPE html>");
    builder.AppendLine("<html lang=\"en\">");
    builder.AppendLine("<head><meta charset=\"utf-8\" /><title>Search</title></head>");
    builder.AppendLine("<body>");
    builder.AppendLine("<h1>Search</h1>");
    builder.AppendLine("<input type=\"search\" id=\"search-input\" placeholder=\"Search…\" />");
    builder.AppendLine("<ul id=\"search-results\"></ul>");
    builder.AppendLine("<script>console.warn('Using fallback search page; provide search.html template for better UX.');</script>");
    builder.AppendLine("</body></html>");

    File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
  }

  internal static void GenerateCategoryPages(MiniJinja.Environment env, List<ContentItem> posts, SiteViewModel siteModel, string publicDir, PagingOptions paging) {
    if (posts.Count == 0) {
      return;
    }

    var archiveGroups = posts
      .AsValueEnumerable()
      .GroupBy(post => post.Date.Year)
      .OrderByDescending(group => group.Key)
      .Select(group => new CategoryArchiveYearViewModel {
        Year = group.Key,
        Count = group.AsValueEnumerable().Count(),
        Posts = group
          .AsValueEnumerable()
          .OrderByDescending(post => post.Date)
          .Select(post => new CategoryArchivePostViewModel {
            Title = post.Title,
            Url = post.Url,
            MonthDay = post.Date.ToString("MM-dd")
          })
          .ToList()
      })
      .ToList();

    var categoryGroups = posts
      .AsValueEnumerable()
      .SelectMany(post => post.Categories.AsValueEnumerable().Select(category => (category, post)))
      .GroupBy(pair => pair.category, StringComparer.OrdinalIgnoreCase)
      .OrderBy(group => group.Key)
      .ToList();

    var categoriesIndexPath = Path.Combine(publicDir, "categories", "index.html");
    if (TemplateEngine.TemplateExists(env, "categories.html")) {
      TemplateEngine.RenderToFile(env, "categories.html", categoriesIndexPath, new CategoriesIndexModel {
        Site = siteModel,
        Categories = categoryGroups.AsValueEnumerable().Select(group => new CategoryViewModel {
          Name = group.Key,
          Count = group.AsValueEnumerable().Count(),
          Url = $"/categories/{Slugify(group.Key)}/"
        }).ToList(),
        TotalPosts = posts.Count,
        Archives = archiveGroups
      });
    } else {
      var categories = categoryGroups.AsValueEnumerable().Select(group => new CategoryInfo(
        Name: group.Key,
        Count: group.AsValueEnumerable().Count(),
        Url: $"/categories/{Slugify(group.Key)}/"
      )).ToList();
      RenderCategoryIndexFallback(categoriesIndexPath, siteModel.I18n.Categories, categories);
    }

    if (categoryGroups.Count == 0) {
      return;
    }

    foreach (var group in categoryGroups) {
      var categorySlug = Slugify(group.Key);
      var categoryPosts = group.AsValueEnumerable().Select(pair => pair.post).ToList();
      var totalPages = GetTotalPages(categoryPosts.Count, paging.PerPage);
      for (var page = 1; page <= totalPages; page++) {
        var baseUrl = $"/categories/{categorySlug}/";
        var outputPath = ResolvePagedOutputPath(publicDir, Path.Combine("categories", categorySlug), page);
        var pagePosts = GetPageItems(categoryPosts, page, paging.PerPage);

        if (TemplateEngine.TemplateExists(env, "category.html")) {
          TemplateEngine.RenderToFile(env, "category.html", outputPath, new CategoryPageModel {
            Site = siteModel,
            Category = group.Key,
            Posts = pagePosts.AsValueEnumerable().Select(p => p.ToPageModel()).ToList(),
            Pagination = BuildPaginationViewModel(baseUrl, page, totalPages)
          });
        } else {
          var postItems = pagePosts.AsValueEnumerable().Select(p => new PostInfo(
            Title: p.Title,
            Url: p.Url,
            Date: p.Date.ToString("o")
          )).ToList();
          RenderCategoryPostsFallback(outputPath, $"{siteModel.I18n.CategoryPrefix}: {group.Key}", postItems, BuildPagination(baseUrl, page, totalPages));
        }
      }
    }
  }

  private static void RenderTagIndexFallback(string outputPath, string title, List<TagInfo> tags) {
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory)) {
      Directory.CreateDirectory(directory);
    }

    var builder = new StringBuilder();
    builder.AppendLine("<!DOCTYPE html>");
    builder.AppendLine("<html lang=\"en\">");
    builder.AppendLine("<head>");
    builder.AppendLine("  <meta charset=\"utf-8\" />");
    builder.AppendLine($"  <title>{System.Net.WebUtility.HtmlEncode(title)}</title>");
    builder.AppendLine("</head>");
    builder.AppendLine("<body>");
    builder.AppendLine($"  <h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>");
    builder.AppendLine("  <ul>");

    foreach (var tag in tags) {
      builder.AppendLine($"    <li><a href=\"{System.Net.WebUtility.HtmlEncode(tag.Url)}\">{System.Net.WebUtility.HtmlEncode(tag.Name)}</a> ({tag.Count})</li>");
    }

    builder.AppendLine("  </ul>");
    builder.AppendLine("</body>");
    builder.AppendLine("</html>");

    File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
  }

  private static void RenderTagPostsFallback(string outputPath, string title, List<PostInfo> posts, PaginationModel pagination) {
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory)) {
      Directory.CreateDirectory(directory);
    }

    var builder = new StringBuilder();
    builder.AppendLine("<!DOCTYPE html>");
    builder.AppendLine("<html lang=\"en\">");
    builder.AppendLine("<head>");
    builder.AppendLine("  <meta charset=\"utf-8\" />");
    builder.AppendLine($"  <title>{System.Net.WebUtility.HtmlEncode(title)}</title>");
    builder.AppendLine("</head>");
    builder.AppendLine("<body>");
    builder.AppendLine($"  <h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>");
    builder.AppendLine("  <ul>");

    foreach (var post in posts) {
      builder.AppendLine($"    <li><a href=\"{System.Net.WebUtility.HtmlEncode(post.Url)}\">{System.Net.WebUtility.HtmlEncode(post.Title)}</a></li>");
    }

    builder.AppendLine("  </ul>");
    builder.AppendLine(RenderPaginationHtml(pagination));
    builder.AppendLine("</body>");
    builder.AppendLine("</html>");

    File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
  }

  private static void RenderCategoryIndexFallback(string outputPath, string title, List<CategoryInfo> categories) {
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory)) {
      Directory.CreateDirectory(directory);
    }

    var builder = new StringBuilder();
    builder.AppendLine("<!DOCTYPE html>");
    builder.AppendLine("<html lang=\"en\">");
    builder.AppendLine("<head>");
    builder.AppendLine("  <meta charset=\"utf-8\" />");
    builder.AppendLine($"  <title>{System.Net.WebUtility.HtmlEncode(title)}</title>");
    builder.AppendLine("</head>");
    builder.AppendLine("<body>");
    builder.AppendLine($"  <h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>");
    builder.AppendLine("  <ul>");

    foreach (var category in categories) {
      builder.AppendLine($"    <li><a href=\"{System.Net.WebUtility.HtmlEncode(category.Url)}\">{System.Net.WebUtility.HtmlEncode(category.Name)}</a> ({category.Count})</li>");
    }

    builder.AppendLine("  </ul>");
    builder.AppendLine("</body>");
    builder.AppendLine("</html>");

    File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
  }

  private static void RenderCategoryPostsFallback(string outputPath, string title, List<PostInfo> posts, PaginationModel pagination) {
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory)) {
      Directory.CreateDirectory(directory);
    }

    var builder = new StringBuilder();
    builder.AppendLine("<!DOCTYPE html>");
    builder.AppendLine("<html lang=\"en\">");
    builder.AppendLine("<head>");
    builder.AppendLine("  <meta charset=\"utf-8\" />");
    builder.AppendLine($"  <title>{System.Net.WebUtility.HtmlEncode(title)}</title>");
    builder.AppendLine("</head>");
    builder.AppendLine("<body>");
    builder.AppendLine($"  <h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>");
    builder.AppendLine("  <ul>");

    foreach (var post in posts) {
      builder.AppendLine($"    <li><a href=\"{System.Net.WebUtility.HtmlEncode(post.Url)}\">{System.Net.WebUtility.HtmlEncode(post.Title)}</a></li>");
    }

    builder.AppendLine("  </ul>");
    builder.AppendLine(RenderPaginationHtml(pagination));
    builder.AppendLine("</body>");
    builder.AppendLine("</html>");

    File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
  }

  private static string NormalizeBaseUrl(string baseUrl) {
    if (string.IsNullOrWhiteSpace(baseUrl)) {
      return "/";
    }

    return baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";
  }

  private static string BuildAbsoluteUrl(string baseUrl, string path) {
    if (string.IsNullOrWhiteSpace(path)) {
      return baseUrl;
    }

    var trimmedBase = NormalizeBaseUrl(baseUrl);
    var trimmedPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;
    return trimmedBase + trimmedPath;
  }

  private static string Slugify(string input) {
    var lower = input.Trim().ToLowerInvariant();
    var slug = System.Text.RegularExpressions.Regex.Replace(lower, "[^a-z0-9]+", "-");
    slug = System.Text.RegularExpressions.Regex.Replace(slug, "-+", "-");
    return slug.Trim('-');
  }

  private static int GetTotalPages(int totalItems, int perPage) {
    if (totalItems <= 0) {
      return 1;
    }
    return (int)Math.Ceiling(totalItems / (double)perPage);
  }

  private static List<ContentItem> GetPageItems(List<ContentItem> items, int page, int perPage) {
    return items
      .AsValueEnumerable()
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .ToList();
  }

  private static PaginationModel BuildPagination(string baseUrl, int currentPage, int totalPages) {
    var normalized = EnsureTrailingSlash(baseUrl);
    var prevUrl = currentPage > 1 ? BuildPageUrl(normalized, currentPage - 1) : null;
    var nextUrl = currentPage < totalPages ? BuildPageUrl(normalized, currentPage + 1) : null;
    var items = BuildPaginationItems(normalized, currentPage, totalPages);
    return new PaginationModel(currentPage, totalPages, prevUrl, nextUrl, normalized, items);
  }

  private static PaginationViewModel BuildPaginationViewModel(string baseUrl, int currentPage, int totalPages) {
    var normalized = EnsureTrailingSlash(baseUrl);
    var prevUrl = currentPage > 1 ? BuildPageUrl(normalized, currentPage - 1) : null;
    var nextUrl = currentPage < totalPages ? BuildPageUrl(normalized, currentPage + 1) : null;
    var items = BuildPaginationItems(normalized, currentPage, totalPages).AsValueEnumerable()
      .Select(item => new PaginationItemViewModel {
        Label = item.Label,
        Url = item.Url ?? "",
        IsCurrent = item.IsCurrent,
        IsEllipsis = item.IsEllipsis
      })
      .ToList();

    return new PaginationViewModel {
      Current = currentPage,
      Total = totalPages,
      PrevUrl = prevUrl ?? "",
      NextUrl = nextUrl ?? "",
      Items = items
    };
  }

  private static string BuildPageUrl(string baseUrl, int page) {
    if (page <= 1) {
      return baseUrl;
    }
    return $"{EnsureTrailingSlash(baseUrl)}page/{page}/";
  }

  private static string EnsureTrailingSlash(string url) {
    return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
  }

  private static string ResolvePagedOutputPath(string publicDir, string relativeBase, int page) {
    var baseSegments = string.IsNullOrWhiteSpace(relativeBase)
      ? Array.Empty<string>()
      : relativeBase.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

    if (page <= 1) {
      var segments = baseSegments.Length == 0 ? new[] { "index.html" } : baseSegments.Concat(new[] { "index.html" }).ToArray();
      return Path.Combine(new[] { publicDir }.AsValueEnumerable().Concat(segments).ToArray());
    }

    var pagedSegments = baseSegments.Length == 0
      ? new[] { "page", page.ToString(), "index.html" }
      : baseSegments.Concat(new[] { "page", page.ToString(), "index.html" }).ToArray();
    return Path.Combine(new[] { publicDir }.AsValueEnumerable().Concat(pagedSegments).ToArray());
  }

  private static string RenderPaginationHtml(PaginationModel pagination) {
    if (pagination.TotalPages <= 1) {
      return string.Empty;
    }

    var builder = new StringBuilder();
    builder.AppendLine("  <div style=\"margin-top:16px\">");
    builder.AppendLine("    <div class=\"join\">");
    if (!string.IsNullOrWhiteSpace(pagination.PrevUrl)) {
      builder.AppendLine($"      <a class=\"btn btn-sm join-item\" href=\"{System.Net.WebUtility.HtmlEncode(pagination.PrevUrl)}\">‹</a>");
    }
    foreach (var item in pagination.Items) {
      if (item.IsEllipsis) {
        builder.AppendLine("      <span class=\"btn btn-sm btn-ghost join-item\">…</span>");
        continue;
      }
      var activeClass = item.IsCurrent ? " btn-active" : string.Empty;
      builder.AppendLine($"      <a class=\"btn btn-sm join-item{activeClass}\" href=\"{System.Net.WebUtility.HtmlEncode(item.Url ?? "#")}\">{item.Label}</a>");
    }
    if (!string.IsNullOrWhiteSpace(pagination.NextUrl)) {
      builder.AppendLine($"      <a class=\"btn btn-sm join-item\" href=\"{System.Net.WebUtility.HtmlEncode(pagination.NextUrl)}\">›</a>");
    }
    builder.AppendLine("    </div>");
    builder.AppendLine("  </div>");
    return builder.ToString();
  }

  private static List<PaginationItem> BuildPaginationItems(string baseUrl, int currentPage, int totalPages) {
    var items = new List<PaginationItem>();
    if (totalPages <= 1) {
      items.Add(new PaginationItem("1", BuildPageUrl(baseUrl, 1), true, false));
      return items;
    }

    var pages = new SortedSet<int> { 1, totalPages };
    for (var i = currentPage - 1; i <= currentPage + 1; i++) {
      if (i >= 1 && i <= totalPages) {
        pages.Add(i);
      }
    }

    var previous = 0;
    foreach (var page in pages) {
      if (previous != 0 && page - previous > 1) {
        items.Add(new PaginationItem("…", null, false, true));
      }
      items.Add(new PaginationItem(page.ToString(), BuildPageUrl(baseUrl, page), page == currentPage, false));
      previous = page;
    }

    return items;
  }
}

internal record TagInfo(string Name, int Count, string Url);

internal record PostInfo(string Title, string Url, string Date);

internal record CategoryInfo(string Name, int Count, string Url);

internal record PaginationModel(int current, int total, string? prevUrl, string? nextUrl, string baseUrl, List<PaginationItem> items) {
  public int CurrentPage => current;
  public int TotalPages => total;
  public string? PrevUrl => prevUrl;
  public string? NextUrl => nextUrl;
  public List<PaginationItem> Items => items;
}

internal record PaginationItem(string Label, string? Url, bool IsCurrent, bool IsEllipsis);

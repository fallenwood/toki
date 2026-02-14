namespace Toki;

using MiniJinja;

/// <summary>
/// Model for site configuration used in templates.
/// </summary>
[MiniJinjaContext]
internal sealed partial class SiteViewModel : ITemplateSerializable {
  public required string Title { get; init; }
  public required string Description { get; init; }
  public required string Author { get; init; }
  public required string BaseUrl { get; init; }
  public required string Theme { get; init; }
  public required GitalkViewModel Gitalk { get; init; }
  public required NavViewModel Nav { get; init; }
  public required FooterViewModel Footer { get; init; }
  public required PluginsViewModel Plugins { get; init; }
  public required DateOptionsViewModel Date { get; init; }
  public required SidebarViewModel Sidebar { get; init; }
  public required PagingViewModel Paging { get; init; }
  public required I18nViewModel I18n { get; init; }
}

[MiniJinjaContext]
internal sealed partial class GitalkViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string ClientId { get; init; }
  public required string ClientSecret { get; init; }
  public required string Repo { get; init; }
  public required string Owner { get; init; }
  public required List<string> Admin { get; init; }
}

[MiniJinjaContext]
internal sealed partial class NavViewModel : ITemplateSerializable {
  public required string Home { get; init; }
  public required string Archive { get; init; }
  public required string Tags { get; init; }
  public required string Rss { get; init; }
  public required string About { get; init; }
}

[MiniJinjaContext]
internal sealed partial class FooterViewModel : ITemplateSerializable {
  public required int YearStart { get; init; }
  public required int YearCurrent { get; init; }
  public required string PoweredBy { get; init; }
}

[MiniJinjaContext]
internal sealed partial class PluginsViewModel : ITemplateSerializable {
  public required MathJaxViewModel MathJax { get; init; }
  public required LucideViewModel Lucide { get; init; }
  public required MediumZoomViewModel MediumZoom { get; init; }
  public required PrismJsViewModel PrismJs { get; init; }
  public required HighlightJsViewModel HighlightJs { get; init; }
  public required ShikiViewModel Shiki { get; init; }
  public required ArboriumViewModel Arborium { get; init; }
  public required DayJsViewModel DayJs { get; init; }
}

[MiniJinjaContext]
internal sealed partial class MathJaxViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string Cdn { get; init; }
}

[MiniJinjaContext]
internal sealed partial class LucideViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string Cdn { get; init; }
}

[MiniJinjaContext]
internal sealed partial class MediumZoomViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string Cdn { get; init; }
}

[MiniJinjaContext]
internal sealed partial class PrismJsViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string CssCdn { get; init; }
  public required string JsCdn { get; init; }
  public required bool AutoloaderEnabled { get; init; }
  public required string AutoloaderCdn { get; init; }
}

[MiniJinjaContext]
internal sealed partial class HighlightJsViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string CssCdn { get; init; }
  public required string JsCdn { get; init; }
  public required string LanguageCdnBase { get; init; }
  public required List<string> Languages { get; init; }
}

[MiniJinjaContext]
internal sealed partial class ShikiViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string StyleCdn { get; init; }
}

[MiniJinjaContext]
internal sealed partial class ArboriumViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string CssCdn { get; init; }
  public required string JsCdn { get; init; }
}

[MiniJinjaContext]
internal sealed partial class DayJsViewModel : ITemplateSerializable {
  public required bool Enabled { get; init; }
  public required string Cdn { get; init; }
  public required string Locale { get; init; }
  public required string LocaleCdn { get; init; }
  public required string RelativeTimeCdn { get; init; }
}

[MiniJinjaContext]
internal sealed partial class DateOptionsViewModel : ITemplateSerializable {
  public required string Format { get; init; }
  public required int RelativeDays { get; init; }
  public required string Locale { get; init; }
}

[MiniJinjaContext]
internal sealed partial class SidebarViewModel : ITemplateSerializable {
  public required bool Show { get; init; }
}

[MiniJinjaContext]
internal sealed partial class PagingViewModel : ITemplateSerializable {
  public required int PerPage { get; init; }
}

[MiniJinjaContext]
internal sealed partial class I18nViewModel : ITemplateSerializable {
  public required string Home { get; init; }
  public required string Archive { get; init; }
  public required string Tags { get; init; }
  public required string Categories { get; init; }
  public required string Rss { get; init; }
  public required string About { get; init; }
  public required string TagPrefix { get; init; }
  public required string CategoryPrefix { get; init; }
  public required string Previous { get; init; }
  public required string Next { get; init; }
  public required string PagePrefix { get; init; }
}

/// <summary>
/// View model for a content item (post or page) used in templates.
/// </summary>
[MiniJinjaContext]
internal sealed partial class ContentPageViewModel : ITemplateSerializable {
  public required string Title { get; init; }
  public required string Description { get; init; }
  public required string Slug { get; init; }
  public required string Layout { get; init; }
  public required string Date { get; init; }
  public required string DateIso { get; init; }
  public string DateRelative { get; init; } = "";
  public required string Url { get; init; }
  public required List<string> Tags { get; init; }
  public required List<string> Categories { get; init; }
  public required string Content { get; init; }
  public string Excerpt { get; init; } = "";
  public required string Hash { get; init; }
}

/// <summary>
/// Model for rendering a single post page.
/// </summary>
[MiniJinjaContext]
internal sealed partial class PostRenderModel : ITemplateSerializable {
  public required SiteViewModel Site { get; init; }
  public required ContentPageViewModel Page { get; init; }
  public required ContentPageViewModel Post { get; init; }
  public required string Content { get; init; }
}

/// <summary>
/// Model for rendering a single page.
/// </summary>
[MiniJinjaContext]
internal sealed partial class PageRenderModel : ITemplateSerializable {
  public required SiteViewModel Site { get; init; }
  public required ContentPageViewModel Page { get; init; }
  public required string Content { get; init; }
}

/// <summary>
/// Model for rendering index pages with posts and pagination.
/// </summary>
[MiniJinjaContext]
internal sealed partial class IndexPageModel : ITemplateSerializable {
  public required SiteViewModel Site { get; init; }
  public required IReadOnlyList<ContentPageViewModel> Posts { get; init; }
  public required PaginationViewModel Pagination { get; init; }
}

/// <summary>
/// Model for rendering tag index page.
/// </summary>
[MiniJinjaContext]
internal sealed partial class TagsIndexModel : ITemplateSerializable {
  public required SiteViewModel Site { get; init; }
  public required IReadOnlyList<TagViewModel> Tags { get; init; }
}

/// <summary>
/// Model for rendering a single tag's posts page.
/// </summary>
[MiniJinjaContext]
internal sealed partial class TagPageModel : ITemplateSerializable {
  public required SiteViewModel Site { get; init; }
  public required string Tag { get; init; }
  public required IReadOnlyList<ContentPageViewModel> Posts { get; init; }
  public required PaginationViewModel Pagination { get; init; }
}

/// <summary>
/// Model for rendering categories index page.
/// </summary>
[MiniJinjaContext]
internal sealed partial class CategoriesIndexModel : ITemplateSerializable {
  public required SiteViewModel Site { get; init; }
  public required IReadOnlyList<CategoryViewModel> Categories { get; init; }
}

/// <summary>
/// Model for rendering a single category's posts page.
/// </summary>
[MiniJinjaContext]
internal sealed partial class CategoryPageModel : ITemplateSerializable {
  public required SiteViewModel Site { get; init; }
  public required string Category { get; init; }
  public required IReadOnlyList<ContentPageViewModel> Posts { get; init; }
  public required PaginationViewModel Pagination { get; init; }
}

/// <summary>
/// View model for tag information.
/// </summary>
[MiniJinjaContext]
internal sealed partial class TagViewModel : ITemplateSerializable {
  public required string Name { get; init; }
  public required int Count { get; init; }
  public required string Url { get; init; }
}

/// <summary>
/// View model for category information.
/// </summary>
[MiniJinjaContext]
internal sealed partial class CategoryViewModel : ITemplateSerializable {
  public required string Name { get; init; }
  public required int Count { get; init; }
  public required string Url { get; init; }
}

/// <summary>
/// View model for pagination.
/// </summary>
[MiniJinjaContext]
internal sealed partial class PaginationViewModel : ITemplateSerializable {
  public required int Current { get; init; }
  public required int Total { get; init; }
  public string PrevUrl { get; init; } = "";
  public string NextUrl { get; init; } = "";
  public required IReadOnlyList<PaginationItemViewModel> Items { get; init; }
}

/// <summary>
/// View model for a pagination item.
/// </summary>
[MiniJinjaContext]
internal sealed partial class PaginationItemViewModel : ITemplateSerializable {
  public required string Label { get; init; }
  public string Url { get; init; } = "";
  public required bool IsCurrent { get; init; }
  public required bool IsEllipsis { get; init; }
}

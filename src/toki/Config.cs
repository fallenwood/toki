namespace Toki;

using Tomlyn.Model;
using ZLinq;

internal static class Config {
  internal static SiteConfig LoadSiteConfig(string configPath) {
    if (!File.Exists(configPath)) {
      return SiteConfig.Default;
    }

    try {
      var toml = File.ReadAllText(configPath);
      var model = Tomlyn.Toml.ToModel(toml) as TomlTable;
      if (model is null) {
        return SiteConfig.Default;
      }

      return new SiteConfig(
        Title: GetTomlString(model, "title") ?? SiteConfig.Default.Title,
        Description: GetTomlString(model, "description") ?? SiteConfig.Default.Description,
        Author: GetTomlString(model, "author") ?? SiteConfig.Default.Author,
        AvatarLink: GetTomlString(model, "avatarLink") ?? SiteConfig.Default.AvatarLink,
        BaseUrl: GetTomlString(model, "baseUrl") ?? SiteConfig.Default.BaseUrl,
        Theme: GetTomlString(model, "theme") ?? SiteConfig.Default.Theme,
        Gitalk: GetGitalkConfig(model),
        Date: GetDateOptions(model),
        Sidebar: GetSidebarOptions(model),
        Paging: GetPagingOptions(model),
        Footer: GetFooterOptions(model),
        Plugins: GetPluginOptions(model),
        Deploy: GetDeployOptions(model),
        I18n: GetI18nOptions(model)
      );
    } catch {
      return SiteConfig.Default;
    }
  }

  internal static SiteViewModel BuildSiteModel(SiteConfig siteConfig) {
    return new SiteViewModel {
      Title = siteConfig.Title,
      Description = siteConfig.Description,
      Author = siteConfig.Author,
      AvatarLink = siteConfig.AvatarLink,
      BaseUrl = siteConfig.BaseUrl,
      Theme = siteConfig.Theme,
      Gitalk = new GitalkViewModel {
        Enabled = siteConfig.Gitalk.Enabled,
        ClientId = siteConfig.Gitalk.ClientId,
        ClientSecret = siteConfig.Gitalk.ClientSecret,
        Repo = siteConfig.Gitalk.Repo,
        Owner = siteConfig.Gitalk.Owner,
        Admin = siteConfig.Gitalk.Admin
      },
      Nav = new NavViewModel {
        Home = siteConfig.I18n.Home,
        Archive = siteConfig.I18n.Archive,
        Tags = siteConfig.I18n.Tags,
        Rss = siteConfig.I18n.Rss,
        About = siteConfig.I18n.About
      },
      Footer = new FooterViewModel {
        YearStart = siteConfig.Footer.YearStart,
        YearCurrent = DateTimeOffset.Now.Year,
        PoweredBy = siteConfig.Footer.PoweredBy
      },
      Plugins = new PluginsViewModel {
        MathJax = new MathJaxViewModel { Enabled = siteConfig.Plugins.MathJax.Enabled, Cdn = siteConfig.Plugins.MathJax.Cdn },
        Lucide = new LucideViewModel { Enabled = siteConfig.Plugins.Lucide.Enabled, Cdn = siteConfig.Plugins.Lucide.Cdn },
        MediumZoom = new MediumZoomViewModel { Enabled = siteConfig.Plugins.MediumZoom.Enabled, Cdn = siteConfig.Plugins.MediumZoom.Cdn },
        PrismJs = new PrismJsViewModel {
          Enabled = siteConfig.Plugins.PrismJs.Enabled,
          CssCdn = siteConfig.Plugins.PrismJs.CssCdn,
          JsCdn = siteConfig.Plugins.PrismJs.JsCdn,
          AutoloaderEnabled = siteConfig.Plugins.PrismJs.AutoloaderEnabled,
          AutoloaderCdn = siteConfig.Plugins.PrismJs.AutoloaderCdn
        },
        HighlightJs = new HighlightJsViewModel {
          Enabled = siteConfig.Plugins.HighlightJs.Enabled,
          CssCdn = siteConfig.Plugins.HighlightJs.CssCdn,
          JsCdn = siteConfig.Plugins.HighlightJs.JsCdn,
          LanguageCdnBase = siteConfig.Plugins.HighlightJs.LanguageCdnBase,
          Languages = siteConfig.Plugins.HighlightJs.Languages
        },
        Shiki = new ShikiViewModel {
          Enabled = siteConfig.Plugins.Shiki.Enabled,
          StyleCdn = siteConfig.Plugins.Shiki.StyleCdn
        },
        Arborium = new ArboriumViewModel {
          Enabled = siteConfig.Plugins.Arborium.Enabled,
          CssCdn = siteConfig.Plugins.Arborium.CssCdn,
          JsCdn = siteConfig.Plugins.Arborium.JsCdn
        },
        DayJs = new DayJsViewModel {
          Enabled = siteConfig.Plugins.DayJs.Enabled,
          Cdn = siteConfig.Plugins.DayJs.Cdn,
          Locale = siteConfig.Plugins.DayJs.Locale,
          LocaleCdn = siteConfig.Plugins.DayJs.LocaleCdn,
          RelativeTimeCdn = siteConfig.Plugins.DayJs.RelativeTimeCdn
        }
      },
      Date = new DateOptionsViewModel {
        Format = siteConfig.Date.Format,
        RelativeDays = siteConfig.Date.RelativeDays,
        Locale = siteConfig.Date.Locale
      },
      Sidebar = new SidebarViewModel {
        Show = siteConfig.Sidebar.Show
      },
      Paging = new PagingViewModel {
        PerPage = siteConfig.Paging.PerPage
      },
      I18n = new I18nViewModel {
        Home = siteConfig.I18n.Home,
        Archive = siteConfig.I18n.Archive,
        Tags = siteConfig.I18n.Tags,
        Categories = siteConfig.I18n.Categories,
        Rss = siteConfig.I18n.Rss,
        About = siteConfig.I18n.About,
        TagPrefix = siteConfig.I18n.TagPrefix,
        CategoryPrefix = siteConfig.I18n.CategoryPrefix,
        Previous = siteConfig.I18n.Previous,
        Next = siteConfig.I18n.Next,
        PagePrefix = siteConfig.I18n.PagePrefix
      }
    };
  }

  private static string? GetTomlString(TomlTable table, string key) {
    if (table.TryGetValue(key, out var value) && value is not null) {
      return value.ToString();
    }

    return null;
  }

  private static TomlTable? GetTomlTable(TomlTable table, string key) {
    if (table.TryGetValue(key, out var value) && value is TomlTable nested) {
      return nested;
    }

    return null;
  }

  private static bool? GetTomlBool(TomlTable table, string key) {
    if (table.TryGetValue(key, out var value) && value is not null) {
      if (value is bool boolValue) {
        return boolValue;
      }

      if (bool.TryParse(value.ToString(), out var parsed)) {
        return parsed;
      }
    }

    return null;
  }

  private static int? GetTomlInt(TomlTable table, string key) {
    if (table.TryGetValue(key, out var value) && value is not null) {
      if (value is int intValue) {
        return intValue;
      }

      if (int.TryParse(value.ToString(), out var parsed)) {
        return parsed;
      }
    }

    return null;
  }

  private static List<string> GetTomlStringList(TomlTable table, string key) {
    if (!table.TryGetValue(key, out var value) || value is null) {
      return [];
    }

    if (value is TomlArray array) {
      return array
        .AsValueEnumerable()
        .Select(item => item?.ToString())
        .Where(item => !string.IsNullOrWhiteSpace(item))
        .Select(item => item!)
        .ToList();
    }

    return value.ToString() is { } single && !string.IsNullOrWhiteSpace(single)
      ? [single]
      : [];
  }

  private static GitalkConfig GetGitalkConfig(TomlTable model) {
    var gitalkTable = GetTomlTable(model, "gitalk");
    if (gitalkTable is null) {
      return GitalkConfig.Disabled;
    }

    return new GitalkConfig(
      Enabled: GetTomlBool(gitalkTable, "enabled") ?? false,
      ClientId: GetTomlString(gitalkTable, "clientId") ?? string.Empty,
      ClientSecret: GetTomlString(gitalkTable, "clientSecret") ?? string.Empty,
      Repo: GetTomlString(gitalkTable, "repo") ?? string.Empty,
      Owner: GetTomlString(gitalkTable, "owner") ?? string.Empty,
      Admin: GetTomlStringList(gitalkTable, "admin")
    );
  }

  private static DateOptions GetDateOptions(TomlTable model) {
    var dateTable = GetTomlTable(model, "date");
    if (dateTable is null) {
      return DateOptions.Default;
    }

    return new DateOptions(
      Format: GetTomlString(dateTable, "format") ?? DateOptions.Default.Format,
      RelativeDays: GetTomlInt(dateTable, "relativeDays") ?? DateOptions.Default.RelativeDays,
      Locale: GetTomlString(dateTable, "locale") ?? DateOptions.Default.Locale
    );
  }

  private static SidebarOptions GetSidebarOptions(TomlTable model) {
    var sidebarTable = GetTomlTable(model, "sidebar");
    if (sidebarTable is null) {
      return SidebarOptions.Default;
    }

    return new SidebarOptions(
      Show: GetTomlBool(sidebarTable, "show") ?? SidebarOptions.Default.Show
    );
  }

  private static PagingOptions GetPagingOptions(TomlTable model) {
    var pagingTable = GetTomlTable(model, "paging");
    if (pagingTable is null) {
      return PagingOptions.Default;
    }

    return new PagingOptions(
      PerPage: Math.Max(1, GetTomlInt(pagingTable, "perPage") ?? PagingOptions.Default.PerPage)
    );
  }
  private static FooterOptions GetFooterOptions(TomlTable model) {
    var footerTable = GetTomlTable(model, "footer");
    if (footerTable is null) {
      return FooterOptions.Default;
    }

    return new FooterOptions(
      YearStart: GetTomlInt(footerTable, "yearStart") ?? FooterOptions.Default.YearStart,
      PoweredBy: GetTomlString(footerTable, "poweredBy") ?? FooterOptions.Default.PoweredBy
    );
  }

  private static PluginOptions GetPluginOptions(TomlTable model) {
    var pluginTable = GetTomlTable(model, "plugins");
    if (pluginTable is null) {
      return PluginOptions.Default;
    }

    return new PluginOptions(
      MathJax: GetPluginSetting(pluginTable, "mathjax", PluginOptions.Default.MathJax),
      Lucide: GetPluginSetting(pluginTable, "lucide", PluginOptions.Default.Lucide),
      MediumZoom: GetPluginSetting(pluginTable, "mediumzoom", PluginOptions.Default.MediumZoom),
      PrismJs: GetPrismJsConfig(pluginTable, "prismjs", PluginOptions.Default.PrismJs),
      HighlightJs: GetHighlightJsConfig(pluginTable, "highlightjs", PluginOptions.Default.HighlightJs),
      Shiki: GetShikiConfig(pluginTable, "shiki", PluginOptions.Default.Shiki),
      Arborium: GetArboriumConfig(pluginTable, "arborium", PluginOptions.Default.Arborium),
      DayJs: GetDayJsConfig(pluginTable, "dayjs", PluginOptions.Default.DayJs)
    );
  }

  private static PluginSetting GetPluginSetting(TomlTable table, string key, PluginSetting fallback) {
    var settingTable = GetTomlTable(table, key);
    if (settingTable is null) {
      return fallback;
    }

    return new PluginSetting(
      Enabled: GetTomlBool(settingTable, "enabled") ?? fallback.Enabled,
      Cdn: GetTomlString(settingTable, "cdn") ?? fallback.Cdn
    );
  }

  private static PrismJsConfig GetPrismJsConfig(TomlTable table, string key, PrismJsConfig fallback) {
    var settingTable = GetTomlTable(table, key);
    if (settingTable is null) {
      return fallback;
    }

    return new PrismJsConfig(
      Enabled: GetTomlBool(settingTable, "enabled") ?? fallback.Enabled,
      CssCdn: GetTomlString(settingTable, "cssCdn") ?? fallback.CssCdn,
      JsCdn: GetTomlString(settingTable, "jsCdn") ?? fallback.JsCdn,
      AutoloaderEnabled: GetTomlBool(settingTable, "autoloaderEnabled") ?? fallback.AutoloaderEnabled,
      AutoloaderCdn: GetTomlString(settingTable, "autoloaderCdn") ?? fallback.AutoloaderCdn
    );
  }

  private static HighlightJsConfig GetHighlightJsConfig(TomlTable table, string key, HighlightJsConfig fallback) {
    var settingTable = GetTomlTable(table, key);
    if (settingTable is null) {
      return fallback;
    }

    return new HighlightJsConfig(
      Enabled: GetTomlBool(settingTable, "enabled") ?? fallback.Enabled,
      CssCdn: GetTomlString(settingTable, "cssCdn") ?? fallback.CssCdn,
      JsCdn: GetTomlString(settingTable, "jsCdn") ?? fallback.JsCdn,
      LanguageCdnBase: GetTomlString(settingTable, "languageCdnBase") ?? fallback.LanguageCdnBase,
      Languages: GetTomlStringList(settingTable, "languages")
    );
  }

  private static ShikiConfig GetShikiConfig(TomlTable table, string key, ShikiConfig fallback) {
    var settingTable = GetTomlTable(table, key);
    if (settingTable is null) {
      return fallback;
    }

    return new ShikiConfig(
      Enabled: GetTomlBool(settingTable, "enabled") ?? fallback.Enabled,
      StyleCdn: GetTomlString(settingTable, "styleCdn") ?? fallback.StyleCdn
    );
  }

  private static ArboriumConfig GetArboriumConfig(TomlTable table, string key, ArboriumConfig fallback) {
    var settingTable = GetTomlTable(table, key);
    if (settingTable is null) {
      return fallback;
    }

    return new ArboriumConfig(
      Enabled: GetTomlBool(settingTable, "enabled") ?? fallback.Enabled,
      CssCdn: GetTomlString(settingTable, "cssCdn") ?? fallback.CssCdn,
      JsCdn: GetTomlString(settingTable, "jsCdn") ?? fallback.JsCdn
    );
  }

  private static DayJsConfig GetDayJsConfig(TomlTable table, string key, DayJsConfig fallback) {
    var settingTable = GetTomlTable(table, key);
    if (settingTable is null) {
      return fallback;
    }

    return new DayJsConfig(
      Enabled: GetTomlBool(settingTable, "enabled") ?? fallback.Enabled,
      Cdn: GetTomlString(settingTable, "cdn") ?? fallback.Cdn,
      Locale: GetTomlString(settingTable, "locale") ?? fallback.Locale,
      LocaleCdn: GetTomlString(settingTable, "localeCdn") ?? fallback.LocaleCdn,
      RelativeTimeCdn: GetTomlString(settingTable, "relativeTimeCdn") ?? fallback.RelativeTimeCdn
    );
  }

  private static DeployOptions GetDeployOptions(TomlTable model) {
    var deployTable = GetTomlTable(model, "deploy");
    if (deployTable is null) {
      return DeployOptions.Default;
    }

    return new DeployOptions(
      Remote: GetTomlString(deployTable, "remote") ?? DeployOptions.Default.Remote,
      Branch: GetTomlString(deployTable, "branch") ?? DeployOptions.Default.Branch,
      Repo: GetTomlString(deployTable, "repo")
    );
  }

  private static I18nOptions GetI18nOptions(TomlTable model) {
    var i18nTable = GetTomlTable(model, "i18n");
    if (i18nTable is null) {
      return I18nOptions.Default;
    }

    return new I18nOptions(
      Home: GetTomlString(i18nTable, "home") ?? I18nOptions.Default.Home,
      Archive: GetTomlString(i18nTable, "archive") ?? I18nOptions.Default.Archive,
      Tags: GetTomlString(i18nTable, "tags") ?? I18nOptions.Default.Tags,
      Categories: GetTomlString(i18nTable, "categories") ?? I18nOptions.Default.Categories,
      Rss: GetTomlString(i18nTable, "rss") ?? I18nOptions.Default.Rss,
      About: GetTomlString(i18nTable, "about") ?? I18nOptions.Default.About,
      TagPrefix: GetTomlString(i18nTable, "tagPrefix") ?? I18nOptions.Default.TagPrefix,
      CategoryPrefix: GetTomlString(i18nTable, "categoryPrefix") ?? I18nOptions.Default.CategoryPrefix,
      Previous: GetTomlString(i18nTable, "previous") ?? I18nOptions.Default.Previous,
      Next: GetTomlString(i18nTable, "next") ?? I18nOptions.Default.Next,
      PagePrefix: GetTomlString(i18nTable, "pagePrefix") ?? I18nOptions.Default.PagePrefix
    );
  }
}

internal record SiteConfig(string Title, string Description, string Author, string AvatarLink, string BaseUrl, string Theme, GitalkConfig Gitalk, DateOptions Date, SidebarOptions Sidebar, PagingOptions Paging, FooterOptions Footer, PluginOptions Plugins, DeployOptions Deploy, I18nOptions I18n) {
  public static SiteConfig Default => new("Toki Site", "", "", "", "/", "themes/default", GitalkConfig.Disabled, DateOptions.Default, SidebarOptions.Default, PagingOptions.Default, FooterOptions.Default, PluginOptions.Default, DeployOptions.Default, I18nOptions.Default);
}

internal record GitalkConfig(bool Enabled, string ClientId, string ClientSecret, string Repo, string Owner, List<string> Admin) {
  public static GitalkConfig Disabled => new(false, string.Empty, string.Empty, string.Empty, string.Empty, new List<string>());
}

internal record DateOptions(string Format, int RelativeDays, string Locale) {
  public static DateOptions Default => new("yyyy-MM-dd HH:mm", 7, "zh-CN");
}

internal record SidebarOptions(bool Show) {
  public static SidebarOptions Default => new(true);
}

internal record PagingOptions(int PerPage) {
  public static PagingOptions Default => new(10);
}

internal record FooterOptions(int YearStart, string PoweredBy) {
  public static FooterOptions Default => new(2014, "Toki");
}

internal record PluginOptions(
  PluginSetting MathJax,
  PluginSetting Lucide,
  PluginSetting MediumZoom,
  PrismJsConfig PrismJs,
  HighlightJsConfig HighlightJs,
  ShikiConfig Shiki,
  ArboriumConfig Arborium,
  DayJsConfig DayJs
) {
  public static PluginOptions Default => new(
    new PluginSetting(false, "https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-svg.js"),
    new PluginSetting(false, "https://unpkg.com/lucide@latest/dist/umd/lucide.min.js"),
    new PluginSetting(false, "https://cdn.jsdelivr.net/npm/medium-zoom@1.1.0/dist/medium-zoom.min.js"),
    new PrismJsConfig(
      true,
      "https://cdn.jsdelivr.net/npm/prismjs@1.29.0/themes/prism.min.css",
      "https://cdn.jsdelivr.net/npm/prismjs@1.29.0/prism.min.js",
      true,
      "https://cdn.jsdelivr.net/npm/prismjs@1.29.0/plugins/autoloader/prism-autoloader.min.js"
    ),
    new HighlightJsConfig(
      true,
      "https://cdn.jsdelivr.net/npm/highlight.js@11.9.0/styles/github.min.css",
      "https://cdn.jsdelivr.net/npm/highlight.js@11.9.0/lib/highlight.min.js",
      "https://cdn.jsdelivr.net/npm/highlight.js@11.9.0/lib/languages",
      []
    ),
    new ShikiConfig(
      true,
      "https://cdn.jsdelivr.net/npm/shiki@1.1.1/style.css"
    ),
    new ArboriumConfig(
      false,
      "https://cdn.jsdelivr.net/npm/@arborium/arborium@2/dist/themes/github-dark.css",
      "https://cdn.jsdelivr.net/npm/@arborium/arborium@2/dist/arborium.iife.js"
    ),
    new DayJsConfig(
      false,
      "https://cdn.jsdelivr.net/npm/dayjs@1.11.13/dayjs.min.js",
      "zh-CN",
      "https://cdn.jsdelivr.net/npm/dayjs@1.11.13/locale/zh-cn.js",
      "https://cdn.jsdelivr.net/npm/dayjs@1.11.13/plugin/relativeTime.js"
    )
  );
}

internal record PluginSetting(bool Enabled, string Cdn);

internal record PrismJsConfig(bool Enabled, string CssCdn, string JsCdn, bool AutoloaderEnabled, string AutoloaderCdn);

internal record HighlightJsConfig(bool Enabled, string CssCdn, string JsCdn, string LanguageCdnBase, List<string> Languages);

internal record ShikiConfig(bool Enabled, string StyleCdn);

internal record ArboriumConfig(bool Enabled, string CssCdn, string JsCdn);

internal record DayJsConfig(bool Enabled, string Cdn, string Locale, string LocaleCdn, string RelativeTimeCdn);

internal record DeployOptions(string Remote, string Branch, string? Repo) {
  public static DeployOptions Default => new("origin", "gh-pages", null);
}

internal record I18nOptions(string Home, string Archive, string Tags, string Categories, string Rss, string About, string TagPrefix, string CategoryPrefix, string Previous, string Next, string PagePrefix) {
  public static I18nOptions Default => new("Home", "Archive", "Tags", "Categories", "RSS", "About", "Tag", "Category", "Previous", "Next", "Page");
}

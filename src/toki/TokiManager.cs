namespace Toki;

using Markdig;
using Microsoft.Extensions.Logging;

internal class TokiManager {
  private readonly string root;
  private readonly string publicDir;
  private readonly string sourceDir;
  private readonly string postsDir;
  private readonly string themesDir;
  private readonly string configPath;
  private readonly ILogger logger;
  private readonly ILoggerFactory loggerFactory;

  public TokiManager(string root, ILogger logger, ILoggerFactory loggerFactory) {
    this.root = root;
    this.logger = logger;
    this.loggerFactory = loggerFactory;
    sourceDir = Path.Combine(root, "source");
    postsDir = Path.Combine(sourceDir, "_posts");
    publicDir = Path.Combine(root, "public");
    themesDir = Path.Combine(root, "themes");
    configPath = Path.Combine(root, "site.toml");
  }

  public void Build() {
    if (!Directory.Exists(sourceDir)) {
      logger.LogError("Missing source directory: {SourceDir}", sourceDir);
      return;
    }

    var siteConfig = Config.LoadSiteConfig(configPath, logger);
    var siteModel = Config.BuildSiteModel(siteConfig);
    var markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    var themePath = ResolveThemePath(root, siteConfig.Theme);
    var templatesDir = Path.Combine(themePath, "templates");
    var themeDistDir = Path.Combine(themePath, "dist");

    if (Directory.Exists(publicDir)) {
      // Be careful not to delete if we are locking files,
      // but usually safe for static site generation unless serve locks them.
      // When serving, we might need to just overwrite or clean specific files.
      // For now, let's try to keep the naive approach, or maybe just clean contents.
      try {
        // Directory.Delete(_publicDir, recursive: true);
        // Deleting the directory while serving might crash the server.
        // Better to empty it or just overwrite.
        // Overwrite is safer for hot reload.
      } catch (Exception ex) {
        logger.LogWarning("Could not clean public dir: {Message}", ex.Message);
      }
    }
    Directory.CreateDirectory(publicDir);

    using var env = new MiniJinja.Environment();
    TemplateEngine.LoadTemplates(env, templatesDir);

    var posts = ContentLoader.LoadPosts(postsDir, markdownPipeline, siteConfig.Date, logger);
    var pages = ContentLoader.LoadPages(sourceDir, postsDir, markdownPipeline, siteConfig.Date, logger);

    foreach (var post in posts) {
      var outputPath = Path.Combine(publicDir, post.OutputPath);
      var page = post.ToPageModel();
      TemplateEngine.RenderToFile(env, ResolveLayout(post.Layout, "post.html"), outputPath, new PostRenderModel {
        Site = siteModel,
        Page = page,
        Post = page,
        Content = post.Html,
      });
    }

    foreach (var page in pages) {
      var outputPath = Path.Combine(publicDir, page.OutputPath);
      TemplateEngine.RenderToFile(env, ResolveLayout(page.Layout, "page.html"), outputPath, new PageRenderModel {
        Site = siteModel,
        Page = page.ToPageModel(),
        Content = page.Html
      });
    }

    SiteGenerator.CopyStaticAssets(sourceDir, postsDir, publicDir);
    SiteGenerator.CopyThemeAssets(themeDistDir, publicDir);

    SiteGenerator.GenerateIndexPages(env, posts, siteModel, publicDir, siteConfig.Paging);
    SiteGenerator.GenerateTagPages(env, posts, siteModel, publicDir, siteConfig.Paging);
    SiteGenerator.GenerateCategoryPages(env, posts, siteModel, publicDir, siteConfig.Paging);
    SiteGenerator.GenerateAtomFeed(posts, siteConfig, publicDir);
    if (siteConfig.Plugins.Search.Enabled) {
      // Build a lightweight search index consumed by the frontend (MiniSearch/Fuse/etc.)
      SearchIndexGenerator.Generate(publicDir, posts, pages, siteConfig.Plugins.Search.IndexPath);
      // Render dedicated search page (if enabled)
      SiteGenerator.GenerateSearchPage(env, siteModel, publicDir, siteConfig.Plugins.Search);
    }

    logger.LogInformation("Generated {PostCount} posts and {PageCount} pages in {PublicDir}", posts.Count, pages.Count, publicDir);
  }

  public async Task Watch(int port, CancellationToken cancellationToken) {
    this.Build();

    var reloadToken = new CancellationTokenSource();

    // Start Preview Server in a separate thread/task
    var serverTask = Task.Run(() => PreviewServer.RunPreviewServer(publicDir, port, loggerFactory, reloadToken.Token), cancellationToken);

    logger.LogInformation("Watching for changes...");

    using var watcherSource = new FileSystemWatcher(sourceDir) { IncludeSubdirectories = true, EnableRaisingEvents = true };
    using var watcherTemplates = new FileSystemWatcher(themesDir) { IncludeSubdirectories = true, EnableRaisingEvents = true };
    using var watcherConfig = new FileSystemWatcher(root, "site.toml") { EnableRaisingEvents = true };

    var debouncer = new Debouncer(TimeSpan.FromMilliseconds(500));

    FileSystemEventHandler handler = (s, e) => {
      debouncer.Debounce(() => {
        logger.LogInformation("File changed: {Name}. Rebuilding...", e.Name);
        try {
          Build();
          PreviewServer.TriggerReload();
        } catch (Exception ex) {
          logger.LogError(ex, "Build failed");
        }
      });
    };

    watcherSource.Changed += handler;
    watcherSource.Created += handler;
    watcherSource.Deleted += handler;
    watcherSource.Renamed += (s, e) => handler(s, e);

    watcherTemplates.Changed += handler;
    watcherTemplates.Created += handler;
    watcherTemplates.Deleted += handler;
    watcherTemplates.Renamed += (s, e) => handler(s, e);

    watcherConfig.Changed += handler;

    await Task.Delay(-1, cancellationToken);
  }

  private static string ResolveLayout(string? layout, string fallback) {
    if (string.IsNullOrWhiteSpace(layout)) return fallback;
    if (layout.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) return layout;
    return $"{layout}.html";
  }

  private static string ResolveThemePath(string root, string? theme) {
    if (string.IsNullOrWhiteSpace(theme)) {
      return Path.Combine(root, "themes", "default");
    }

    return Path.IsPathRooted(theme)
      ? theme
      : Path.Combine(root, theme);
  }
}

internal class Debouncer {
  private readonly TimeSpan _delay;
  private CancellationTokenSource? _usageToken;
  private readonly object _lock = new();

  public Debouncer(TimeSpan delay) {
    _delay = delay;
  }

  public void Debounce(Action action) {
    lock (_lock) {
      _usageToken?.Cancel();
      _usageToken = new CancellationTokenSource();
      var token = _usageToken.Token;
      Task.Delay(_delay, token).ContinueWith(t => {
        if (!t.IsCanceled) {
          action();
        }
      });
    }
  }
}

namespace toki.unittests {
  using System.Text.Json;
  using FluentAssertions;
  using Microsoft.Extensions.Logging.Abstractions;
  using Toki;
  using Xunit;

  public class SearchIndexGeneratorTests {
    [Fact]
    public void Generate_search_index_includes_posts_and_pages() {
    using var temp = new TempTestSite();
    temp.WriteConfig("""
      title = "Test Site"
      baseUrl = "/"
      [plugins.search]
      enabled = true
      provider = "minisearch"
      indexPath = "search/custom-index.json"
      minChars = 1
      limit = 5
      fuzzy = 0.3
      """);

    temp.WriteMinimalTemplates();

    var now = DateTimeOffset.Parse("2024-01-15T12:00:00+00:00");
    temp.WritePost("first-post.md", """
---
title: First Post
date: 2024-01-15 12:00
tags: [c#, dotnet]
categories: [dev]
---
Hello **world**!
<!-- more -->
Rest of content to index. More keywords here.
""");

    temp.WritePage("about/index.md", """
---
title: About Page
date: 2023-12-31 08:00
---
This is the about page.
""");

    var loggerFactory = NullLoggerFactory.Instance;
    var manager = new TokiManager(temp.Root, NullLogger<TokiManager>.Instance, loggerFactory);
    manager.Build();

    var searchPath = Path.Combine(temp.PublicDir, "search", "custom-index.json");
    File.Exists(searchPath).Should().BeTrue("search index should be generated during build");

    var json = File.ReadAllText(searchPath);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;
    root.ValueKind.Should().Be(JsonValueKind.Array);
    root.GetArrayLength().Should().Be(2);
    // Verify camelCase serialization (source-gen context)
    var first = root[0];
    first.TryGetProperty("title", out _).Should().BeTrue();
    first.TryGetProperty("url", out _).Should().BeTrue();
    first.TryGetProperty("description", out _).Should().BeTrue();

    var entries = JsonSerializer.Deserialize<List<TestSearchEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    entries.Should().NotBeNull();
    entries!.Should().HaveCount(2);

    var post = entries.Single(e => e.Title == "First Post");
    post.Url.Should().NotBeNullOrWhiteSpace();
    post.Tags.Should().Contain(new[] { "c#", "dotnet" });
    post.Categories.Should().ContainSingle("dev");
    post.Description.Should().Contain("Hello world");
    post.Content.Should().Contain("Hello world");
    post.Hash.Should().NotBeNullOrEmpty();

    var page = entries.Single(e => e.Title == "About Page");
    page.Url.Should().Contain("about", "url should include slug");
    page.Content.Should().Contain("about page");
  }

  [Fact]
  public void Config_parses_search_plugin_settings() {
    const string toml = """
      [plugins.search]
      enabled = true
      provider = "fuse"
      indexPath = "custom-index.json"
      minChars = 3
      limit = 20
      fuzzy = 0.4
      """;

    var configFile = Path.GetTempFileName();
    File.WriteAllText(configFile, toml);

    try {
      var siteConfig = Config.LoadSiteConfig(configFile);
      siteConfig.Plugins.Search.Enabled.Should().BeTrue();
      siteConfig.Plugins.Search.Provider.Should().Be("fuse");
      siteConfig.Plugins.Search.IndexPath.Should().Be("custom-index.json");
      siteConfig.Plugins.Search.MinChars.Should().Be(3);
      siteConfig.Plugins.Search.Limit.Should().Be(20);
      siteConfig.Plugins.Search.Fuzzy.Should().Be(0.4);
    } finally {
      File.Delete(configFile);
    }
  }

  private sealed record TestSearchEntry(
    string Title,
    string Url,
    string Description,
    DateTimeOffset Date,
    List<string> Tags,
    List<string> Categories,
    string Content,
    string Hash
  );

  private sealed class TempTestSite : IDisposable {
    public string Root { get; }
    public string SourceDir { get; }
    public string PostsDir { get; }
    public string PublicDir { get; }

    public TempTestSite() {
      Root = Path.Combine(Path.GetTempPath(), "toki-tests", Guid.NewGuid().ToString("N"));
      SourceDir = Path.Combine(Root, "source");
      PostsDir = Path.Combine(SourceDir, "_posts");
      PublicDir = Path.Combine(Root, "public");
      Directory.CreateDirectory(PostsDir);
    }

    public void WriteConfig(string toml) {
      Directory.CreateDirectory(Root);
      File.WriteAllText(Path.Combine(Root, "site.toml"), toml);
    }

    public void WritePost(string relativePath, string content) {
      var path = Path.Combine(PostsDir, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, content);
    }

    public void WritePage(string relativePath, string content) {
      var path = Path.Combine(SourceDir, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, content);
    }

    public void WriteMinimalTemplates() {
      var templatesDir = Path.Combine(Root, "themes", "default", "templates");
      Directory.CreateDirectory(templatesDir);
      File.WriteAllText(Path.Combine(templatesDir, "base.html"), "<!doctype html><html><body>{% block content %}{% endblock %}</body></html>");
      File.WriteAllText(Path.Combine(templatesDir, "post.html"), "{% extends \"base.html\" %}{% block content %}{{ content|safe }}{% endblock %}");
      File.WriteAllText(Path.Combine(templatesDir, "page.html"), "{% extends \"base.html\" %}{% block content %}{{ content|safe }}{% endblock %}");
      File.WriteAllText(Path.Combine(templatesDir, "index.html"), "{% extends \"base.html\" %}{% block content %}{% for post in posts %}<article><a href='{{ post.url }}'>{{ post.title }}</a></article>{% endfor %}{% endblock %}");
      File.WriteAllText(Path.Combine(templatesDir, "tags.html"), "{% extends \"base.html\" %}{% block content %}{% for tag in tags %}<a href='{{ tag.url }}'>{{ tag.name }}</a>{% endfor %}{% endblock %}");
      File.WriteAllText(Path.Combine(templatesDir, "tag.html"), "{% extends \"base.html\" %}{% block content %}{% for post in posts %}<a href='{{ post.url }}'>{{ post.title }}</a>{% endfor %}{% endblock %}");
      File.WriteAllText(Path.Combine(templatesDir, "categories.html"), "{% extends \"base.html\" %}{% block content %}{% for category in categories %}<a href='{{ category.url }}'>{{ category.name }}</a>{% endfor %}{% endblock %}");
      File.WriteAllText(Path.Combine(templatesDir, "category.html"), "{% extends \"base.html\" %}{% block content %}{% for post in posts %}<a href='{{ post.url }}'>{{ post.title }}</a>{% endfor %}{% endblock %}");
      File.WriteAllText(Path.Combine(templatesDir, "search.html"), "{% extends \"base.html\" %}{% block content %}<div id='search-page'></div>{% endblock %}");
    }

    public void Dispose() {
      try {
        if (Directory.Exists(Root)) {
          Directory.Delete(Root, recursive: true);
        }
      } catch {
        // ignore cleanup exceptions
      }
    }
  }

  }
}

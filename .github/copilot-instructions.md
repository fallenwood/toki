# Copilot Instructions for TokiSite

## Project Overview

Toki is a static site generator written in C# targeting .NET 10. It processes Markdown content with YAML front matter, renders HTML using MiniJinja templates, and outputs a static blog site.

## Build and Test Commands

```bash
# Build the entire solution
dotnet build toki.slnx

# Run all tests
dotnet test toki.slnx

# Run a single test by name
dotnet test toki.slnx --filter "FullyQualifiedName~YourTestName"

# Build and run the CLI
dotnet run --project src/toki -- build
dotnet run --project src/toki -- preview -p 5000

# Theme development (requires Bun)
cd themes/default
bun install
bun run build
```

## Architecture

### Content Pipeline

1. **Configuration** (`Config.cs`): Parses `site.toml` using Tomlyn to load site settings, plugins, i18n, and deploy options
2. **Content Loading** (`ContentLoader.cs`): Reads Markdown files from `source/_posts/` and `source/`, parses YAML front matter, converts to HTML using Markdig
3. **Template Rendering** (`TemplateEngine.cs`): Uses MiniJinja to render Jinja2-style templates from the theme
4. **Site Generation** (`SiteGenerator.cs`): Generates index, tag, category pages and Atom feed; handles pagination
5. **Output**: Static files written to `public/`

### Directory Structure

- `source/_posts/`: Blog post Markdown files (date-based URL: `/yyyy/MM/dd/slug/`)
- `source/`: Static pages (URL mirrors directory structure)
- `themes/default/templates/`: Jinja2 HTML templates
- `themes/default/dist/`: Compiled theme assets (Tailwind CSS + DaisyUI)
- `public/`: Generated static site output

### CLI Commands

- `toki build`: Generate static site
- `toki preview`: Start dev server with file watching and live reload
- `toki deploy`: Push to configured Git remote (GitHub Pages)

## Key Conventions

### C# Code Style

- File-scoped namespaces without braces
- Records for immutable data models (e.g., `ContentItem`, `SiteConfig`)
- `internal` visibility for non-public types
- ZLinq's `AsValueEnumerable()` for LINQ operations on collections
- Source generators for YAML deserialization (`YamlStaticContext`)

### Front Matter Format

```yaml
---
title: Post Title
date: 2024-01-15 12:00
slug: custom-slug
layout: post
tags: [tag1, tag2]
categories: [cat1]
---
```

### Template System

- Templates use Jinja2 syntax via MiniJinja
- Standard layouts: `base.html`, `post.html`, `page.html`, `index.html`
- View models implement `ITemplateSerializable` for MiniJinja rendering
- Post excerpts use `<!-- more -->` marker in Markdown

### Theme Development

The default theme uses:
- Vite (via rolldown-vite) for building
- Tailwind CSS v4 with DaisyUI components
- TypeScript for any JS functionality

Theme assets are automatically built during `dotnet build` via MSBuild targets.

## Dependencies

- **Markdig**: Markdown processing with advanced extensions
- **Tomlyn**: TOML configuration parsing
- **YamlDotNet**: YAML front matter parsing (with source generator)
- **MiniJinja**: Jinja2 template engine
- **ConsoleAppFramework**: CLI command structure
- **xUnit + FluentAssertions**: Testing framework

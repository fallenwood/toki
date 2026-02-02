namespace Toki;

using MiniJinja;

internal static class TemplateEngine {
  internal static void LoadTemplates(MiniJinja.Environment env, string templatesDir) {
    if (!Directory.Exists(templatesDir)) {
      Console.Error.WriteLine($"Missing templates directory: {templatesDir}");
      return;
    }

    var templateFiles = Directory.GetFiles(templatesDir, "*.html", SearchOption.AllDirectories);
    foreach (var file in templateFiles) {
      var name = Path.GetRelativePath(templatesDir, file).Replace("\\", "/");
      env.AddTemplate(name, File.ReadAllText(file));
    }
  }

  internal static bool TemplateExists(MiniJinja.Environment env, string name) {
    try {
      _ = env.GetTemplate(name);
      return true;
    } catch {
      return false;
    }
  }

  internal static void RenderToFile(MiniJinja.Environment env, string templateName, string outputPath, ITemplateSerializable model) {
    var template = env.GetTemplate(templateName);
    var rendered = template.Render(model);
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory)) {
      Directory.CreateDirectory(directory);
    }
    File.WriteAllText(outputPath, rendered, System.Text.Encoding.UTF8);
  }
}

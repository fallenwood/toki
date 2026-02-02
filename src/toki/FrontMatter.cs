namespace Toki;

using Microsoft.Extensions.Logging;
using System.Text;

internal static class FrontMatter {
  internal static (FrontMatterModel frontMatter, string body) ParseFrontMatter(string input, ILogger logger) {
    using var reader = new StringReader(input);
    var firstLine = reader.ReadLine();
    if (firstLine is null) {
      return (new FrontMatterModel(), input);
    }

    var yamlBuilder = new StringBuilder();
    yamlBuilder.AppendLine(firstLine);
    string? line;
    while ((line = reader.ReadLine()) != null) {
      if (line.Trim().Equals("---", StringComparison.Ordinal)) {
        break;
      }
      yamlBuilder.AppendLine(line);
    }

    var body = reader.ReadToEnd();

    logger.LogInformation("Parsed front matter:\n{FrontMatter}", yamlBuilder.ToString());

    var frontMatter = YamlStaticContext
      .Deserializer
      .Deserialize<FrontMatterModel>(yamlBuilder.ToString());

    return (frontMatter, body);
  }
}

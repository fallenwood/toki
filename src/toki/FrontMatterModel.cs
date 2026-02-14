namespace Toki;

using YamlDotNet.Serialization;

internal sealed class FrontMatterModel {
  [YamlMember(Alias = "title")]
  public string? Title { get; set; }

  [YamlMember(Alias = "description")]
  public string? Description { get; set; }

  [YamlMember(Alias = "layout")]
  public string? Layout { get; set; }

  [YamlMember(Alias = "slug")]
  public string? Slug { get; set; }

  [YamlMember(Alias = "date")]
  public DateTime? Date { get; set; }

  [YamlMember(Alias = "tags")]
  public List<string>? Tags { get; set; }

  [YamlMember(Alias = "categories")]
  public List<string>? Categories { get; set; }
}

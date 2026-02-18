namespace Toki {
  using System.Text.Json.Serialization;

  [JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  )]
  [JsonSerializable(typeof(List<SearchIndexEntry>))]
  internal partial class SearchIndexJsonContext : JsonSerializerContext {
  }
}

namespace Toki;

using YamlDotNet.Core;
using YamlDotNet.Serialization;

[YamlStaticContext]
[YamlSerializable(typeof(FrontMatterModel))]
public sealed partial class YamlStaticContext : StaticContext {
}

public sealed partial class YamlStaticContext {
  public static YamlStaticContext Instance { get; } = new YamlStaticContext();

  public static IDeserializer Deserializer {get;} = new StaticDeserializerBuilder(Instance)
    .Build();
  public static ISerializer Serializer {get;}= new StaticSerializerBuilder(Instance)
    .Build();
}

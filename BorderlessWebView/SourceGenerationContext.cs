using System.Text.Json.Serialization;

namespace BorderlessWebView;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ExtensionInfo))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

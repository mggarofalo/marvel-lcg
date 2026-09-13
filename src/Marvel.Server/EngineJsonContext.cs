using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(EngineRequest))]
[JsonSerializable(typeof(EngineResponse))]
internal sealed partial class EngineJsonContext : JsonSerializerContext;

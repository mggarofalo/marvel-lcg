using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Protocol;

[JsonSourceGenerationOptions(
    Converters = [typeof(DecisionJsonConverter)],
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(EngineRequest))]
[JsonSerializable(typeof(EngineResponse))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(Dictionary<string, long>))]
[JsonSerializable(typeof(Marvel.Rules.Play.ResourceAllocation[]))]
internal sealed partial class EngineJsonContext : JsonSerializerContext;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>The source-generated spelling of the engine-host protocol.</summary>
internal static class EngineJson
{
    public static byte[] Write(EngineRequest request) =>
        JsonSerializer.SerializeToUtf8Bytes(request, EngineJsonContext.Default.EngineRequest);

    public static byte[] Write(EngineResponse response) =>
        JsonSerializer.SerializeToUtf8Bytes(response, EngineJsonContext.Default.EngineResponse);

    public static EngineRequest ReadRequest(ReadOnlySpan<byte> json) =>
        JsonSerializer.Deserialize(json, EngineJsonContext.Default.EngineRequest)
        ?? throw new JsonException("request document was null");

    public static EngineResponse ReadResponse(ReadOnlySpan<byte> json) =>
        JsonSerializer.Deserialize(json, EngineJsonContext.Default.EngineResponse)
        ?? throw new JsonException("response document was null");

    public static int ReadResponseVersion(ReadOnlyMemory<byte> json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("version", out JsonElement version)
            || !version.TryGetInt32(out int value))
        {
            throw new JsonException("response version was missing or invalid");
        }

        return value;
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(EngineRequest))]
[JsonSerializable(typeof(EngineResponse))]
internal sealed partial class EngineJsonContext : JsonSerializerContext;

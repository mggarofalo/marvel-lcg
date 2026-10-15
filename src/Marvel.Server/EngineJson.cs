using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>The source-generated spelling of the engine-host protocol.</summary>
internal static class EngineJson
{
    // Embedded exchanges use the same value boundary as socket exchanges. The
    // protocol codec copies every nested wire field without sharing collections
    // with the sender or exposing server-only, nonserialized metadata.
    internal static EngineRequest Snapshot(EngineRequest request) => ReadRequest(Write(request));

    internal static EngineResponse Snapshot(EngineResponse response) => ReadResponse(Write(response));

    public static byte[] Write(EngineRequest request) =>
        JsonSerializer.SerializeToUtf8Bytes(request, EngineJsonContext.Default.EngineRequest);

    public static byte[] Write(EngineResponse response) =>
        JsonSerializer.SerializeToUtf8Bytes(response, EngineJsonContext.Default.EngineResponse);

    public static EngineRequest ReadRequest(ReadOnlySpan<byte> json) =>
        JsonSerializer.Deserialize(json, EngineJsonContext.Default.EngineRequest)
        ?? throw new JsonException("request document was null");

    public static EngineResponse ReadResponse(ReadOnlySpan<byte> json)
    {
        EngineResponse response =
            JsonSerializer.Deserialize(json, EngineJsonContext.Default.EngineResponse)
            ?? throw new JsonException("response document was null");
        if (response.Error is not null
            && string.IsNullOrWhiteSpace(response.Error.Code))
        {
            throw new JsonException("response error code was missing or invalid");
        }

        return response;
    }

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

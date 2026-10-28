using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Protocol;

/// <summary>The source-generated spelling of the engine-host protocol.</summary>
public static class EngineJson
{
    // Embedded exchanges use the same value boundary as socket exchanges. The
    // protocol codec copies every nested wire field without sharing collections
    // with the sender or exposing server-only, nonserialized metadata.
    /// <summary>Encodes or decodes a complete protocol value using the strict wire contract.</summary>
    public static EngineRequest Snapshot(EngineRequest request) => ReadRequest(Write(request));

    /// <summary>Encodes or decodes a complete protocol value using the strict wire contract.</summary>
    public static EngineResponse Snapshot(EngineResponse response) => ReadResponse(Write(response));

    /// <summary>Encodes or decodes a complete protocol value using the strict wire contract.</summary>
    public static byte[] Write(EngineRequest request) =>
        JsonSerializer.SerializeToUtf8Bytes(request, EngineJsonContext.Default.EngineRequest);

    /// <summary>Encodes or decodes a complete protocol value using the strict wire contract.</summary>
    public static byte[] Write(EngineResponse response) =>
        JsonSerializer.SerializeToUtf8Bytes(response, EngineJsonContext.Default.EngineResponse);

    /// <summary>Encodes or decodes a complete protocol value using the strict wire contract.</summary>
    public static EngineRequest ReadRequest(ReadOnlySpan<byte> json)
    {
        try
        {
            return JsonSerializer.Deserialize(json, EngineJsonContext.Default.EngineRequest)
                ?? throw new JsonException("request document was null");
        }
        catch (ArgumentException failure)
        {
            throw new JsonException("request contains an invalid domain value", failure);
        }
    }

    /// <summary>Encodes or decodes a complete protocol value using the strict wire contract.</summary>
    public static EngineResponse ReadResponse(ReadOnlySpan<byte> json)
    {
        EngineResponse response =
            JsonSerializer.Deserialize(json, EngineJsonContext.Default.EngineResponse)
            ?? throw new JsonException("response document was null");
        if (response.Error is not null
            && (string.IsNullOrWhiteSpace(response.Error.Code) || !Enum.IsDefined(response.Error.Disposition)))
        {
            throw new JsonException("response error code was missing or invalid");
        }

        if (!ConditionalResourceContract.IsValid(response.Prompt))
            throw new JsonException("response conditional resource bonus is invalid");

        if (response.World is { } world && !EncounterGroupContract.IsValid(world))
            throw new JsonException("response encounter groups contain invalid references");
        if (response.World is { } invocationWorld && !InvocationDeckContract.IsValid(invocationWorld))
            throw new JsonException("response Invocation decks contain invalid references");

        return response;
    }

    /// <summary>Encodes or decodes a complete protocol value using the strict wire contract.</summary>
    public static int ReadResponseVersion(ReadOnlyMemory<byte> json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("version", out JsonElement version)
            || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int value))
        {
            throw new JsonException("response version was missing or invalid");
        }

        return value;
    }
}

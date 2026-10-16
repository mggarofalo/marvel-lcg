using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Reads and writes the explicit protected authority envelope.</summary>
internal static class StoredAuthorityJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Write(IReadOnlyList<StoredAuthority> authorities) =>
        JsonSerializer.Serialize(new StoredAuthorityEnvelope(1, authorities), Options);

    public static IReadOnlyList<StoredAuthority> Read(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<StoredAuthorityEnvelope>(json, Options)
                ?? throw new SessionSaveException("stored authority is empty");
            if (envelope.Schema != 1 || envelope.Authorities is null)
                throw new SessionSaveException("stored authority schema is not supported");
            return envelope.Authorities;
        }
        catch (JsonException failure)
        {
            throw new SessionSaveException("stored authority is not valid JSON", failure);
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

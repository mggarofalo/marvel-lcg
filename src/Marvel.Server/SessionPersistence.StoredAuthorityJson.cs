using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A non-secret verifier and its server-authorized visibility scope.</summary>

internal static class StoredAuthorityJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Write(IReadOnlyList<StoredAuthority> authorities) =>
        JsonSerializer.Serialize(authorities, Options);

    public static IReadOnlyList<StoredAuthority> Read(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<StoredAuthority>>(json, Options)
                ?? throw new SessionSaveException("stored authority is empty");
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

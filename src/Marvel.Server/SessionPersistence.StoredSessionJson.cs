using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A non-secret verifier and its server-authorized visibility scope.</summary>

internal static class StoredSessionJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Write(StoredSession session)
    {
        Validate(session);
        return JsonSerializer.Serialize(session, Options);
    }

    public static StoredSession Read(string json)
    {
        try
        {
            var session = JsonSerializer.Deserialize<StoredSession>(json, Options)
                ?? throw new SessionSaveException("stored generation is empty");
            Validate(session, readable: true);
            return session;
        }
        catch (JsonException failure)
        {
            throw new SessionSaveException("stored generation is not valid JSON", failure);
        }
    }

    public static void ValidateLoaded(StoredSession session) =>
        Validate(session, readable: true);

    private static void Validate(StoredSession session, bool readable = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Save is null)
        {
            throw new SessionSaveException("stored generation has no save");
        }

        if (readable)
        {
            SessionSaveJson.ValidateReadable(session.Save);
        }
        else
        {
            SessionSaveJson.Validate(session.Save);
        }
        if (session.Authorities is null || session.Authorities.Any(InvalidAuthority))
        {
            throw new SessionSaveException("stored authority is invalid");
        }

        if (session.Authorities.Select(authority => authority.Verifier)
            .Distinct(StringComparer.Ordinal).Count() != session.Authorities.Count)
        {
            throw new SessionSaveException("stored authority verifier is duplicated");
        }

        if (!ValidLifecycle(session))
        {
            throw new SessionSaveException("stored lifecycle authority is invalid");
        }
    }

    private static bool InvalidAuthority(StoredAuthority authority) =>
        authority is null || authority.Verifier is not { Length: 64 }
        || authority.Verifier.Any(character => character is not (>= '0' and <= '9')
            and not (>= 'a' and <= 'f'))
        || authority.Seats is null || authority.Seats.Any(seat => seat < 0);

    private static bool ValidLifecycle(StoredSession session)
    {
        if (session.Authorities.Any(authority => authority.Owner && authority.Invitation))
            return false;
        int owners = session.Authorities.Count(authority => authority.Owner);
        if (session.Save.Session.Lifecycle == "active") return owners == 1;
        return session.Save.Session.Lifecycle != "retired" || session.Authorities.Count == 0;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new ResourceAllocationJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

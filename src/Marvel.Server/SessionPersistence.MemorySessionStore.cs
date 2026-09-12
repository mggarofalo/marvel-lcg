using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A non-secret verifier and its server-authorized visibility scope.</summary>

/// <summary>An isolated store used when a host has no filesystem authority.</summary>
public sealed class MemorySessionStore : ISessionStore
{
    private readonly Dictionary<string, string> generations = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<StoredSession> Load() =>
        [.. generations.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => StoredSessionJson.Read(pair.Value))];

    /// <inheritdoc />
    public string? Commit(StoredSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Save is null)
        {
            throw new SessionSaveException("stored generation has no save");
        }

        generations[session.Save.Session.StorageId] = StoredSessionJson.Write(session);
        return null;
    }
}

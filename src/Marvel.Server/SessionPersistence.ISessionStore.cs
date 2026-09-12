using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A non-secret verifier and its server-authorized visibility scope.</summary>

/// <summary>Atomically persists complete hosted-session generations.</summary>
public interface ISessionStore
{
    /// <summary>Loads only complete generations selected by committed manifests.</summary>
    IReadOnlyList<StoredSession> Load();

    /// <summary>Loads independent candidates so one corrupt session can be quarantined.</summary>
    IReadOnlyList<SessionLoadResult> LoadForRestore() =>
        [.. Load().Select(session => new SessionLoadResult(session, null, null))];

    /// <summary>Commits a complete generation and returns its opaque id when available.</summary>
    string? Commit(StoredSession session);

}

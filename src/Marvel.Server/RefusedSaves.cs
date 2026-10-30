using System.Collections.Concurrent;
using Marvel.Protocol;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Saves this runtime refuses to load, answered only to their own stored authority.</summary>
/// <remarks>The engine's choice: a save loads only under the exact current replay, RNG,
/// digest and dataset identities. There is no upgrade route, so a refused save is reported
/// to its holder with one typed code instead of disappearing behind "session not found".</remarks>
internal sealed class RefusedSaves
{
    /// <summary>The protocol error code for a save written under another identity.</summary>
    internal const string Code = "save_incompatible";

    private readonly ConcurrentDictionary<string, Refusal> capabilities = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Refusal> entries = new(StringComparer.Ordinal);

    internal void Refuse(StoredSession stored, SessionCompatibilityException failure)
    {
        var refusal = new Refusal(stored.Save.Session.Label, failure.Category);
        foreach (StoredAuthority authority in stored.Authorities.Where(item => !item.Revoked))
        {
            capabilities.TryAdd(authority.Verifier, refusal);
            if (authority.Entry is { } entry) entries.TryAdd(entry.Verifier, refusal);
        }
    }

    internal EngineResponse? Answer(EngineRequest request)
    {
        Refusal? refusal = null;
        if (EntryReceipts.Valid(request.Entry))
            entries.TryGetValue(EntryReceipts.Verifier(request.Entry!), out refusal);
        if (refusal is null && request.Capability is { Length: > 0 and <= EngineProtocol.MaximumIdentifierLength } capability
            && capabilities.TryGetValue(SessionAuthorityRegistry.Verifier(capability), out var found)
            && string.Equals(found.GameId, request.GameId, StringComparison.Ordinal))
            refusal = found;
        return refusal is null ? null : EngineHostLifecycle.Failed(request, Code, Message(refusal.Category));
    }

    internal static string Message(string category) => category == "unsupported_downgrade"
        ? "This saved game was written by a newer version of Marvel Champions. Install that version to continue it."
        : "This saved game was written by another engine or card data version, and this version does not load it. Start a new game.";

    private sealed record Refusal(string GameId, string Category);
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>A bounded compatibility category for operator quarantine diagnostics.</summary>
public sealed class SessionCompatibilityException(string category, string message)
    : SessionSaveException(message)
{
    /// <summary>The stable non-secret mismatch category.</summary>
    public string Category { get; } = category;
}

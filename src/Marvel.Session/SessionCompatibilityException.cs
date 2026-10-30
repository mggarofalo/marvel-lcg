using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>A save written under another engine or dataset identity, named by a bounded category.</summary>
public sealed class SessionCompatibilityException(string category, string message)
    : SessionSaveException(message)
{
    /// <summary>The stable non-secret mismatch category recorded for the operator.</summary>
    public string Category { get; } = category;
}

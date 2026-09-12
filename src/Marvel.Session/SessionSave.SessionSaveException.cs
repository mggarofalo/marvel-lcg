using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>A save cannot be safely parsed or replayed by this runtime.</summary>
public class SessionSaveException(string message, Exception? inner = null)
    : Exception(message, inner);

using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>A persisted decision or derived record no longer matches engine truth.</summary>
public sealed class ReplayDivergenceException(string message) : Exception(message);

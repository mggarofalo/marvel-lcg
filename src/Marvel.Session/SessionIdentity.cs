using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>The non-game identity and durable lifecycle of one hosted table.</summary>
public sealed record SessionIdentity(
    [property: JsonRequired] string StorageId,
    [property: JsonRequired] string Label,
    [property: JsonRequired] string Lifecycle);

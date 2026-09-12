using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>
public sealed record SessionCompatibility(
    [property: JsonRequired] string Application,
    [property: JsonRequired] string ReplayContract,
    [property: JsonRequired] string RngContract,
    [property: JsonRequired] string StateDigest,
    [property: JsonRequired] string CardsSha256,
    [property: JsonRequired] string SetupSha256,
    [property: JsonRequired] string AbilitiesSha256);

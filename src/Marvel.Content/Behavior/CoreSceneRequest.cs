using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>A legal Core Set deal from which one behavioral transcript begins.</summary>
public sealed record CoreSceneRequest(
    string Authority,
    string Campaign,
    IReadOnlyList<string> Heroes,
    uint Seed,
    IReadOnlyList<string>? ModularSets = null,
    IReadOnlyList<string>? PlayerDecks = null);

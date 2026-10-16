using Marvel.Rules.Setup;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>The complete deterministic input from which a game is dealt.</summary>
public sealed record SessionSetup(
    [property: JsonRequired] string Scenario,
    [property: JsonRequired] IReadOnlyList<string> Heroes,
    [property: JsonRequired] IReadOnlyList<string>? ModularSets,
    [property: JsonRequired] uint Seed,
    [property: JsonRequired] IReadOnlyList<PlayerDeckRecipe>? PlayerDecks = null)
{
    internal SessionSetup Snapshot() => this with
    {
        Heroes = JournalValues.List(Heroes),
        ModularSets = JournalValues.OptionalList(ModularSets),
        PlayerDecks = JournalValues.OptionalList(PlayerDecks),
    };
}

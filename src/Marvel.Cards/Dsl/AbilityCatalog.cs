using System.Collections.Frozen;
using static Marvel.Cards.Dsl.AbilityJsonReader;
using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>
/// Reads authored card abilities out of their canonical JSON.
/// </summary>
/// <remarks>
/// <para>
/// Takes a string, never a path: <c>Marvel.Cards</c> does no file or network
/// I/O (<c>docs/presentation-layer.md</c>'s dependency rules), and where the
/// bytes come from is a packaging decision nobody has made yet.
/// </para>
/// <para>
/// <b>Strict, and deliberately so.</b> Every unknown key is refused rather than
/// ignored. A card is data a player may one day author or download, and the
/// failure mode of a lenient reader is a card that looks accepted and does
/// three quarters of what it says — which nothing downstream can detect.
/// </para>
/// </remarks>
public static class AbilityCatalog
{
    // `note` carries the reasoning that JSON has nowhere else to put: why a card
    // was read the way it was, and what its data deliberately does not say.
    // Nothing reads it, and that is the point -- it is for the next person.
    internal static readonly FrozenSet<string> CardKeys =
        new[]
        {
            "card", "name", "note", "abilities", "attachTo", "controlledBy",
            "startingCounters",
        }.ToFrozenSet(StringComparer.Ordinal);

    internal static readonly FrozenSet<string> StartingCounterKeys =
        new[] { "type", "count", "uses" }.ToFrozenSet(StringComparer.Ordinal);

    internal static readonly FrozenSet<string> AbilityKeys =
        new[]
        {
            "name", "note", "trigger", "effect", "cost", "limitPerRound", "when",
            "anyPlayer", "labels", "printedResources", "maxPerRound", "maxPerPhase",
            "maxPerGame", "maxPerInstance",
        }.ToFrozenSet(StringComparer.Ordinal);

    internal static readonly FrozenSet<string> TriggerKeys =
        new[]
        {
            "event", "timing", "subject", "actor", "target", "form", "alsoHappened", "player",
        }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Parses the canonical ability dataset.</summary>
    /// <param name="json">The dataset text.</param>
    /// <exception cref="AbilityException">The text is not an ability dataset.</exception>
    public static AbilityBook Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = Read(json);
        if (!document.RootElement.TryGetProperty("cards", out var cards)
            || cards.ValueKind != JsonValueKind.Array)
        {
            throw new AbilityException("the ability dataset has no 'cards' array");
        }

        var catalog = new AbilityCatalogAccumulator();
        foreach (var element in cards.EnumerateArray())
        {
            catalog.Add(element);
        }
        return catalog.Build();
    }

}

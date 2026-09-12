using static Marvel.Cards.Dsl.AbilityJsonReader;
using System.Text.Json;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

internal sealed class AbilityCatalogAccumulator
{
    private readonly List<CardAbility> abilities = [];
    private readonly HashSet<string> authored = new(StringComparer.Ordinal);
    private readonly HashSet<string> described = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AbilityValue> attachTo = new(StringComparer.Ordinal);
    private readonly HashSet<string> controlledByFirstPlayer = new(StringComparer.Ordinal);
    private readonly HashSet<string> placementOnly = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CardCounterPool> counterPools = new(StringComparer.Ordinal);
    private readonly List<string> incomplete = [];

    internal void Add(JsonElement element)
    {
        Refuse(element, AbilityCatalog.CardKeys, "a card");
        string card = Text(element, "card")
            ?? throw new AbilityException("a card has no 'card'");
        string name = Text(element, "name") ?? card;
        if (!described.Add(card))
        {
            throw new AbilityException($"card '{card}' is authored twice");
        }

        ReadAttachment(element, card);
        ReadController(element, card);
        ReadCounters(element, card);
        ReadAbilities(element, card, name);
    }

    internal AbilityBook Build()
    {
        if (incomplete.Count > 0)
        {
            throw new AbilityException(
                $"card '{incomplete[0]}' has neither abilities nor placement data");
        }
        return new AbilityBook(
            abilities, authored, attachTo, controlledByFirstPlayer, placementOnly,
            counterPools);
    }

    private void ReadAttachment(JsonElement element, string card)
    {
        if (!element.TryGetProperty("attachTo", out var host)) return;
        if (host.ValueKind != JsonValueKind.String) Node(host, card);
        attachTo[card] = Value(host, card);
    }

    private void ReadController(JsonElement element, string card)
    {
        if (!element.TryGetProperty("controlledBy", out var controller)) return;
        if (controller.ValueKind != JsonValueKind.String
            || !string.Equals(controller.GetString(), "firstPlayer", StringComparison.Ordinal))
        {
            throw new AbilityException(
                $"card '{card}' has a 'controlledBy' other than 'firstPlayer'");
        }
        controlledByFirstPlayer.Add(card);
    }

    private void ReadCounters(JsonElement element, string card)
    {
        if (element.TryGetProperty("startingCounters", out var counters))
        {
            counterPools[card] = StartingCounters(counters, card);
        }
    }

    private void ReadAbilities(JsonElement element, string card, string name)
    {
        if (!element.TryGetProperty("abilities", out var list))
        {
            if (HasPlacementData(element)) placementOnly.Add(card);
            else incomplete.Add(card);
            return;
        }
        authored.Add(card);
        foreach (var ability in list.EnumerateArray())
        {
            abilities.Add(Ability(card, name, ability));
        }
    }

    private static bool HasPlacementData(JsonElement element) =>
        element.TryGetProperty("attachTo", out _)
        || element.TryGetProperty("controlledBy", out _)
        || element.TryGetProperty("startingCounters", out _);
}

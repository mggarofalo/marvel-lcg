using System.Globalization;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Words for supplied persistent meanings; never evaluates an ability or adds a host total.</summary>
internal static class CardSourceSummary
{
    internal static IReadOnlyList<string> Lines(BoardCardPresentation card)
    {
        if (card.Concealed || card.Persistent is not { } source) return [];
        var lines = new List<string>();
        foreach (CardPersistentAbilityDescriptor ability in source.Abilities)
        {
            string trigger = Trigger(ability.Trigger);
            string costs = string.Join(" + ", ability.Costs.Select(Cost));
            string effects = string.Join("; ", ability.Effects.Select(Effect));
            lines.Add(trigger + (costs.Length == 0 ? "" : costs + " → ") + effects);
        }
        if (card.Damage > 0 || StoresDamage(source)) lines.Add($"{card.Damage} damage here");
        foreach (BoardFieldPresentation counter in card.Counters) lines.Add($"{counter.Value} {counter.Name.ToLowerInvariant()}");
        if (SpatialTableZones.IsExhausted(card)) lines.Add("Exhausted");
        if (source.HasUnresolvedAbilities) lines.Add("Further abilities: inspect rules");
        return lines;
    }

    private static bool StoresDamage(CardPersistentDescriptor source) => source.Abilities
        .SelectMany(ability => ability.Effects).Any(effect => effect.Operation == "RedirectAllDamage" && effect.Target == "Source");

    internal static string Contribution(CardContributionDescriptor value) =>
        $"{CardValueSourceInspection.Operation(new(value.Operation, value.Amount, null, value.Duration))} {Attribute(value.Attribute)}";

    private static string Attribute(string value) => value switch
    { "HP" => "max HP", "HS" => "hand size", _ => value };

    private static string Trigger(CardPersistentTriggerDescriptor value) => value.Event switch
    {
        "WhenAttackInitiated" => "Next attack: ",
        "WhenDamageWouldBeDealt" => "",
        "WhenCardDefeated" => $"When {Role(value.Subject == "attachedTo" ? "Host" : "Source")} is defeated: ",
        null when value.Timing == "Constant" => "",
        "WhenActionTriggered" when value.Timing == "Action" => value.Form == "hero" ? "Hero action: " : "Action: ",
        _ => $"{value.Timing}: ",
    };

    private static string Cost(CardPersistentCostDescriptor value) => value.Operation switch
    {
        "Exhaust" => $"Exhaust {Role(value.Target)}",
        "Discard" => $"Discard {Role(value.Target)}",
        "RemoveCounters" => $"Remove {value.Amount} {value.Counter} from {Role(value.Target)}",
        "SpendResources" => $"Spend {Resources(value.Resources ?? "")}{(value.PrintedOnly ? " (printed)" : "")}",
        _ => value.Operation,
    };

    private static string Resources(string value) => string.Join(" ", CardRulesMarkup.ResourceTokens(value)
        .Select(item => $"[{item.Name.ToLowerInvariant()}]"));

    private static string Effect(CardPersistentEffectDescriptor value)
    {
        string text = value.Operation switch
        {
            "Ready" => $"ready {Role(value.Target)}",
            "Exhaust" => $"exhaust {Role(value.Target)}",
            "Discard" => $"discard {Role(value.Target)}",
            "RedirectAllDamage" => $"All damage → {Role(value.Target, here: true)}",
            "RemoveThreat" => $"remove {value.Amount} threat from {Role(value.Target)}",
            "Grant" => value.Field == "overkill" ? "overkill" : $"{value.Field} {value.Amount?.ToString(CultureInfo.InvariantCulture)}",
            _ => value.Operation,
        };
        if (value.Until is { } until) text += until == "EndOfAttack" ? " until attack ends" : $" until {until}";
        if (value.After is { } after) text += after == "WhenAttackEnds" ? " after attack" : $" after {after}";
        if (value.Condition is { } condition)
            text += $" when {condition.Counter} {Role(condition.Target, here: true)} ≥{condition.Minimum}";
        return text;
    }

    private static string Role(string value, bool here = false) => value switch
    {
        "Source" => here ? "here" : "this",
        "Host" => "attached card",
        "ActingIdentity" => "your hero",
        "ActingPlayer" => "you",
        "ChosenScheme" => "a chosen scheme",
        "TriggerActor" => "the acting card",
        "TriggerSubject" => "the affected card",
        "TriggerTarget" => "the target",
        _ => value,
    };
}

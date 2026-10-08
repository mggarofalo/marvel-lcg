using System.Globalization;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Selects stat display facts without evaluating effects or modifiers.</summary>
internal static class CardStatValues
{
    private static readonly (string Printed, string Current)[] Names =
    [
        ("THW", "THWART"), ("SCH", "SCHEME"), ("ATK", "ATTACK"), ("DEF", "DEFENSE"),
        ("REC", "RECOVER"), ("HS", "HAND_SIZE"),
        ("REC+", "REC+"), ("THW+", "THW+"), ("ATK+", "ATK+"),
        ("DEF+", "DEF+"), ("SCH+", "SCH+"), ("HP+", "HP+"),
        ("StartingThreat", "PRINTED_STARTING_THREAT"), ("EscalationThreat", "ESCALATION_THREAT"),
    ];

    internal static IReadOnlyList<CardStatValue> From(BoardCardPresentation card)
    {
        if (card.Concealed) return [];
        var result = new List<CardStatValue>();
        foreach ((string name, string currentName) in Names)
        {
            if (Select(card, name, currentName) is { } value) result.Add(value);
        }
        return result;
    }

    private static CardStatValue? Select(BoardCardPresentation card, string name, string currentName)
    {
        BoardPrintedValueMark? printed = card.PrintedMarks.FirstOrDefault(value => value.Attribute == name);
        BoardFieldPresentation? current = card.Fields.FirstOrDefault(value => value.Name == currentName || value.Name == name);
        CardEffectiveValue? effective = card.EffectiveValues.GetValueOrDefault(name);
        if (printed is null && current is null && effective is null) return null;
        string? evaluated = EvaluatedValue(effective);
        string baseline = Numeral(name, PrintedValue(printed, current, evaluated));
        string fallback = Fallback(printed, current, evaluated);
        return Describe(name, baseline, Numeral(name, evaluated ?? fallback), printed,
            ShowsPerPlayer(effective, current, printed), effective);
    }

    private static CardStatValue Describe(string name, string baseline, string value,
        BoardPrintedValueMark? printed, bool perPlayer, CardEffectiveValue? effective) =>
        new(name, baseline, value, perPlayer, printed?.ConsequentialDamage ?? 0)
        {
            SpecialStar = printed?.SpecialStar == true,
            Modified = effective?.IsModified == true,
        };

    private static string? EvaluatedValue(CardEffectiveValue? effective) =>
        effective?.CurrentValue.ToString(CultureInfo.InvariantCulture);

    private static string PrintedValue(BoardPrintedValueMark? printed, BoardFieldPresentation? current, string? evaluated) =>
        printed?.Value ?? current?.Value ?? evaluated!;

    private static string Fallback(BoardPrintedValueMark? printed, BoardFieldPresentation? current, string? evaluated) =>
        printed?.Value is "X" or "—" or "★" ? printed.Value : current?.Value ?? printed?.Value ?? evaluated!;

    private static bool ShowsPerPlayer(CardEffectiveValue? effective, BoardFieldPresentation? current,
        BoardPrintedValueMark? printed) => effective is null && current is null && printed?.PerPlayer == true;

    private static string Numeral(string name, string value)
    {
        string numeral = value;
        return name.EndsWith('+') && !numeral.StartsWith('+') && !numeral.StartsWith('-')
            ? $"+{numeral}" : numeral;
    }
}

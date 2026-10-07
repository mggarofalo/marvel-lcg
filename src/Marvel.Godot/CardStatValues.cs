using Marvel.View;

namespace Marvel.Godot;

/// <summary>Selects stat display facts without evaluating effects or modifiers.</summary>
internal static class CardStatValues
{
    private static readonly (string Printed, string Current)[] Names =
    [
        ("THW", "THWART"), ("ATK", "ATTACK"), ("DEF", "DEFENSE"), ("SCH", "SCHEME"),
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
        BoardFieldPresentation? printed = card.PrintedStats.FirstOrDefault(value => value.Name == name);
        BoardFieldPresentation? current = card.Fields.FirstOrDefault(value => value.Name == currentName || value.Name == name);
        if (printed is null && current is null) return null;
        return Describe(name, printed, current, card.PrintedMarks.FirstOrDefault(value => value.Attribute == name));
    }

    private static CardStatValue Describe(string name, BoardFieldPresentation? printed,
        BoardFieldPresentation? current, BoardPrintedValueMark? mark) =>
        new(name, Numeral(name, (printed ?? current!).Value), Numeral(name, (current ?? printed!).Value),
            current is null && mark?.PerPlayer == true, mark?.ConsequentialDamage ?? 0);

    private static string Numeral(string name, string value)
    {
        string numeral = value.TrimEnd('*');
        return name.EndsWith('+') && !numeral.StartsWith('+') && !numeral.StartsWith('-')
            ? $"+{numeral}" : numeral;
    }
}

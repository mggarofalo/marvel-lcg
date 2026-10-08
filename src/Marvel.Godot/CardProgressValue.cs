using Marvel.View;

namespace Marvel.Godot;

/// <summary>Selects supplied health or threat values without evaluating their quantities.</summary>
internal sealed record CardProgressValue(string FieldName, string Value, bool IsThreat, bool PerPlayer)
{
    internal bool MaximumModified { get; init; }

    internal static CardProgressValue? From(BoardCardPresentation card)
    {
        if (card.Concealed) return null;
        if (card.Fields.FirstOrDefault(field => field.Name == "HEALTH") is { } health)
            return Health(card, health);
        if (card.Fields.FirstOrDefault(field => field.Name == "THREAT") is { } threat)
        {
            if (card.Fields.FirstOrDefault(field => field.Name == "TARGET_THREAT") is { } maximum)
                return new(threat.Name, $"{threat.Value}/{maximum.Value}", true, false);
            if (card.PrintedMarks.FirstOrDefault(field => field.Attribute == "TargetThreat") is { } printedMaximum)
                return new(threat.Name, $"{threat.Value}/{printedMaximum.Value}", true,
                    IsPerPlayer(card, printedMaximum.Attribute));
            return new(threat.Name, threat.Value, true, false);
        }
        BoardPrintedValueMark? printed = card.PrintedMarks.FirstOrDefault(field => field.Attribute == "HP")
            ?? card.PrintedMarks.FirstOrDefault(field => field.Attribute == "TargetThreat");
        return printed is null ? null : new(printed.Attribute, printed.Value,
            printed.Attribute == "TargetThreat", printed.PerPlayer);
    }

    private static CardProgressValue Health(BoardCardPresentation card, BoardFieldPresentation health) =>
        new(health.Name, health.Value, false, false)
        { MaximumModified = card.EffectiveValues.TryGetValue("HP", out CardEffectiveValue? maximum) && maximum.IsModified };

    private static bool IsPerPlayer(BoardCardPresentation card, string attribute) =>
        card.PrintedMarks.Any(mark => mark.Attribute == attribute && mark.PerPlayer);
}

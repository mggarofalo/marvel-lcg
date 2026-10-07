using Marvel.View;

namespace Marvel.Godot;

/// <summary>Selects supplied health or threat values without evaluating their quantities.</summary>
internal sealed record CardProgressValue(string FieldName, string Value, bool IsThreat, bool PerPlayer)
{
    internal static CardProgressValue? From(BoardCardPresentation card)
    {
        if (card.Concealed) return null;
        if (card.Fields.FirstOrDefault(field => field.Name == "HEALTH") is { } health)
            return new(health.Name, health.Value, false, false);
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

    private static bool IsPerPlayer(BoardCardPresentation card, string attribute) =>
        card.PrintedMarks.Any(mark => mark.Attribute == attribute && mark.PerPlayer);
}

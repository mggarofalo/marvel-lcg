namespace Marvel.Cards.Run;

/// <summary>The exact incarnations offered as one ordered threat move.</summary>
internal sealed record AbilityThreatMovePair(AbilityCardReference From, AbilityCardReference To)
{
    internal bool Current => From.Card.Incarnation == From.Incarnation
        && To.Card.Incarnation == To.Incarnation
        && From.Card.Area.Id == From.Area && To.Card.Area.Id == To.Area;
}

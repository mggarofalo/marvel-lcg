namespace Marvel.Cards.Dsl;

/// <summary>Effects that place, remove or move named counters on cards.</summary>
public abstract record AbilityCounterEffect : AbilityEffect
{
    private AbilityCounterEffect() { }

    /// <summary>Place named counters on a card.</summary>
    public sealed record PlaceCounters(AbilityCardSelection Card, string Counter, AbilityNumber Count) : AbilityCounterEffect;
    /// <summary>Remove named counters from a card.</summary>
    public sealed record RemoveCounters(AbilityCardSelection Card, string Counter, long Count) : AbilityCounterEffect;
    /// <summary>Move named counters from one card to another.</summary>
    public sealed record MoveCounters(AbilityCardSelection From, AbilityCardSelection To, string Counter, long Count) : AbilityCounterEffect;
}

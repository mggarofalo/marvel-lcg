namespace Marvel.Cards.Dsl;

/// <summary>Effects that open a card play from a hand while their ability resolves.</summary>
public abstract record AbilityCardPlayEffect : AbilityEffect
{
    private AbilityCardPlayEffect() { }

    /// <summary>Play a card from the resolving player's hand, its resource cost reduced by <paramref name="Reduction"/>; an <paramref name="Optional"/> play may be declined.</summary>
    public sealed record PlayFromHand(long Reduction, bool Optional) : AbilityCardPlayEffect;
}

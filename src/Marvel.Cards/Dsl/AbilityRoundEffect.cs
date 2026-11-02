namespace Marvel.Cards.Dsl;

/// <summary>Effects that record what has happened this round.</summary>
public abstract record AbilityRoundEffect : AbilityEffect
{
    private AbilityRoundEffect() { }

    /// <summary>Record a named fact until the end of the round.</summary>
    public sealed record MarkThisRound(string Mark) : AbilityRoundEffect;
}

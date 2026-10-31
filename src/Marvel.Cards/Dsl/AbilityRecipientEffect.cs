namespace Marvel.Cards.Dsl;

/// <summary>Effects that first choose a bounded group of distinct characters, then resolve on each.</summary>
public abstract record AbilityRecipientEffect : AbilityEffect
{
    private AbilityRecipientEffect() { }

    /// <summary>Select up to a bounded number of distinct damaged characters, then heal each the same amount.</summary>
    public sealed record ChooseHealRecipients(AbilityCardSelection From, long Amount, int Maximum) : AbilityRecipientEffect;
}

namespace Marvel.Cards.Dsl;

/// <summary>Checked price modifiers for candidate cards and future card plays.</summary>
public abstract record AbilityPricingEffect : AbilityEffect
{
    private AbilityPricingEffect() { }

    /// <summary>A constant discount for the first ally played globally each round.</summary>
    public sealed record ReduceFirstAllyCost(long Amount) : AbilityPricingEffect;
    /// <summary>Reduce each upgrade played onto this physical host.</summary>
    public sealed record ReduceUpgradeCostOnThis(long Amount) : AbilityPricingEffect;
    /// <summary>Reduce only this candidate's play cost for each minion engaged with its future controller.</summary>
    public sealed record ReduceSelfCostPerEngagedMinion(long Amount) : AbilityPricingEffect;
    /// <summary>Reduce this candidate's play cost per controlled in-play card with a trait.</summary>
    public sealed record ReduceSelfCostPerControlledTrait(string Trait, long Amount) : AbilityPricingEffect;
    /// <summary>Increase your next event cost, then discard this source after that event.</summary>
    public sealed record IncreaseNextEventCost(long Amount) : AbilityPricingEffect;
    /// <summary>Reduce the next card cost paid by a player.</summary>
    public sealed record ReduceNextCardCost(AbilityPlayer Player, AbilityNumber Amount) : AbilityPricingEffect
    {
        /// <summary>Any player may use the next matching card reduction.</summary>
        public bool AnyPlayer { get; init; }
        /// <summary>Optional printed card kind and trait restriction.</summary>
        public Marvel.Rules.Play.CardCostFilter? Filter { get; init; }
    }
}

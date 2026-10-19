using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>A checked cost, separate from the effect it pays for.</summary>
public abstract record AbilityCost
{
    private AbilityCost() { }

    /// <summary>Costs paid together in authored order, subject to engine atomicity rules.</summary>
    public sealed record Sequence(ImmutableArray<AbilityCost> Costs) : AbilityCost;
    /// <summary>Exhaust the source or resolving identity.</summary>
    public sealed record Exhaust(AbilityCostCard Card) : AbilityCost;
    /// <summary>Exhaust the single named card controlled by the resolving player.</summary>
    public sealed record ExhaustTitled(string Title) : AbilityCost;
    /// <summary>Discard the source or resolving identity.</summary>
    public sealed record Discard(AbilityCostCard Card) : AbilityCost;
    /// <summary>Return the source from play to its owner's hand.</summary>
    public sealed record ReturnSourceToHand : AbilityCost;
    /// <summary>Discard a campaign TECH reward and permanently remove its saved instance.</summary>
    public sealed record ConsumeCampaignCard(AbilityCostCard Card) : AbilityCost;
    /// <summary>Remove a positive number of named counters.</summary>
    public sealed record RemoveCounters(AbilityCostCard Card, string Counter, long Count) : AbilityCost;
    /// <summary>Generate the required resources, optionally using printed icons only.</summary>
    public sealed record Spend(string Resources, bool PrintedOnly) : AbilityCost
    {
        /// <summary>Additional resources with no required type.</summary>
        public int Generic { get; init; }
        /// <summary>Total icons required, including unrestricted resources.</summary>
        public int Amount => checked(Resources.Length + Generic);
    }
    /// <summary>Define and pay a positive X in energy resources.</summary>
    public sealed record SpendEnergy : AbilityCost;
    /// <summary>Discard selected cards from the resolving player's hand.</summary>
    public sealed record DiscardFromHand(AbilityCostRange Range) : AbilityCost;
    /// <summary>Exhaust selected cards from a supported payment relation.</summary>
    public sealed record ExhaustChosen(AbilityCardQuery From, AbilityCostRange Range) : AbilityCost;
    /// <summary>Heal a positive amount of damage from the cost's card.</summary>
    public sealed record Heal(AbilityCostCard Card, long Amount) : AbilityCost;
    /// <summary>Deal damage, or require that all of it be taken to pay the cost.</summary>
    public sealed record Damage(AbilityCostCard Card, long Amount, bool MustTakeAll) : AbilityCost;
}

using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>A checked cost, separate from the effect it pays for.</summary>
public abstract record AbilityCost
{
    private AbilityCost() { }

    /// <summary>Pay the current public Invocation top and bind that physical card.</summary>
    public sealed record InvocationTop : AbilityCost;

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
    /// <summary>Return the single named controlled card from play to its owner's hand.</summary>
    public sealed record ReturnTitledToHand(string Title) : AbilityCost;
    /// <summary>Discard an exact number from the top of the specified deck.</summary>
    public sealed record DiscardTop(AbilitySearchArea From, int Count) : AbilityCost;
    /// <summary>Discard until a minion is found, then put that physical minion into play engaged with you.</summary>
    public sealed record DiscardUntilMinionEngaged : AbilityCost;
    /// <summary>Deal one facedown encounter card to the resolving player.</summary>
    public sealed record DealEncounterToYou : AbilityCost;
    /// <summary>Discard a campaign TECH reward and permanently remove its saved instance.</summary>
    public sealed record ConsumeCampaignCard(AbilityCostCard Card) : AbilityCost;
    /// <summary>Remove a positive number of named counters.</summary>
    public sealed record RemoveCounters(AbilityCostCard Card, string Counter, long Count) : AbilityCost;
    /// <summary>Generate the required resources, optionally using printed icons only.</summary>
    public sealed record Spend(string Resources, bool PrintedOnly) : AbilityCost
    {
        /// <summary>Additional resources with no required type.</summary>
        public int Generic { get; init; }
        /// <summary>Each paid icon declares a different resource type.</summary>
        public bool DistinctTypes { get; init; }
        /// <summary>Total icons required, including unrestricted resources.</summary>
        public int Amount => checked(Resources.Length + Generic);
    }
    /// <summary>Define and pay a positive X in energy resources.</summary>
    public sealed record SpendEnergy : AbilityCost;
    /// <summary>Discard selected cards from the resolving player's hand.</summary>
    public sealed record DiscardFromHand(AbilityCostRange Range) : AbilityCost;

    /// <summary>Discard a fixed number of random hand cards as an arrow cost.</summary>
    public sealed record RandomDiscardFromHand(int Count) : AbilityCost;
    /// <summary>Exhaust selected cards from a supported payment relation.</summary>
    public sealed record ExhaustChosen(AbilityCardQuery From, AbilityCostRange Range) : AbilityCost
    {
        /// <summary>Required current trait of the character paying this cost.</summary>
        public string? Trait { get; init; }
        /// <summary>Capture the selected ally's matching modified power for one basic use.</summary>
        public bool CaptureBasicUsePower { get; init; }
    }
    /// <summary>Choose one trait-filtered physical card in your discard pile before payment.</summary>
    public sealed record ChooseFromDiscard(string Trait) : AbilityCost;
    /// <summary>Heal a positive amount of damage from the cost's card.</summary>
    public sealed record Heal(AbilityCostCard Card, long Amount) : AbilityCost;
    /// <summary>Deal damage, or require that all of it be taken to pay the cost.</summary>
    public sealed record Damage(AbilityCostCard Card, long Amount, bool MustTakeAll) : AbilityCost;
}

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
    /// <summary>Discard the source or resolving identity.</summary>
    public sealed record Discard(AbilityCostCard Card) : AbilityCost;
    /// <summary>Remove a positive number of named counters.</summary>
    public sealed record RemoveCounters(AbilityCostCard Card, string Counter, long Count) : AbilityCost;
    /// <summary>Generate the required resources, optionally using printed icons only.</summary>
    public sealed record Spend(string Resources, bool PrintedOnly) : AbilityCost;
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

/// <summary>Cards a cost can identify without a player choice.</summary>
public enum AbilityCostCard
{
    /// <summary>The ability's source.</summary>
    Source,
    /// <summary>The resolving player's identity.</summary>
    Identity,
}

/// <summary>A checked cardinality for a cost's player selection.</summary>
public abstract record AbilityCostRange
{
    private AbilityCostRange() { }
    /// <summary>Exactly this many cards.</summary>
    public sealed record Exact(int Count) : AbilityCostRange;
    /// <summary>At least one and no more than this many cards.</summary>
    public sealed record UpTo(int Count) : AbilityCostRange;
    /// <summary>Any positive number of available cards.</summary>
    public sealed record Any : AbilityCostRange;
}

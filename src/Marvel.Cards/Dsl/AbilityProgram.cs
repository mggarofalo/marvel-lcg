using System.Collections.Immutable;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

/// <summary>A deterministic address of an effect in one authored ability.</summary>
/// <remarks>
/// Paths use explicit DSL field names and ordered list indexes, never CLR type
/// names. This internal lookup does not change the session-ledger wire format.
/// </remarks>
public sealed record AbilityEffectAddress(string Card, int Ability, string Path);

/// <summary>One ability whose executable syntax has been lowered completely.</summary>
public sealed record CompiledCardAbility(
    string Card, string Name, AbilityTrigger Trigger, AbilityEffect Effect,
    AbilityCost? Cost, AbilityCondition? When, long? Limit, bool AnyPlayer,
    ImmutableArray<string> Labels, string PrintedResources, AbilityMaximum? Maximum,
    AbilityEffectAddress Address);

/// <summary>An immutable, validated ability book with deterministic effect addresses.</summary>
public sealed class AbilityProgram
{
    internal AbilityProgram(
        ImmutableArray<CompiledCardAbility> abilities,
        ImmutableHashSet<string> authored,
        ImmutableDictionary<string, AbilityCardSelection> attachTo,
        ImmutableHashSet<string> controlledByFirstPlayer,
        ImmutableHashSet<string> placementOnly,
        ImmutableDictionary<string, CardCounterPool> counterPools,
        ImmutableDictionary<AbilityEffectAddress, AbilityEffect> effects)
    {
        Abilities = abilities;
        Authored = authored;
        AttachTo = attachTo;
        ControlledByFirstPlayer = controlledByFirstPlayer;
        PlacementOnly = placementOnly;
        CounterPools = counterPools;
        Effects = effects;
        byCard = abilities.GroupBy(ability => ability.Card, StringComparer.Ordinal)
            .ToImmutableDictionary(group => group.Key, group => group.ToImmutableArray(), StringComparer.Ordinal);
    }

    private readonly ImmutableDictionary<string, ImmutableArray<CompiledCardAbility>> byCard;

    /// <summary>Abilities in their authored order.</summary>
    public ImmutableArray<CompiledCardAbility> Abilities { get; }
    /// <summary>Faces known to be authored, including those with no abilities.</summary>
    public ImmutableHashSet<string> Authored { get; }
    /// <summary>Checked attachment relations by printed face.</summary>
    public ImmutableDictionary<string, AbilityCardSelection> AttachTo { get; }
    /// <summary>Faces whose setup controller is the first player.</summary>
    public ImmutableHashSet<string> ControlledByFirstPlayer { get; }
    /// <summary>Faces with known placement and reveal silence only.</summary>
    public ImmutableHashSet<string> PlacementOnly { get; }
    /// <summary>Starting counter pools by printed face.</summary>
    public ImmutableDictionary<string, CardCounterPool> CounterPools { get; }
    /// <summary>Every effect and its stable structural address.</summary>
    public ImmutableDictionary<AbilityEffectAddress, AbilityEffect> Effects { get; }

    /// <summary>The abilities on one printed face, in authored order.</summary>
    public ImmutableArray<CompiledCardAbility> On(string card) => byCard.GetValueOrDefault(card, []);
}

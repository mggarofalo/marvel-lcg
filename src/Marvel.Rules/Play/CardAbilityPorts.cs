using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text needed while resolving an encounter card.</summary>
public interface IEncounterCardAbilities
{
    IReadOnlyList<GameEvent> EntersPlay(World world, Card card);
    IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player);
    IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player, Occurrence occurrence);
    IReadOnlyList<PendingAbility> WhenRevealedAbilities(World world, Card card, int player);
    bool CancelWhenRevealed(World world, Card card, int player, Occurrence occurrence);
    IReadOnlyList<GameEvent> Boost(World world, Card card, int player);
}

/// <summary>Card text needed by the damage procedure.</summary>
public interface ICardDamageAbilities
{
    bool CanTakeDamage(World world, Card target, Card source);
    DamageProjection PreviewDamageReplacement(World world, Card target, Card source, long amount);
    DefeatProjection? PreviewDefeatReplacement(World world, Card target, long maximumHealth);
    long WouldBeDealt(World world, Card target, Card source, long amount, List<GameEvent> events);
    long WouldTake(World world, Card target, Card source, long amount, List<GameEvent> events);
    void DamagePreventedByTough(World world, Card target, Card source, List<GameEvent> events);
    void WouldBeDefeated(World world, Card target, List<GameEvent> events);
    bool WouldBeDefeated(World world, Card target, Card source, string trigger, string verb, int by,
        List<GameEvent> events, Occurrence? recordDefeatOn = null);
    IReadOnlyList<GameEvent> WhenCardDefeated(World world, Card card, Defeated defeated);
    bool WhenCardDefeated(World world, Card card, Defeated defeated, string trigger, List<GameEvent> events);
}

/// <summary>Card text that can prohibit a card from readying.</summary>
public interface ICardReadinessAbilities
{
    bool CanReady(World world, Card target, Card source);
}

/// <summary>Card text needed to remove threat.</summary>
// Completing a main scheme is part of threat removal; the ensuing stage must
// reveal its card text, so this cohesive port includes that nested operation.
public interface IThreatCardAbilities : IEncounterCardAbilities
{
    bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1);
}

/// <summary>Card text that resolves a labelled basic power.</summary>
public interface ICardPowerAbilities
{
    void ResolveCardAttack(World world, CharacterAttack attack, Occurrence occurrence, List<GameEvent> events);
    void ResolveCardThwart(World world, CharacterThwart thwart, Occurrence occurrence, List<GameEvent> events);
}

/// <summary>Card text used while paying a cost.</summary>
public interface IResourceCardAbilities
{
    string ResourcesGeneratedBy(World world, Card source, Card? payingFor);
    IReadOnlyList<ResourceSource> PrintedResourceAbilities(World world, int player);
    IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player);
    string ResourceGeneratorName(World world, int player, int card);
    string UseResource(World world, int player, int card, List<GameEvent> events);
}

/// <summary>Card text that starts or resumes an ability outside a window.</summary>
public interface ICardContinuationAbilities
{
    IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation);
    IReadOnlyList<GameEvent> ResolveSpecial(World world, Card card, int player, bool finalStep);
    IReadOnlyList<GameEvent> ResolveEachPlayer(World world, Card source, int player, int stoppedAt,
        AbilityType? tier, bool finalStep, bool finalPlayer);
    Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier = null);
    Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier, bool finalStep);
    Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier,
        bool finalStep, bool eachPlayerFrame, bool finalPlayer);
    IReadOnlyList<GameEvent> Chose(World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier = null);
    IReadOnlyList<GameEvent> Chose(World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer, string trigger);
}

/// <summary>Card text waiting for an enemy activation to complete.</summary>
public interface IActivationCompletionAbilities
{
    IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result);
}

/// <summary>Card-defined placement facts.</summary>
public interface ICardPlacementAbilities
{
    int? AttachesTo(World world, Card card);
    IReadOnlyList<int>? AttachmentTargets(World world, Card card);
}

/// <summary>Card-defined setup facts.</summary>
public interface ICardSetupAbilities
{
    int? SetupController(World world, Card card);
    void ValidateForPlay(World world);
    IReadOnlyList<Card> PlayerSetupCards(World world, int player);
    IReadOnlyList<GameEvent> Setup(World world, Card card);
}

/// <summary>Card-defined continuous effects.</summary>
public interface ICardConstantAbilities
{
    IReadOnlyList<ContinuousEffect> Constant(World world, Card card);
}

/// <summary>Card actions and their player-facing descriptions.</summary>
public interface ICardActionAbilities : IAbilityDescriptions
{
    IReadOnlyList<PendingAbility> Actions(World world, int player);
    IReadOnlyList<GameEvent> Act(World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null);
    IReadOnlyList<GameEvent> Act(World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, Occurrence occurrence,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null);
}

/// <summary>Card text used by enemy attack resolution.</summary>
public interface IAttackCardAbilities
{
    DefenderChoice Defenders(World world, EnemyAttack attack, IReadOnlyList<Card> candidates);
    IReadOnlyList<GameEvent> Boost(World world, Card card, int player);
}

/// <summary>Card text used by card play and its enter-play transition.</summary>
public interface ICardPlayAbilities : ICardPlacementAbilities, IEncounterCardAbilities, ICardCounterPools
{
}

/// <summary>Card text used by the encounter-card reveal procedure.</summary>
public interface IRevealCardAbilities : IThreatCardAbilities, ICardPlacementAbilities, IWindowAbilities
{
}
#pragma warning restore CS1591

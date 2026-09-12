using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

/// <summary>Nothing has an ability. What an engine with no cards ported does.</summary>
/// <remarks>
/// Open rather than sealed, and every member virtual, so that something which
/// does <i>one</i> thing can say only that. Tests want a card that answers a
/// window and nothing else far more often than they want the whole interface,
/// and nine copies of "return an empty list" is nine places for this interface
/// to grow through.
/// </remarks>
public class NoCardAbilities : ICardAbilities
{
    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> EntersPlay(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ActivationCompleted(
        World world, EnemyActivation result) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ResumeAbility(
        World world, PhaseStep continuation) => [];

    /// <inheritdoc/>
    public virtual bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1) => true;

    /// <inheritdoc/>
    public virtual string ResourcesGeneratedBy(World world, Card source, Card? payingFor) =>
        Resources.GeneratedBy(source.FaceId, world.Facts);

    /// <inheritdoc/>
    public virtual DefenderChoice Defenders(
        World world, EnemyAttack attack, IReadOnlyList<Card> candidates) =>
        new(candidates, Required: false);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> WhenRevealed(
        World world, Card card, int player, Occurrence occurrence) => WhenRevealed(world, card, player);

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> WhenRevealedAbilities(
        World world, Card card, int player) => [];

    /// <inheritdoc/>
    public virtual bool CancelWhenRevealed(
        World world, Card card, int player, Occurrence occurrence) => false;

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Boost(World world, Card card, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> WhenCardDefeated(
        World world, Card card, Defeated defeated) => [];

    /// <inheritdoc/>
    public virtual void ResolveCardAttack(
        World world, CharacterAttack attack, Timing.Occurrence occurrence,
        List<GameEvent> events) =>
        throw new RulesNotImplementedException("no card attack effect is registered");

    /// <inheritdoc/>
    public virtual void ResolveCardThwart(
        World world, CharacterThwart thwart, Timing.Occurrence occurrence,
        List<GameEvent> events) =>
        throw new RulesNotImplementedException("no card thwart effect is registered");

    /// <inheritdoc/>
    public virtual bool CanTakeDamage(World world, Card target, Card source) => true;

    /// <inheritdoc/>
    public virtual DamageProjection PreviewDamageReplacement(
        World world, Card target, Card source, long amount) => new(amount);

    /// <inheritdoc/>
    public virtual DefeatProjection? PreviewDefeatReplacement(
        World world, Card target, long maximumHealth) => null;

    /// <inheritdoc/>
    public virtual bool CanReady(World world, Card target, Card source) => true;

    /// <inheritdoc/>
    public virtual long WouldBeDealt(
        World world, Card target, Card source, long amount, List<GameEvent> events) => amount;

    /// <inheritdoc/>
    public virtual long WouldTake(
        World world, Card target, Card source, long amount, List<GameEvent> events) => amount;

    /// <inheritdoc/>
    public virtual void DamagePreventedByTough(
        World world, Card target, Card source, List<GameEvent> events)
    {
    }

    /// <inheritdoc/>
    public virtual void WouldBeDefeated(
        World world, Card target, List<GameEvent> events)
    {
    }

    /// <inheritdoc/>
    public virtual bool WouldBeDefeated(
        World world, Card target, Card source, string trigger, string verb, int by,
        List<GameEvent> events, Occurrence? recordDefeatOn = null)
    {
        WouldBeDefeated(world, target, events);
        return true;
    }

    /// <inheritdoc/>
    public virtual bool WhenCardDefeated(
        World world, Card card, Defeated defeated, string trigger, List<GameEvent> events)
    {
        events.AddRange(WhenCardDefeated(world, card, defeated));
        return true;
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<Prompts.ResourceSource> ResourceAbilities(
        World world, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<Prompts.ResourceSource> PrintedResourceAbilities(
        World world, int player) => [];

    /// <inheritdoc/>
    public virtual string ResourceGeneratorName(World world, int player, int card)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.Facts.Title(world.Cards[card].FaceId);
    }

    /// <inheritdoc/>
    public virtual string UseResource(
        World world, int player, int card, List<GameEvent> events) =>
        throw new RulesNotImplementedException(
            "no card has a resource ability, so none of them can be used");

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> Actions(World world, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null) =>
        throw new RulesNotImplementedException(
            "no card has an action, so none of them can be triggered");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying,
        IReadOnlyList<int> chosen, Occurrence occurrence,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null) =>
        Act(world, ability, paying, chosen, values, allocations);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ResolveSpecial(
        World world, Card card, int player, bool finalStep) =>
        throw new RulesNotImplementedException(
            $"card '{card.FaceId}' has no implemented Special ability");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ResolveEachPlayer(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep, bool finalPlayer) =>
        throw new RulesNotImplementedException(
            $"card '{source.FaceId}' has no implemented each-player continuation");

    /// <inheritdoc/>
    public virtual int? AttachesTo(World world, Card card) => null;

    /// <inheritdoc/>
    public virtual int? SetupController(World world, Card card) => null;

    /// <inheritdoc/>
    public virtual void ValidateForPlay(World world)
    {
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<int>? AttachmentTargets(World world, Card card) => null;

    /// <inheritdoc/>
    public virtual IReadOnlyList<Card> PlayerSetupCards(World world, int player) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Setup(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<Timing.ContinuousEffect> Constant(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier = null) => null;

    /// <inheritdoc/>
    public virtual Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep) =>
        Choosing(world, source, player, stoppedAt, tier);

    /// <inheritdoc/>
    public virtual Prompts.Prompt? Choosing(
        World world, Card source, int player, int stoppedAt,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        Choosing(world, source, player, stoppedAt, tier, finalStep);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier = null) =>
        throw new RulesNotImplementedException(
            "no card has an ability, so none of them is waiting on a choice");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep) =>
        Chose(world, source, player, stoppedAt, input, tier);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        Chose(world, source, player, stoppedAt, input, tier, finalStep);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        Timing.AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string trigger) =>
        Chose(world, source, player, stoppedAt, input, tier, finalStep, eachPlayerFrame, finalPlayer);

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability,
        IReadOnlyList<int> paying, IReadOnlyList<int> chosen) =>
        throw new RulesNotImplementedException(
            "nothing is waiting in any window, so nothing can be resolved from one");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability,
        IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        IReadOnlyList<ResourceAllocation>? allocations = null) =>
        Resolve(world, occurrence, ability, paying, chosen);

    /// <inheritdoc/>
    public virtual Prompts.Affordance Describe(World world, PendingAbility ability) =>
        throw new RulesNotImplementedException(
            "nothing is waiting in any window, so nothing can be described from one");
}

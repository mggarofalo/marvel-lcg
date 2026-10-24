using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

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
    public virtual bool RequiresAllyPlayHistory(World world) => false;

    /// <inheritdoc/>
    public virtual bool ObservesPlayerTurnBeginning(World world) => false;

    /// <inheritdoc/>
    public virtual bool CanPlayFromStorage(World world, Card card, int player) => false;

    /// <inheritdoc/>
    public virtual AbilityExecutionResult EntersPlay(World world, AbilityExecution entry, Card card) => world.Agenda.Executions.FinishBatch(entry, []);

    /// <inheritdoc/>
    public virtual AbilityExecutionResult ActivationCompleted(
        World world, AbilityExecution entry, EnemyActivation result) => world.Agenda.Executions.FinishBatch(entry, []);

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ResumeAbility(
        World world, PhaseStep continuation) => [];

    /// <inheritdoc/>
    public virtual bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1) => true;
    /// <inheritdoc/>
    public virtual bool CanThwartScheme(World world, Card scheme, int ignoredSource = -1) => CanRemoveThreat(world, scheme, ignoredSource);
    /// <inheritdoc/>
    public virtual Card? SchemeDestination(World world, Card enemy) => world.TheCardIn(DeckType.MainSchemesArea);

    /// <inheritdoc/>
    public virtual string ResourcesGeneratedBy(World world, Card source, Card? payingFor) =>
        EffectiveCards.HasProfile(source) ? string.Empty : Resources.GeneratedBy(source.FaceId, world.Facts);

    /// <inheritdoc/>
    public virtual DefenderChoice Defenders(
        World world, EnemyAttack attack, IReadOnlyList<Card> candidates) =>
        new(candidates, Required: false);

    /// <inheritdoc/>
    public virtual AbilityExecutionResult WhenRevealed(World world, AbilityExecution entry, Card card, int player) => world.Agenda.Executions.FinishBatch(entry, []);

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> WhenRevealedAbilities(
        World world, Card card, int player) => [];

    /// <inheritdoc/>
    public virtual WhenRevealedCanceled? CancelWhenRevealed(
        World world, Card card, int player, Occurrence occurrence) => null;

    /// <inheritdoc/>
    public virtual AbilityExecutionResult Boost(World world, AbilityExecution entry, Card card, int player) => world.Agenda.Executions.FinishBatch(entry, []);

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
    public virtual AbilityExecutionResult WouldBeDealt(
        World world, AbilityExecution entry, DamageReplacement replacement)
    {
        world.Agenda.Executions.RequireBatch(entry);
        if (!ReferenceEquals(entry, replacement.Execution))
            throw new InvalidOperationException("the entry does not own this replacement");
        return entry.Finish([]);
    }

    /// <inheritdoc/>
    public virtual long WouldTake(
        World world, Card target, Card source, long amount, List<GameEvent> events) => amount;

    /// <inheritdoc/>
    public virtual void DamagePreventedByTough(
        World world, Card target, Card source, List<GameEvent> events)
    {
    }

    /// <inheritdoc/>
    public virtual AbilityExecutionResult WouldBeDefeated(World world, AbilityExecution entry,
        Card target, DefeatCause cause) => world.Agenda.Executions.FinishBatch(entry, []);

    /// <inheritdoc/>
    public virtual AbilityExecutionResult WhenCardsDefeated(World world, AbilityExecution entry,
        IReadOnlyList<Defeated> defeated, string trigger) => world.Agenda.Executions.FinishBatch(entry, []);

    /// <inheritdoc/>
    public virtual AbilityExecutionResult WhenCardDefeated(World world, AbilityExecution entry,
        Card card, Defeated defeated, string trigger) => world.Agenda.Executions.FinishBatch(entry, []);

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
    public virtual ResourceExecutionResult UseResource(
        World world, AbilityExecution entry, int player, int card) =>
        throw new RulesNotImplementedException(
            "no card has a resource ability, so none of them can be used");

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> Actions(World world, int player) => [];

    /// <inheritdoc/>
    public virtual AbilityExecutionResult Act(
        World world, AbilityExecution invocation, Decision answer) =>
        throw new RulesNotImplementedException(
            "no card has an action, so none of them can be triggered");

    /// <inheritdoc/>
    public virtual AbilityExecutionResult ResolveSpecial(
        World world, AbilityExecution entry, Card card, int player, bool finalStep) =>
        throw new RulesNotImplementedException(
            $"card '{card.FaceId}' has no implemented Special ability");

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> ResolveEachPlayer(World world, PhaseStep continuation) =>
        throw new RulesNotImplementedException("no authored each-player ability is installed");

    /// <inheritdoc/>
    public virtual Prompts.Prompt? Choosing(World world, PhaseStep continuation) => null;

    /// <inheritdoc/>
    public virtual IReadOnlyList<GameEvent> Chose(World world, PhaseStep continuation, Decision input) =>
        throw new RulesNotImplementedException("no authored choice is installed");

    /// <inheritdoc/>
    public virtual int? AttachesTo(World world, Card card) => null;

    /// <inheritdoc/>
    public virtual int? SetupController(World world, Card card) => null;

    /// <inheritdoc/>
    public virtual void ValidateForPlay(World world) { }

    /// <inheritdoc/>
    public virtual IReadOnlyList<int>? AttachmentTargets(World world, Card card) => null;

    /// <inheritdoc/>
    public virtual IReadOnlyList<Card> PlayerSetupCards(World world, int player) => [];

    /// <inheritdoc/>
    public virtual AbilityExecutionResult Setup(World world, AbilityExecution entry, Card card) => world.Agenda.Executions.FinishBatch(entry, []);

    /// <inheritdoc/>
    public virtual IReadOnlyList<Timing.ContinuousEffect> Constant(World world, Card card) => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window) => [];

    /// <inheritdoc/>
    public virtual AbilityExecutionResult Resolve(
        World world, AbilityExecution invocation, Decision answer) =>
        throw new RulesNotImplementedException(
            "nothing is waiting in any window, so nothing can be resolved from one");

    /// <inheritdoc/>
    public virtual Prompts.Affordance Describe(World world, PendingAbility ability) =>
        throw new RulesNotImplementedException(
            "nothing is waiting in any window, so nothing can be described from one");

    /// <inheritdoc/>
    public virtual PersistentAbilityDescription DescribePersistent(Card card) => new([], false);

}

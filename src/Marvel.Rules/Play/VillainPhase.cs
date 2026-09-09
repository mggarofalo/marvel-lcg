using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>
public sealed record DefeatProjection(long? RemainingHealth, string Note);

/// <summary>
/// What a card does when it is revealed from the encounter deck.
/// </summary>
/// <remarks>
/// This is the public compatibility composition surface. Rules services take
/// their exact consumer ports; hosts may supply one implementation that
/// satisfies all of them.
/// </remarks>
public interface ICardAbilities : IWindowAbilities, ICardCounterPools,
    IEncounterCardAbilities, ICardDamageAbilities, IThreatCardAbilities,
    ICardPowerAbilities, IResourceCardAbilities, ICardContinuationAbilities,
    IActivationCompletionAbilities, ICardReadinessAbilities,
    ICardSetupAbilities, ICardPlacementAbilities, ICardConstantAbilities, ICardActionAbilities,
    IAttackCardAbilities, ICardPlayAbilities, IRevealCardAbilities
{
}

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

/// <summary>
/// The villain phase, step by step, as <c>rr:villain-phase</c> lists them.
/// </summary>
/// <remarks>
/// <para>
/// The steps are numbered here as the Rules Reference numbers them, so a
/// divergence can be argued against the published text rather than against this
/// file. What is implemented is what the recorded milestone game reaches; the
/// rest throws rather than silently doing nothing, because a villain phase that
/// quietly skipped minion activation would produce a plausible board that is
/// wrong.
/// </para>
/// <para>
/// <b>The order is the whole thing.</b> The boost card is drawn before the
/// encounter card and discarded before it, which is why the recorded discard
/// pile holds the boost card at index 0 and the encounter card at index 1. Draw
/// them the other way round and every subsequent card in the encounter deck
/// shifts.
/// </para>
/// </remarks>
public static class VillainPhase
{
    /// <summary>Schedule the villain phase's six steps.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:villain-phase</c> lists six, and they are six values here rather
    /// than the order of six method calls. That is not tidiness: a window may
    /// hold an ability somebody has to be asked about, and a phase that is a
    /// call has nowhere to stop. See <see cref="Agenda"/>.
    /// </para>
    /// <para>
    /// Steps 2 and 4 are headings rather than occurrences, so they open no
    /// windows of their own; what happens under them — one activation, one card
    /// revealed — is scheduled when they are reached.
    /// </para>
    /// </remarks>
    /// <param name="agenda">What the game still has to do.</param>
    /// <param name="round">Which round this is.</param>
    public static void Schedule(Agenda agenda, int round)
    {
        ArgumentNullException.ThrowIfNull(agenda);
        agenda.Add(new PhaseStep(Steps.PlaceThreat, round, 1));
        agenda.Add(new PhaseStep(Steps.EnemiesActivate, round, 2, Plan: true));
        agenda.Add(new PhaseStep(Steps.DealEncounterCards, round, 3));
        agenda.Add(new PhaseStep(Steps.RevealEncounterCards, round, 4, Plan: true));
        agenda.Add(new PhaseStep(Steps.PassFirstPlayerToken, round, 5));
        agenda.Add(new PhaseStep(Steps.EndVillainPhase, round, 6));
    }
}

/// <summary>Dispatches phase-neutral agenda operations to their rules procedures.</summary>
/// <remarks>
/// The engine chooses this dispatch boundary. The Rules Reference defines the
/// procedures and their order, but not the software component that routes a
/// scheduled operation to its owner. <see cref="Sequence"/> retains timing and
/// scheduling; this type applies or answers the current operation.
/// </remarks>
public static class AgendaProcedures
{
    /// <summary>Apply one agenda operation.</summary>
    /// <remarks>
    /// Returns a prompt when the step itself has something to ask, which one of
    /// them does: <c>rr:attack-enemy-activation.step.2</c> asks whether anybody
    /// defends. That is not a window — nobody is using an ability — so it is the
    /// step that stops, and the answer comes back to
    /// <see cref="Answer"/>.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">Which step.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>The question the step is waiting on, or null.</returns>
    /// <exception cref="RulesNotImplementedException">
    /// The board reached a rule this engine does not have — a minion engaged
    /// with a player, or an attack that would defeat its target.
    /// </exception>
    public static Prompt? Apply(
        World world, ICardFacts facts, ICardAbilities abilities,
        PhaseStep step, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(abilities);
        world.Abilities = abilities;
        return ApplyWithWorldAbilities(world, facts, step, events);
    }

    internal static Prompt? ApplyWithWorldAbilities(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        return step.Operation.Procedure switch
        {
            AgendaProcedureKind.Attack => AttackProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.Threat => ThreatProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.Reveal => RevealProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.Defeat => DefeatProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.PlayerAction =>
                PlayerActionProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.AbilityContinuation =>
                AbilityContinuationProcedure.Apply(world, step, events),
            AgendaProcedureKind.Activation => ActivationProcedure.Apply(world, facts, step),
            AgendaProcedureKind.PhaseTransition =>
                PhaseTransitionProcedure.Apply(world, facts, step, events),
            AgendaProcedureKind.Lifecycle => null,
            _ => throw new RulesNotImplementedException(
                $"the agenda has no procedure for '{step.What}'"),
        };
    }

    /// <summary>Give a step the answer it stopped for.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">The step that asked.</param>
    /// <param name="input">The player's answer.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Answer(
        World world, ICardFacts facts, ICardAbilities abilities, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(abilities);
        world.Abilities = abilities;
        AnswerWithWorldAbilities(world, facts, step, input, events);
    }

    internal static void AnswerWithWorldAbilities(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        switch (step.Operation.Procedure)
        {
            case AgendaProcedureKind.Attack:
                AttackProcedure.Answer(world, facts, step, input, events);
                break;
            case AgendaProcedureKind.Reveal:
                RevealProcedure.Answer(world, facts, step, input, events);
                break;
            case AgendaProcedureKind.Defeat:
                DefeatProcedure.Answer(world, facts, step, input, events);
                break;
            case AgendaProcedureKind.PlayerAction:
                PlayerActionProcedure.Answer(world, facts, step, input, events);
                break;
            case AgendaProcedureKind.AbilityContinuation:
                AbilityContinuationProcedure.Answer(world, step, input, events);
                break;
            case AgendaProcedureKind.Activation:
                ActivationProcedure.Answer(world, step, input);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"step '{step.What}' asked nothing and cannot take an answer");
        }
    }

}

internal static class PlayerLimitProcedure
{
    internal static Prompt ChooseAlly(World world, ICardFacts facts, int player)
    {
        var allies = ControlledAllies(world, player);
        long limit = StateFields.Modified(
            world, world.Seats[player].IdentityCard, "ally_limit", facts, world.Players);
        if (allies.Count <= limit)
        {
            throw new InvalidOperationException(
                $"player {player} no longer exceeds their ally limit");
        }

        return new Prompt(
            player,
            Question.Element,
            TimingPriority.Untimed,
            Steps.ChooseAllyForLimit,
            $"{world.Seats[player].Name} chooses an ally to discard",
            false,
            [.. allies.Select(ally => new Affordance(
                ally.ObjectId,
                "Discard",
                ally.ObjectId,
                player,
                facts.Title(ally.FaceId)))]);
    }

    internal static void DiscardAlly(
        World world, ICardFacts facts, int player, Decision input, List<GameEvent> events)
    {
        var ally = ControlledAllies(world, player)
            .FirstOrDefault(card => card.ObjectId == input.Affordance)
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered for the ally limit");
        Discard.Card(world, ally, Steps.ChooseAllyForLimit, events);

        long limit = StateFields.Modified(
            world, world.Seats[player].IdentityCard, "ally_limit", facts, world.Players);
        if (ControlledAllies(world, player).Count > limit)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.ChooseAllyForLimit,
                world.Agenda.Current?.Round ?? 0,
                0,
                Seat: player,
                Plan: true));
        }
    }

    private static List<Card> ControlledAllies(World world, int player) =>
    [
        .. world.Areas
            .Where(area => area.Type == DeckType.AlliesArea
                && area.PlayArea == PlayArea.Of(player))
            .SelectMany(area => area.Cards)
            .OrderBy(card => card.ObjectId),
    ];

    internal static Prompt ChooseRestricted(World world, ICardFacts facts, int player)
    {
        var restricted = RestrictedCards(world, facts, player);
        if (restricted.Count <= StateFields.RestrictedLimit)
        {
            throw new InvalidOperationException(
                $"player {player} no longer exceeds their restricted-card limit");
        }

        return new Prompt(
            player,
            Question.Element,
            TimingPriority.Untimed,
            Steps.ChooseRestrictedCard,
            $"{world.Seats[player].Name} chooses a restricted card to discard",
            false,
            [.. restricted.Select(card => new Affordance(
                card.ObjectId,
                "Discard",
                card.ObjectId,
                player,
                facts.Title(card.FaceId)))]);
    }

    internal static void DiscardRestricted(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        var offered = step.ProcedureCandidates ?? [];
        var card = RestrictedCards(world, facts, step.Seat)
            .FirstOrDefault(candidate => candidate.ObjectId == input.Affordance
                && offered.Contains(candidate.ObjectId))
            ?? throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered for the restricted-card limit");

        Discard.Card(world, card, Steps.ChooseRestrictedCard, events);

        var remaining = RestrictedCards(world, facts, step.Seat);
        if (remaining.Count > StateFields.RestrictedLimit)
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                ProcedureCandidates = [.. remaining.Select(candidate => candidate.ObjectId)],
                OccurrenceId = null,
            });
        }
    }

    private static List<Card> RestrictedCards(World world, ICardFacts facts, int player) =>
    [
        .. world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => card.Owner == player
                && StateFields.Modified(
                    world, card, "restricted", facts, world.Players) > 0)
            .OrderBy(card => card.ObjectId),
    ];

}

internal static class DefeatProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.FinalizeCharacterDefeat:
                FinalizeCharacter(world, facts, step, events);
                break;
            case Steps.FinalizeSchemeDefeat:
                FinalizeScheme(world, facts, step, events);
                break;
            case Steps.ChooseWouldBeDefeated:
                return ChooseWouldBeDefeated(world, world.WindowAbilities, step);
            case Steps.ResumeWouldBeDefeated:
                ResumeWouldBeDefeated(world, facts, world.WindowAbilities, step, events);
                break;
            case Steps.ChooseCardDefeatedAbility:
                return ChooseCardDefeatedAbility(world, world.WindowAbilities, step);
            case Steps.ResumeCardDefeatedAbility:
                ResumeCardDefeatedAbility(world, facts, world.WindowAbilities, step, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the defeat procedure has no step '{step.What}'");
        }
        return null;
    }

    internal static void Answer(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ChooseWouldBeDefeated:
                ResolveWouldBeDefeated(
                    world, facts, world.WindowAbilities, step, input, events);
                break;
            case Steps.ChooseCardDefeatedAbility:
                ResolveCardDefeatedAbility(
                    world, facts, world.WindowAbilities, step, input, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"defeat step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    internal static void ScheduleProcedureChoice(World world, PhaseStep step)
    {
        if (world.Agenda.Occurrence is { } occurrence)
        {
            world.Agenda.ThenContinuation(step, occurrence);
            return;
        }

        world.Agenda.Add(step with { Plan = true });
    }

    internal static Prompt ChooseAttachmentTarget(
        World world, ICardFacts facts, PhaseStep step)
    {
        var card = world.Cards[step.Subject];
        var candidates = step.ProcedureCandidates ?? [];
        return new Prompt(
            world.FirstPlayer,
            Question.Element,
            TimingPriority.Untimed,
            Steps.ChooseAttachmentTarget,
            $"{world.Seats[world.FirstPlayer].Name} chooses where {card.FaceId} attaches",
            false,
            [.. candidates.Select(id => new Affordance(
                id,
                "Attach",
                id,
                world.Cards[id].Area.PlayArea.IsPlayers
                    ? world.Cards[id].Area.PlayArea.Player
                    : -1,
                facts.Title(world.Cards[id].FaceId)))]);
    }

    internal static Prompt ChooseWouldBeDefeated(
        World world, IWindowAbilities abilities, PhaseStep step)
    {
        var pending = step.ProcedureAbilities ?? [];
        if (pending.Count == 0)
        {
            throw new InvalidOperationException("damage step 6 has no pending abilities");
        }

        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));
        int player = ProcedurePlayer(world, step, pending, mandatory);
        var offered = mandatory
            ? pending
            : pending.Where(ability => ability.Player < 0 || ability.Player == player).ToList();
        return new Prompt(
            player,
            mandatory ? Question.Order : Question.Opportunity,
            AbilityTypes.PriorityOf(pending[0].Type),
            Steps.CardWouldBeDefeated,
            mandatory
                ? $"order abilities before card {step.Subject} is defeated"
                : $"interrupt before card {step.Subject} is defeated",
            !mandatory,
            [.. offered.Select(ability => abilities.Describe(world, ability))]);
    }

    internal static void ResolveWouldBeDefeated(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        var procedure = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("damage step 6 has no occurrence");
        var pending = step.ProcedureAbilities ?? [];
        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));

        if (input.IsDecline)
        {
            if (mandatory)
            {
                throw new RulesNotImplementedException(
                    "a forced damage-step ability ordering cannot be declined");
            }
            int player = ProcedurePlayer(world, step, pending, mandatory: false);
            var passed = (step.ProcedurePlayersPassed ?? []).Append(player).Distinct().ToList();
            if (HasOptionalProcedurePlayer(world, pending, passed))
            {
                ScheduleProcedureChoice(world, step with
                {
                    ProcedurePlayersPassed = passed,
                    OccurrenceId = null,
                });
            }
            else
            {
                FinishWouldBeDefeated(world, facts, step, events);
            }
            return;
        }

        int resolvingPlayer = ProcedurePlayer(world, step, pending, mandatory);
        var offered = mandatory
            ? pending
            : pending.Where(ability =>
                ability.Player < 0 || ability.Player == resolvingPlayer).ToList();
        var chosen = offered
            .Select(ability => (Ability: ability, Offer: abilities.Describe(world, ability)))
            .Where(pair => pair.Offer.Id == input.Affordance)
            .Select(pair => (PendingAbility?)pair.Ability)
            .FirstOrDefault()
            ?? throw new RulesNotImplementedException(
                $"affordance {input.Affordance} was not offered at damage step 6");

        procedure.Trigger(WindowKind.Interrupt, chosen.Card);
        var containingOccurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("damage step 6 has no containing occurrence");
        if (!ReferenceEquals(containingOccurrence, procedure))
        {
            containingOccurrence.Trigger(WindowKind.Interrupt, chosen.Card);
        }
        events.AddRange(abilities.Resolve(
            world, procedure, chosen, input.Spent, input.Targets,
            input.DefinedValues, input.Allocated));
        world.Agenda.ThenContinuation(step with
        {
            What = Steps.ResumeWouldBeDefeated,
            ProcedureAbilities = [.. pending.Where(ability => ability != chosen)],
            ProcedurePlayersPassed = [],
            Tier = chosen.Type,
            OccurrenceId = null,
        }, containingOccurrence);
    }

    internal static void ResumeWouldBeDefeated(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var target = world.Cards[step.Subject];
        if (Damage.Health(world, facts, target) - target.Damage > 0)
        {
            world.Effects.CompleteHealthDefeat(target);
            return;
        }

        ContinueWouldBeDefeated(world, facts, abilities, step, events);
    }

    private static void ContinueWouldBeDefeated(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var procedure = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("damage step 6 has no occurrence");
        var carried = (step.ProcedureAbilities ?? []).ToList();
        if (carried.Count == 1 && AbilityTypes.IsMandatory(carried[0].Type))
        {
            var next = carried[0];
            var containing = world.Agenda.Occurrence
                ?? throw new InvalidOperationException(
                    "damage step 6 resume has no containing occurrence");
            procedure.Trigger(WindowKind.Interrupt, next.Card);
            if (!ReferenceEquals(containing, procedure))
            {
                containing.Trigger(WindowKind.Interrupt, next.Card);
            }
            events.AddRange(abilities.Resolve(world, procedure, next, [], []));
            world.Agenda.ThenContinuation(step with
            {
                ProcedureAbilities = [],
                ProcedurePlayersPassed = [],
                Tier = next.Type,
                OccurrenceId = null,
            }, containing);
            return;
        }
        var pending = carried.Count > 0
            ? carried
            : FirstInterruptTier(abilities, world, procedure, after: step.Tier);
        if (pending.Count > 0)
        {
            ScheduleProcedureChoice(world, step with
            {
                What = Steps.ChooseWouldBeDefeated,
                ProcedureAbilities = pending,
                ProcedurePlayersPassed = [],
                OccurrenceId = null,
            });
            return;
        }

        FinishWouldBeDefeated(world, facts, step, events);
    }

    private static void FinishWouldBeDefeated(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var target = world.Cards[step.Subject];
        world.Effects.CompleteHealthDefeat(target);
        if (DeckTypes.IsInPlay(target.Area.Type)
            && Damage.Health(world, facts, target) - target.Damage <= 0)
        {
            Defeat.Character(
                world, facts, target, step.ProcedureTrigger, events,
                how: step.ProcedureVerb, by: step.ProcedureBy,
                recordOn: step.ProcedureOwnerOccurrence);
        }
    }

    internal static Prompt ChooseCardDefeatedAbility(
        World world, IWindowAbilities abilities, PhaseStep step)
    {
        var pending = step.ProcedureAbilities ?? [];
        if (pending.Count == 0)
        {
            throw new InvalidOperationException("damage step 7 has no pending abilities");
        }

        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));
        int player = ProcedurePlayer(world, step, pending, mandatory);
        var offered = mandatory
            ? pending
            : pending.Where(ability => ability.Player < 0 || ability.Player == player).ToList();
        return new Prompt(
            player,
            mandatory ? Question.Order : Question.Opportunity,
            AbilityTypes.PriorityOf(pending[0].Type),
            Steps.CardDefeated,
            mandatory
                ? $"order abilities when card {step.Subject} is defeated"
                : $"interrupt when card {step.Subject} is defeated",
            !mandatory,
            [.. offered.Select(ability => abilities.Describe(world, ability))]);
    }

    internal static void ResolveCardDefeatedAbility(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        var occurrence = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("damage step 7 has no occurrence");
        var pending = step.ProcedureAbilities ?? [];
        bool mandatory = pending.All(ability => AbilityTypes.IsMandatory(ability.Type));

        if (input.IsDecline)
        {
            if (mandatory)
            {
                throw new RulesNotImplementedException(
                    "a forced damage-step ability ordering cannot be declined");
            }
            int player = ProcedurePlayer(world, step, pending, mandatory: false);
            var passed = (step.ProcedurePlayersPassed ?? []).Append(player).Distinct().ToList();
            if (HasOptionalProcedurePlayer(world, pending, passed))
            {
                ScheduleProcedureChoice(world, step with
                {
                    ProcedurePlayersPassed = passed,
                    OccurrenceId = null,
                });
            }
            else
            {
                FinalizeCardDefeat(world, facts, step, events);
            }
            return;
        }

        int resolvingPlayer = ProcedurePlayer(world, step, pending, mandatory);
        var offered = mandatory
            ? pending
            : pending.Where(ability =>
                ability.Player < 0 || ability.Player == resolvingPlayer).ToList();
        var chosen = offered
            .Select(ability => (Ability: ability, Offer: abilities.Describe(world, ability)))
            .Where(pair => pair.Offer.Id == input.Affordance)
            .Select(pair => (PendingAbility?)pair.Ability)
            .FirstOrDefault()
            ?? throw new RulesNotImplementedException(
                $"affordance {input.Affordance} was not offered at damage step 7");

        occurrence.Trigger(WindowKind.Interrupt, chosen.Card);
        var containingOccurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("damage step 7 has no containing occurrence");
        if (!ReferenceEquals(containingOccurrence, occurrence))
        {
            containingOccurrence.Trigger(WindowKind.Interrupt, chosen.Card);
        }
        events.AddRange(abilities.Resolve(
            world, occurrence, chosen, input.Spent, input.Targets,
            input.DefinedValues, input.Allocated));
        world.Agenda.ThenContinuation(step with
        {
            What = Steps.ResumeCardDefeatedAbility,
            ProcedureAbilities = [.. pending.Where(ability => ability != chosen)],
            ProcedurePlayersPassed = [],
            Tier = chosen.Type,
            OccurrenceId = null,
        }, containingOccurrence);
    }

    internal static void ResumeCardDefeatedAbility(
        World world, ICardFacts facts, IWindowAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var occurrence = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("damage step 7 has no occurrence");
        var carried = (step.ProcedureAbilities ?? []).ToList();
        if (carried.Count == 1 && AbilityTypes.IsMandatory(carried[0].Type))
        {
            var next = carried[0];
            var containing = world.Agenda.Occurrence
                ?? throw new InvalidOperationException(
                    "damage step 7 resume has no containing occurrence");
            occurrence.Trigger(WindowKind.Interrupt, next.Card);
            if (!ReferenceEquals(containing, occurrence))
            {
                containing.Trigger(WindowKind.Interrupt, next.Card);
            }
            events.AddRange(abilities.Resolve(world, occurrence, next, [], []));
            world.Agenda.ThenContinuation(step with
            {
                ProcedureAbilities = [],
                ProcedurePlayersPassed = [],
                Tier = next.Type,
                OccurrenceId = null,
            }, containing);
            return;
        }
        var pending = carried.Count > 0
            ? carried
            : FirstInterruptTier(
                abilities, world, occurrence, excludeCard: step.Subject,
                after: step.Tier);
        if (pending.Count > 0)
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                What = Steps.ChooseCardDefeatedAbility,
                ProcedureAbilities = pending,
                ProcedurePlayersPassed = [],
                OccurrenceId = null,
            });
            return;
        }

        FinalizeCardDefeat(world, facts, step, events);
    }

    private static List<PendingAbility> FirstInterruptTier(
        IWindowAbilities abilities, World world, Occurrence occurrence,
        int excludeCard = -1, AbilityType? after = null)
    {
        var tiers = AbilityWindow.Tiers(
            abilities.Waiting(world, occurrence, WindowKind.Interrupt)
                .Where(ability => ability.Card != excludeCard),
            WindowKind.Interrupt,
            occurrence);
        var tier = tiers.FirstOrDefault(candidate => after is null
            || candidate.Priority > AbilityTypes.PriorityOf(after.Value));
        if (tier.Abilities is null)
        {
            return [];
        }
        var (forced, optional) = AbilityWindow.Split(tier);
        return forced.Count > 0 ? [.. forced] : [.. optional];
    }

    private static int ProcedurePlayer(
        World world, PhaseStep step, IReadOnlyList<PendingAbility> pending,
        bool mandatory)
    {
        if (mandatory)
        {
            return world.FirstPlayer;
        }
        var passed = (step.ProcedurePlayersPassed ?? []).ToHashSet();
        return world.PlayerOrder.FirstOrDefault(player =>
            !passed.Contains(player)
            && pending.Any(ability => ability.Player < 0 || ability.Player == player),
            step.Seat >= 0 ? step.Seat : world.FirstPlayer);
    }

    private static bool HasOptionalProcedurePlayer(
        World world, IReadOnlyList<PendingAbility> pending, IReadOnlyList<int> passed)
    {
        var skipped = passed.ToHashSet();
        return world.PlayerOrder.Any(player =>
            !skipped.Contains(player)
            && pending.Any(ability => ability.Player < 0 || ability.Player == player));
    }

    private static void FinalizeCardDefeat(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        if (facts.Kind(card.FaceId) == CardKind.EncounterSideScheme)
        {
            Defeat.FinalizeScheme(world, facts, card, step.ProcedureTrigger, events);
        }
        else
        {
            Defeat.FinalizeCharacter(world, facts, card, step.ProcedureTrigger, events);
        }
    }

    internal static void FinalizeCharacter(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        Defeat.FinalizeCharacter(
            world, facts, world.Cards[step.Subject], step.Trigger, events);

    internal static void FinalizeScheme(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        Defeat.FinalizeScheme(
            world, facts, world.Cards[step.Subject], step.Trigger, events);
}

internal static partial class RevealProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.DealEncounterCards:
                DealEncounterCards(world, facts, events);
                break;
            case Steps.RevealEncounterCards:
                RevealNextEncounterCard(world, step);
                break;
            case Steps.RevealEncounterCard:
                RevealEncounterCard(
                    world, facts, world.RevealAbilities, world.Cards[step.Subject],
                    step.Seat, step.Round, events);
                break;
            case Steps.DiscardRevealedTreachery:
                DiscardRevealedTreachery(world, facts, step, events);
                break;
            case Steps.ChooseAttachmentTarget:
                return DefeatProcedure.ChooseAttachmentTarget(world, facts, step);
            case Steps.ChooseRevealAbility:
                return ChooseRevealAbility(world, facts, step);
            case Steps.ResumeRevealAbility:
                ResumeRevealAbility(world, facts, world.RevealAbilities, step, events);
                break;
            case Steps.ChoosePostRevealAbility:
                return ChoosePostRevealAbility(world, step);
            case Steps.FinalizeAllyEntry:
                FinalizeAllyEntry(world, facts, step.Subject, step.Seat, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the reveal procedure has no step '{step.What}'");
        }
        return null;
    }

    internal static void Answer(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.ChooseAttachmentTarget:
                AttachRevealedCard(
                    world, facts, world.RevealAbilities, step, input, events);
                break;
            case Steps.ChooseRevealAbility:
                ResolveRevealAbility(
                    world, facts, world.RevealAbilities, step, input, events);
                break;
            case Steps.ChoosePostRevealAbility:
                ResolvePostRevealAbility(world, facts, step, input, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"reveal step '{step.What}' asked nothing and cannot take an answer");
        }
    }

    internal static Prompt ChooseRevealAbility(
        World world, ICardFacts facts, PhaseStep step)
    {
        var card = world.Cards[step.Subject];
        var handles = step.ProcedureCandidates ?? [];
        var abilities = step.ProcedureAbilities ?? [];
        if (handles.Count != abilities.Count || handles.Count < 2)
        {
            throw new InvalidOperationException("reveal ordering has no simultaneous abilities");
        }

        return new Prompt(
            world.FirstPlayer,
            Question.Order,
            TimingPriority.Occurrence,
            Steps.CardRevealed,
            $"order When Revealed abilities on {card.FaceId}",
            false,
            [.. handles.Select((handle, index) => new Affordance(
                handle,
                "Resolve",
                card.ObjectId,
                world.FirstPlayer,
                RevealAbilityLabel(facts, card, abilities[index])))]);
    }

    private static string RevealAbilityLabel(
        ICardFacts facts, Card card, PendingAbility ability) => ability.Ordinal switch
    {
        Reveal.InciteResolutionOrdinal => "Incite",
        Reveal.SurgeResolutionOrdinal => "Surge",
        _ => $"{facts.Title(card.FaceId)} When Revealed {ability.Ordinal + 1}",
    };

    internal static void ResolveRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        var pending = step.ProcedureAbilities ?? [];
        var handles = step.ProcedureCandidates ?? [];
        int chosenIndex = Enumerable.Range(0, handles.Count)
            .FirstOrDefault(index => handles[index] == input.Affordance, -1);
        if (input.IsDecline || chosenIndex < 0 || chosenIndex >= pending.Count)
        {
            throw new RulesNotImplementedException(
                $"affordance {input.Affordance} was not offered for reveal ordering");
        }

        var occurrence = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("reveal ordering has no occurrence");
        var containingOccurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("reveal ordering has no containing occurrence");
        ResolveOneRevealAbility(
            world, facts, abilities, world.Cards[step.Subject], step.Seat,
            pending[chosenIndex], occurrence, events);

        var remainingAbilities = pending.Where((_, index) => index != chosenIndex).ToList();
        var remainingHandles = handles.Where((_, index) => index != chosenIndex).ToList();
        var chosen = pending[chosenIndex];
        world.Agenda.ThenContinuation(step with
        {
            What = Steps.ResumeRevealAbility,
            ProcedureAbilities = remainingAbilities,
            ProcedureCandidates = remainingHandles,
            ProcedureSource = chosen.Card,
            Tier = chosen.Type,
            AbilityOrdinal = chosen.Ordinal,
            OccurrenceId = null,
        }, containingOccurrence);
    }

    internal static void ResumeRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        var occurrence = step.ProcedureOccurrence
            ?? throw new InvalidOperationException("reveal ordering has no occurrence");
        if (step.ProcedureSource >= 0
            && step.AbilityOrdinal >= 0
            && step.Tier is { } completedType)
        {
            occurrence.Complete(new PendingAbility(
                step.ProcedureSource, completedType, step.Seat, step.AbilityOrdinal));
        }

        var pending = step.ProcedureAbilities ?? [];
        var handles = step.ProcedureCandidates ?? [];
        if (pending.Count > 1)
        {
            DefeatProcedure.ScheduleProcedureChoice(world, step with
            {
                What = Steps.ChooseRevealAbility,
                ProcedureSource = -1,
                Tier = null,
                AbilityOrdinal = -1,
                OccurrenceId = null,
            });
            return;
        }

        if (pending.Count == 1)
        {
            var next = pending[0];
            var containingOccurrence = world.Agenda.Occurrence
                ?? throw new InvalidOperationException(
                    "reveal continuation has no containing occurrence");
            ResolveOneRevealAbility(
                world, facts, abilities, world.Cards[step.Subject], step.Seat,
                next, occurrence, events);
            world.Agenda.ThenContinuation(step with
            {
                What = Steps.ResumeRevealAbility,
                ProcedureAbilities = [],
                ProcedureCandidates = [],
                ProcedureSource = next.Card,
                Tier = next.Type,
                AbilityOrdinal = next.Ordinal,
                OccurrenceId = null,
            }, containingOccurrence);
            return;
        }

        FinishEncounterRevealTail(
            world, facts, world.Cards[step.Subject], step.Seat, step.Round,
            occurrence, events, beforeResponses: false);
    }

    private static void ResolveOneRevealAbility(
        World world, ICardFacts facts, IRevealCardAbilities abilities, Card card, int player,
        PendingAbility ability, Occurrence occurrence, List<GameEvent> events)
    {
        if (ability.Ordinal is Reveal.InciteResolutionOrdinal or Reveal.SurgeResolutionOrdinal)
        {
            Reveal.ResolveKeyword(
                world, facts, abilities, card, player, ability, events, occurrence);
            return;
        }

        events.AddRange(abilities.Resolve(world, occurrence, ability, [], []));
    }

    internal static Prompt ChoosePostRevealAbility(World world, PhaseStep step)
    {
        var card = world.Cards[step.Subject];
        return new Prompt(
            world.FirstPlayer,
            Question.Order,
            TimingPriority.ForcedResponse,
            Steps.CardRevealed,
            $"order responses after {card.FaceId} is revealed",
            false,
            [
                new Affordance(1, "Resolve", card.ObjectId, world.FirstPlayer, "Quickstrike"),
                new Affordance(2, "Resolve", card.ObjectId, world.FirstPlayer, "Teamwork"),
            ]);
    }

    internal static void ResolvePostRevealAbility(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        if (input.IsDecline || input.Affordance is not (1 or 2))
        {
            throw new RulesNotImplementedException(
                $"affordance {input.Affordance} was not offered after reveal");
        }

        var card = world.Cards[step.Subject];
        if (input.Affordance == 1)
        {
            Reveal.Quickstrike(world, facts, card, step.Seat, step.Round);
            Reveal.Teamwork(world, facts, card, step.Seat, step.Round);
        }
        else
        {
            Reveal.Teamwork(world, facts, card, step.Seat, step.Round);
            Reveal.Quickstrike(world, facts, card, step.Seat, step.Round);
        }

        FinishEncounterRevealDiscard(
            world, facts, card, step.Seat, step.Round,
            step.ProcedureOccurrence
                ?? throw new InvalidOperationException("post-reveal order has no occurrence"));
    }

    internal static void AttachRevealedCard(
        World world, ICardFacts facts, IRevealCardAbilities abilities, PhaseStep step,
        Decision input, List<GameEvent> events)
    {
        var candidates = step.ProcedureCandidates ?? [];
        if (input.IsDecline || !candidates.Contains(input.Affordance))
        {
            throw new RulesNotImplementedException(
                $"card {input.Affordance} was not offered as an attachment target");
        }

        var card = world.Cards[step.Subject];
        var occurrence = step.AbilityOccurrence
            ?? world.Agenda.Occurrence
            ?? throw new InvalidOperationException("an attachment choice has no reveal occurrence");
        Reveal.Resolve(
            world, facts, card, step.Seat, events, occurrence, input.Affordance);
        FinishEncounterReveal(
            world, facts, abilities, card, step.Seat, step.Round, occurrence, events);
    }

    internal static void FinalizeAllyEntry(
        World world, ICardFacts facts, int allyId, int player,
        List<GameEvent> events)
    {
        var ally = world.Cards[allyId];
        if (ally.Area.Type == DeckType.AlliesArea)
        {
            Reveal.EnterPlay(world, facts, ally, events, abilities: world.CardPlayAbilities);
            CardPlay.Entered(world, ally, player);
        }
    }

}

internal static class ActivationProcedure
{
    internal static Prompt? Apply(World world, ICardFacts facts, PhaseStep step) =>
        step.What == Steps.EnemiesActivate
            ? Plan(world, facts, step)
            : throw new RulesNotImplementedException(
                $"the activation procedure has no step '{step.What}'");

    internal static void Answer(World world, PhaseStep step, Decision input)
    {
        if (step.What != Steps.EnemiesActivate)
        {
            throw new RulesNotImplementedException(
                $"activation step '{step.What}' asked nothing and cannot take an answer");
        }
        Order(world, step, input);
    }

    /// <summary>
    /// Step 2, one enemy at a time — <c>rr:villain-phase.step.2</c>, "in player
    /// order, each player resolves".
    /// </summary>
    internal static Prompt? Plan(World world, ICardFacts facts, PhaseStep step)
    {
        var playerOrder = step.ActivationPlayers ?? world.PlayerOrder.ToList();
        if (step.Index >= playerOrder.Count)
        {
            return null;
        }

        var villain = world.TheCardIn(DeckType.VillainArea);
        if (villain is null)
        {
            return null;
        }

        int seat = playerOrder[step.Index];
        var activated = step.ActivatedEnemies ?? [];

        // An eliminated seat remains in this procedure's stable order so its
        // removal cannot shift the next player under the current index. It has
        // no enemies left to activate; advance and clear the per-player set.
        if (world.Seats[seat].Eliminated)
        {
            world.Agenda.Then(step with
            {
                Index = step.Index + 1,
                ActivatedEnemies = [],
                ActivationPlayers = playerOrder,
                OccurrenceId = null,
            });
            return null;
        }

        // `rr:activation.1`: hero form and the enemy attacks, alter-ego form
        // and it schemes. Read the form immediately before each activation:
        // an earlier activation can change it.
        var identity = world.Seats[seat].IdentityCard;
        bool attacking = facts.Kind(identity.FaceId) != CardKind.AlterEgo;

        var remaining = world
            .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat))
            .Cards
            .Where(minion => !activated.Contains(minion.ObjectId))
            .OrderBy(minion => minion.ObjectId)
            .ToList();

        bool chosenOrderHasCandidate = step.ActivationOrder?.Any(candidate =>
            !activated.Contains(candidate)
            && remaining.Any(minion => minion.ObjectId == candidate)) == true;
        if (activated.Contains(villain.ObjectId)
            && !chosenOrderHasCandidate
            && remaining.Count > 1)
        {
            var ids = remaining.Select(minion => minion.ObjectId).ToList();
            return new Prompt(
                seat,
                Question.Order,
                TimingPriority.Untimed,
                Steps.EnemiesActivate,
                $"{world.Seats[seat].Name} orders engaged minion activations",
                false,
                [new Affordance(
                    villain.ObjectId,
                    "Order",
                    villain.ObjectId,
                    seat,
                    "engaged minions",
                    new TargetRequest(ids, ids.Count, ids.Count, Rule: "rr:minion.3"))]);
        }

        int? enemy = !activated.Contains(villain.ObjectId)
            ? villain.ObjectId
            : step.ActivationOrder?
                .Where(candidate => !activated.Contains(candidate)
                    && remaining.Any(minion => minion.ObjectId == candidate))
                .Select(candidate => (int?)candidate)
                .FirstOrDefault();

        enemy ??= remaining
            .Select(minion => (int?)minion.ObjectId)
            .FirstOrDefault();

        if (enemy is { } next)
        {
            world.Agenda.Then(new PhaseStep(
                attacking ? Steps.Attack : Steps.Scheme,
                step.Round, 2, Index: seat, Subject: next, Seat: seat));
            world.Agenda.Then(step with
            {
                ActivatedEnemies = [.. activated, next],
                ActivationPlayers = playerOrder,
                ActivationOrder = step.ActivationOrder,
                OccurrenceId = null,
            });
            return null;
        }

        // `rr:minion.4`: a minion that becomes engaged while engaged minions
        // are activating joins this procedure. The continuation above therefore
        // re-reads the area only after the preceding activation has completely
        // resolved. The list prevents a surviving minion from being chosen
        // again. A newly engaged group is ordered when this chosen order has
        // been exhausted.
        world.Agenda.Then(step with
        {
            Index = step.Index + 1,
            ActivatedEnemies = [],
            ActivationPlayers = playerOrder,
            ActivationOrder = null,
            OccurrenceId = null,
        });
        return null;
    }

    internal static void Order(World world, PhaseStep step, Decision input)
    {
        var playerOrder = step.ActivationPlayers ?? world.PlayerOrder.ToList();
        if (step.Index < 0 || step.Index >= playerOrder.Count)
        {
            throw new RulesNotImplementedException(
                "the minion-order continuation has no engaged player");
        }
        int seat = playerOrder[step.Index];
        var villain = world.TheCardIn(DeckType.VillainArea)
            ?? throw new RulesNotImplementedException(
                "minion activations cannot be ordered without a villain");
        var activated = step.ActivatedEnemies ?? [];
        var candidates = world
            .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat))
            .Cards
            .Where(minion => !activated.Contains(minion.ObjectId))
            .Select(minion => minion.ObjectId)
            .OrderBy(id => id)
            .ToList();
        var request = new TargetRequest(
            candidates, candidates.Count, candidates.Count, Rule: "rr:minion.3");

        if (input.IsDecline
            || input.Affordance != villain.ObjectId
            || !request.Allows(input.Targets))
        {
            throw new RulesNotImplementedException(
                $"player {seat} must order every engaged minion activation; "
                + $"offered [{string.Join(',', candidates)}], chose "
                + $"[{string.Join(',', input.Targets)}], affordance "
                + $"{input.Affordance} (expected {villain.ObjectId})");
        }

        world.Agenda.Then(step with
        {
            ActivationOrder = [.. input.Targets],
            ProcedureCandidates = [.. candidates],
            OccurrenceId = null,
        });
    }

}

internal static class ThreatProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.PlaceThreat:
                PlaceThreat(world, facts, world.ThreatAbilities, events);
                break;
            case Steps.PlaceThreatEffect:
                ApplyThreat(world, facts, world.ThreatAbilities, events);
                break;
            case Steps.Scheme:
                Scheme(world, facts, step, events);
                break;
            case Steps.SchemeThreat:
                SchemeThreat(world, facts, world.ThreatAbilities, step, events);
                break;
            case Steps.EndSchemeEarly:
                EndSchemeEarly(world, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the threat procedure has no step '{step.What}'");
        }
        return null;
    }

    /// <summary>Step 1. Threat from the main scheme's acceleration field.</summary>
    /// <remarks>
    /// <c>rr:villain-phase.1</c>: "Place the amount of threat indicated in the
    /// main scheme's acceleration field onto that scheme." The engine's name for
    /// that field is <c>EscalationThreat</c>, and it is per-player —
    /// <c>1*</c> on <c>01097b</c>, so one threat at one player and three at
    /// three. Acceleration icons and tokens add more; nothing on the milestone
    /// board has one.
    /// </remarks>
    internal static void PlaceThreat(
        World world, ICardFacts facts, IThreatCardAbilities abilities, List<GameEvent> events)
    {
        if (world.Agenda.Occurrence is { } occurrence)
        {
            long placed = Threat.Apply(world, facts, abilities, occurrence, events);
            // Assault on NORAD says "After placing threat here during step
            // one". A fully prevented assignment did not place threat, so it
            // does not create that response condition (FAQ 01138).
            if (placed > 0)
            {
                occurrence.Also(Steps.VillainPhaseStepOneEnds);
            }
        }
    }

    internal static void ApplyThreat(
        World world, ICardFacts facts, IThreatCardAbilities abilities, List<GameEvent> events)
    {
        var step = world.Agenda.Current
            ?? throw new InvalidOperationException("a threat step has no agenda item");
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a threat step has no occurrence");
        long placed = Threat.Apply(world, facts, abilities, occurrence, events);
        if (step.AbilityOccurrence is { } abilityOccurrence
            && step.Tier is { } tier
            && step.AbilityOrdinal >= 0
            && step.Placement is { Source: >= 0 } placement)
        {
            var ability = new PendingAbility(
                placement.Source, tier, step.Seat, step.AbilityOrdinal);
            if (placed > 0)
            {
                abilityOccurrence.Resolve(ability);
            }
            abilityOccurrence.Complete(ability);
        }
    }

    /// <summary>An enemy schemes. <c>rr:scheme-enemy-activation</c>.</summary>
    /// <remarks>
    /// Three steps: give it one facedown boost card from the encounter deck,
    /// resolve that card (flip, add its boost icons to SCH, discard), then place
    /// threat equal to the modified SCH on the main scheme.
    /// </remarks>
    internal static void Scheme(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        Card villain = world.Cards[step.Subject];
        int seat = step.Seat;
        // `rr:activation.6`: "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve." A scheme is an activation -- `rr:activation`
        // says so -- and a minion can be defeated between being scheduled to
        // scheme and getting to. `rr:in-play-and-out-of-play.2` is what in play
        // means for an encounter card.
        if (!DeckTypes.IsInPlay(villain.Area.Type))
        {
            return;
        }

        // `rr:confuse-confused.1`: "when this character would scheme or thwart,
        // remove each confused status card from it instead." The scheme does
        // not happen, so no boost card is given and no threat is placed.
        if (BasicPowers.Cancelled(world, facts, villain, Statuses.Confused, events))
        {
            return;
        }

        // `rr:activation` -- the other kind, and the one that had no value on
        // the board until now. Set after `rr:stun-stunned`'s cancellation
        // above, because a cancelled activation is not one.
        world.Activation = new EnemyActivation(
            villain.ObjectId, seat, Attacking: false, Id: world.Agenda.Current?.ActivationId ?? -1);

        // **A scheming enemy holds boost cards, plural.**
        // `rr:scheme-enemy-activation.step.1` gives the card to the enemy --
        // "give **it** one facedown boost card" -- and step 2 resolves "each of
        // the scheming enemy's boost cards, one at a time and in the order in
        // which they were dealt", ending at `.step.2.e`: "if the enemy has any
        // boost cards remaining, repeat these steps with the next boost card."
        // That sentence cannot be true of a card drawn and discarded inside one
        // call, which is what this was: exactly one, with nowhere to put a
        // second. the original investigation.
        //
        // So the card goes where the rule puts it, on the enemy, and steps 1
        // and 2 become the two steps `rr:attack-enemy-activation` writes the
        // same way -- its step 1 word for word, and its step 3 sub-step for
        // sub-step, differing only in naming SCH where the attack names ATK.
        int round = world.Agenda.Current?.Round ?? 0;
        world.Agenda.Then(new PhaseStep(
            Steps.GiveBoostCard, round, 1, Index: seat, Subject: villain.ObjectId,
            ActivationId: world.Activation.Id));
        world.Agenda.Then(new PhaseStep(
            Steps.FlipBoostCards, round, 2, Index: seat, Subject: villain.ObjectId,
            ActivationId: world.Activation.Id));

        // **Step 3 is a step, because step 2 can stop and ask.** A `Boost`
        // ability that offers the player a choice suspends, and the threat used
        // to go onto the scheme while the question was still on the table --
        // so whatever they chose arrived after the number it was meant to
        // change. The attack activation has the same shape:
        // `FlipBoostCards` is step 3 and `CalculateAttackDamage` is step 4.
        world.Agenda.Then(new PhaseStep(
            Steps.SchemeThreat,
            round,
            3,
            Index: seat,
            Subject: villain.ObjectId,
            Seat: seat,
            ActivationId: world.Activation.Id));
    }

    /// <summary>
    /// Step 3 of a scheme activation —
    /// <c>rr:scheme-enemy-activation.step.3</c>.
    /// </summary>
    /// <remarks>
    /// "Place threat on the main scheme equal to the scheming enemy's
    /// <b>modified</b> SCH value." Modified is the word: the attack's own step
    /// reads a modified ATK, boost icons are registered as modifiers by step 2,
    /// and an attachment printing <c>SCH+</c> is one too.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">The step.</param>
    /// <param name="events">Where to record what happened.</param>
    internal static void SchemeThreat(
        World world, ICardFacts facts, IThreatCardAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        long placed = 0;
        if (world.Agenda.Occurrence is { } occurrence)
        {
            placed = Threat.Apply(world, facts, abilities, occurrence, events);
            occurrence.Also(Steps.SchemeEnds);
        }

        // The other kind of activation ends here. A boost card's ability that
        // says "this activation" was given for *this* scheme and must not
        // survive into somebody's attack -- `rr:activation` makes a scheme an
        // activation, and `rr:activation.6` gives an activation an end.
        world.Effects.Expire(TimingPoints.EndOfActivation, events);
        if (world.Activation is { } activation)
        {
            world.FinishedActivation = activation with { ThreatPlaced = placed };
            world.Activation = null;
        }
    }

    /// <summary>Ends a scheme without placing threat when its minion left play.</summary>
    internal static void EndSchemeEarly(World world, List<GameEvent> events)
    {
        world.Effects.Expire(TimingPoints.EndOfActivation, events);
        world.FinishedActivation = world.Activation;
        world.Activation = null;
    }

}

internal static partial class RevealProcedure
{
    /// <summary>Step 3. One card each, plus one per hazard icon in play.</summary>
    /// <remarks>
    /// <c>rr:villain-phase.step.3</c>: "Deal one encounter card to each player.
    /// Deal one additional card for each hazard icon on a card in play. These
    /// additional cards are dealt in player order."
    /// <para>
    /// Nothing here schedules a reveal. Step 4 drains the queue instead, which
    /// is what lets a card dealt at any other moment — by an ability, or by a
    /// player's deck running out mid-turn — be revealed in the same step as the
    /// rest.
    /// </para>
    /// </remarks>
    internal static void DealEncounterCards(
        World world, ICardFacts facts, List<GameEvent> events)
    {
        foreach (int seat in world.PlayerOrder)
        {
            if (Deal.EncounterCard(world, seat, "villain phase", events) is null)
            {
                return;
            }
        }

        // `rr:hazard-icon`: "for each hazard icon on cards in play, deal one
        // player one additional card *(not one card per player)*. Additional
        // cards are dealt in player order" -- so these go round the table one
        // at a time, wrapping, rather than one round per icon.
        long icons = Deal.HazardIcons(world, facts);
        for (long dealt = 0; dealt < icons; dealt++)
        {
            int seat = (world.FirstPlayer + (int)(dealt % world.Players)) % world.Players;
            if (Deal.EncounterCard(world, seat, "hazard", events) is null)
            {
                return;
            }
        }
    }

    /// <summary>Step 4, one card at a time, until the queue is empty.</summary>
    internal static void RevealNextEncounterCard(World world, PhaseStep step)
    {
        if (Deal.NextToReveal(world) is not { } next)
        {
            return;
        }

        // The reveal is an occurrence with its own windows; this heading is
        // not. Scheduling itself *after* the reveal is what makes step 4 a
        // loop -- a card revealed here can deal another, and `rr:deal.1` puts
        // that one in the same step.
        //
        // **The order of these two calls is the loop's termination.**
        // `Agenda.Then` appends in call order, so the reveal has to be
        // scheduled first; the other way round, this heading runs again with
        // the card still in the queue and schedules itself forever.
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard, step.Round, 4,
            Index: step.Index, Subject: next.Card.ObjectId, Seat: next.Player));
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCards, step.Round, 4, Index: step.Index + 1, Plan: true));
    }

    /// <summary>Step 4. Each player reveals their cards, in the order dealt.</summary>
    internal static void RevealEncounterCard(
        World world, ICardFacts facts, IRevealCardAbilities abilities, Card card, int player,
        int round, List<GameEvent> events)
    {
        var revealOccurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a revealing card has no occurrence");

        // An interrupt to this reveal may discard the card and replace its
        // effects before the occurrence applies. A card no longer in either
        // reveal staging area cannot enter play or resolve printed text from
        // the stale scheduled step. Cards explicitly revealed by an ability
        // begin in RevealingArea; villain-phase cards begin in the dealt queue.
        if (card.Area.Type is not (DeckType.DealtEncounterCardsDeck
            or DeckType.RevealingArea))
        {
            return;
        }

        // `rr:reveal.4.1` -- "if the card specifies a player to give it to,
        // **that player is considered to be revealing it**." One reassignment
        // and not a special case at the placement, because being the revealing
        // player is the whole of what the rule says: `rr:obligation.1` makes
        // every "you" on the card point at the player whose area it is in, and
        // `rr:obligation.4` puts it in the named player's.
        //
        // At one player the named player and the revealing player are the same
        // seat, which is why this went unnoticed. Above one they are not.
        switch (Reveal.Names(world, facts, card))
        {
            case null:
                break;

            case >= 0 and var named:
                player = named;
                break;

            default:
                // `rr:obligation.5` -- "if an obligation cannot be given to the
                // specified player for any reason, **ignore the card's
                // ability, remove it from the game, and reveal an additional
                // encounter card**." Dealt rather than revealed directly: step
                // 4 is a loop over what a player has been dealt, so a card put
                // in that queue is revealed by the same step -- which is how
                // `rr:surge` already works.
                var gone = world.AreaOf(DeckType.RemovedArea);
                var was = card.Area;
                World.MoveToTop(card, gone);
                events.Add(new CardsMoved(
                    Places.Reference(was), Places.Reference(gone),
                    [new Landing(card.ObjectId, gone.Cards.Count - 1)])
                {
                    Trigger = "villain phase", Verb = "Remove",
                });

                Deal.EncounterCard(world, player, "obligation", events);
                return;
        }

        // Same reason as the boost card: the revealing area is where an
        // encounter card registers its pools.
        World.MoveToTop(
            card,
            world.AreaOf(DeckType.RevealingArea, PlayArea.Of(player)));
        card.TurnFaceUp();
        world.RecordInformation(InformationKind.Reveal);
        events.Add(new CardsFlipped([card.ObjectId], true)
        {
            Trigger = "villain phase", Verb = "Reveal",
        });

        bool uniqueBlocked = facts.Kind(card.FaceId) != CardKind.EncounterVillain
            && Uniqueness.IsBlocked(world, facts, card);
        if (uniqueBlocked)
        {
            Reveal.Resolve(world, facts, card, player, events, revealOccurrence);
            // Its own reveal/entry effects are ignored. The reveal occurrence
            // still reaches its response boundary before the queue continues.
            world.Agenda.BeforeResponses(revealOccurrence);
            return;
        }

        if (facts.Kind(card.FaceId) == CardKind.Attachment
            && abilities.AttachmentTargets(world, card) is { Count: > 1 } targets)
        {
            world.Agenda.ThenContinuation(new PhaseStep(
                Steps.ChooseAttachmentTarget,
                round,
                2,
                Subject: card.ObjectId,
                Seat: player,
                Plan: true,
                ProcedureCandidates: [.. targets],
                AbilityOccurrence: revealOccurrence), revealOccurrence);
            world.Agenda.BeforeResponses(revealOccurrence);
            return;
        }

        // `rr:reveal.step.2` -- **where the card goes is decided by its type**,
        // and it happens before step 3's "When Revealed" abilities. A minion
        // that entered play is already engaged when its own ability resolves.
        Reveal.Resolve(world, facts, card, player, events, world.Agenda.Occurrence);

        FinishEncounterReveal(
            world, facts, abilities, card, player, round, revealOccurrence, events);
    }

    private static void FinishEncounterReveal(
        World world, ICardFacts facts, IRevealCardAbilities abilities, Card card, int player,
        int round, Occurrence revealOccurrence, List<GameEvent> events)
    {

        // Step 3. "Resolve each **When Revealed** ability on that card
        // *(including those provided by keywords)*."
        //
        // `rr:forced.5`: "if two or more forced abilities would initiate at
        // the same moment, the first player determines the order in which the
        // abilities initiate." Keyword-provided and printed When Revealed
        // abilities are therefore one ordering question.
        var occurrence = revealOccurrence;
        if (!abilities.CancelWhenRevealed(world, card, player, occurrence))
        {
            var keyword = Reveal.KeywordAbilities(world, facts, card, player);
            var printed = abilities.WhenRevealedAbilities(world, card, player);
            var simultaneous = keyword.Concat(printed).ToList();
            if (simultaneous.Count > 1)
            {
                if (facts.Kind(card.FaceId) == CardKind.Treachery)
                {
                    occurrence.BeginCard(card.ObjectId, simultaneous);
                }
                var handles = simultaneous
                    .Select((_, index) => 1_000_000 + index)
                    .ToList();
                world.Agenda.ThenContinuation(new PhaseStep(
                    Steps.ChooseRevealAbility,
                    round,
                    3,
                    Subject: card.ObjectId,
                    Seat: player,
                    Plan: true,
                    ProcedureAbilities: simultaneous,
                    ProcedureCandidates: handles,
                    ProcedureOccurrence: occurrence), occurrence);
                world.Agenda.BeforeResponses(occurrence);
                return;
            }

            Reveal.Keywords(world, facts, abilities, card, player, events, occurrence);
            events.AddRange(abilities.WhenRevealed(world, card, player, occurrence));
        }

        FinishEncounterRevealTail(
            world, facts, card, player, round, revealOccurrence, events);
    }

    private static void FinishEncounterRevealTail(
        World world, ICardFacts facts, Card card, int player, int round,
        Occurrence revealOccurrence, List<GameEvent> events,
        bool beforeResponses = true)
    {
        // `rr:quickstrike.2` puts this after the card's own abilities, and it
        // is the one keyword that does something *after* them rather than
        // beside them.
        bool quickstrike = Reveal.QuickstrikeApplies(world, facts, card, player);
        bool teamwork = Reveal.TeamworkApplies(world, facts, card);
        if (quickstrike && teamwork)
        {
            world.Agenda.ThenContinuation(new PhaseStep(
                Steps.ChoosePostRevealAbility,
                round,
                3,
                Subject: card.ObjectId,
                Seat: player,
                Plan: true,
                ProcedureOccurrence: revealOccurrence), revealOccurrence);
            if (beforeResponses)
            {
                world.Agenda.BeforeResponses(revealOccurrence);
            }
            return;
        }

        Reveal.Quickstrike(world, facts, card, player, round);
        Reveal.Teamwork(world, facts, card, player, round);

        FinishEncounterRevealDiscard(
            world, facts, card, player, round, revealOccurrence, beforeResponses);
    }

    private static void FinishEncounterRevealDiscard(
        World world, ICardFacts facts, Card card, int player, int round,
        Occurrence revealOccurrence, bool beforeResponses = true)
    {

        // Step 4. "If the card is a treachery, discard it." This is agenda
        // work rather than an inline move because `rr:treachery.2.1` keeps a
        // treachery whose last effect initiates activations faceup until all of
        // them finish. `Then` places this behind any activations or choices the
        // When Revealed text just scheduled.
        if (facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.DiscardRevealedTreachery,
                round,
                4,
                Subject: card.ObjectId,
                Seat: player,
                Plan: true));

            // Reveal responses wait for all four reveal steps. Move both the
            // work initiated by the final effect and this discard continuation
            // ahead of that response window, preserving their scheduled order.
            if (beforeResponses)
            {
                world.Agenda.BeforeResponses(revealOccurrence);
            }
        }
    }

    internal static void DiscardRevealedTreachery(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        // The area check keeps an ability that moved the treachery from being
        // undone. The kind check makes a reconstructed agenda refuse stale or
        // malformed continuation data rather than discarding another type.
        if (facts.Kind(card.FaceId) != CardKind.Treachery
            || card.Area.Type != DeckType.RevealingArea)
        {
            return;
        }

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        var from = card.Area;
        World.MoveToTop(card, discard);
        events.Add(new CardsMoved(
            Places.Reference(from),
            Places.Reference(discard),
            [new Landing(card.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = "villain phase", Verb = "Reveal",
        });
    }

}

internal static class PhaseTransitionProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.PassFirstPlayerToken:
                PassFirstPlayerToken(world);
                break;
            case Steps.EndVillainPhase:
                PhaseEnd.EndVillainPhase(world, facts, events);
                break;
            case Steps.DrawToHandSize:
                PhaseEnd.DrawToHandSize(world, facts, events);
                break;
            case Steps.ReadyCards:
                PhaseEnd.ReadyCards(world, events);
                break;
            case Steps.EndPlayerPhase:
                PhaseEnd.EndPlayerPhase(world, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the phase-transition procedure has no step '{step.What}'");
        }
        return null;
    }

    /// <summary>Step 5. <c>rr:villain-phase.step.5</c>, to the next clockwise player.</summary>
    internal static void PassFirstPlayerToken(World world) =>
        world.FirstPlayer = world.Players > 0 ? (world.FirstPlayer + 1) % world.Players : 0;
}

using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    /// <inheritdoc/>
    public Prompt? Choosing(
        World world, Card source, int player, int stoppedAt, AbilityType? tier = null)
        => Choosing(world, source, player, stoppedAt, tier, finalStep: false);

    /// <inheritdoc/>
    public Prompt? Choosing(
        World world, Card source, int player, int stoppedAt, AbilityType? tier, bool finalStep)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        var persisted = ContinuationStep(world, source, stoppedAt, tier);
        var cast = Resuming(
            world, source, player, tier, finalStep,
            persisted?.AbilityOccurrence) with
        {
            EachPlayerFrame = persisted?.EachPlayerFrame ?? false,
            FinalPlayer = persisted?.FinalPlayer ?? false,
            AbilityPlayer = persisted?.AbilityPlayer ?? player,
        };
        RestorePersisted(cast, persisted);
        if (persisted is
            { AbilityOrdinal: >= 0, AbilityPath: { } choicePath } current)
        {
            var decoded = AbilityContinuationCodec.Decode(
                program, source, current, tier);
            RestoreContinuationCursor(cast, decoded.State);
            RestoreAlteredFromFrames(cast, decoded.State.Frames);
        }
        var choice = AbilityContinuationCodec.Choice(
            program, source, tier, stoppedAt, persisted, AdmissionContext(cast));

        if (choice.OperationName() == "indirectDamage")
        {
            return AbilityStructuralExecution.DescribeIndirectDamage(
                StructuralContext(cast), (AbilityEffect.IndirectDamage)choice);
        }

        if (choice.OperationName() == "and")
        {
            return AbilityStructuralExecution.DescribeSimultaneous(
                StructuralContext(cast), (AbilityEffect.Simultaneous)choice);
        }

        if (choice.OperationName() is "enemyAttacks" or "enemySchemes")
        {
            return AbilityStructuralExecution.DescribeActivationOrder(
                StructuralContext(cast), (AbilityEffect.ActivateEnemies)choice);
        }

        // `rr:choose-option` and `rr:choose-game-element` are two questions and
        // not one: an option is a branch the card lists, an element is a card
        // on the board.
        if (choice.OperationName() == "resolveSpecials")
        {
            return AbilityStructuralExecution.DescribeSpecialChoice(
                StructuralContext(cast), choice);
        }
        if (choice.OperationName() is "payOrEffect" or "payOrExhaust")
        {
            return AbilityStructuralExecution.DescribePaymentChoice(
                StructuralContext(cast), (AbilityEffect.PayOrEffect)choice);
        }
        if (choice.OperationName() == "chooseTopForHand")
        {
            return AbilityStructuralExecution.DescribeSpecialChoice(
                StructuralContext(cast), choice);
        }
        if (choice.OperationName() == "chooseDiscardToShuffle")
        {
            return AbilityStructuralExecution.DescribeSpecialChoice(
                StructuralContext(cast), choice);
        }
        if (choice.OperationName() == "thwartDifferentSchemes")
        {
            return AbilityStructuralExecution.DescribeThwartChoice(
                StructuralContext(cast), (AbilityEffect.ThwartGroup)choice);
        }
        if (choice.OperationName() == "makeTheCall")
        {
            return AbilityStructuralExecution.DescribeMakeTheCall(StructuralContext(cast));
        }
        if (choice.OperationName() == "legalPractice")
        {
            return AbilityStructuralExecution.DescribeThwartChoice(
                StructuralContext(cast), (AbilityEffect.ThwartGroup)choice);
        }
        var described = AbilityStructuralExecution.DescribeGenericChoice(
            StructuralContext(cast), choice, ContinuationFacts(source, persisted, tier));
        _ = ApplyAdmission(described.Admission, cast);
        return described.Prompt;
    }

    /// <inheritdoc/>
    public Prompt? Choosing(
        World world, Card source, int player, int stoppedAt, AbilityType? tier,
        bool finalStep, bool eachPlayerFrame, bool finalPlayer) =>
        Choosing(world, source, player, stoppedAt, tier, finalStep);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier = null)
        => Chose(world, source, player, stoppedAt, input, tier, finalStep: false);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep)
        => ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame: false, finalPlayer: false);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer)
        => ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame, finalPlayer, eventTrigger: null);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string trigger)
        => ChoseCore(
            world, source, player, stoppedAt, input, tier, finalStep,
            eachPlayerFrame, finalPlayer, trigger);

    private List<GameEvent> ChoseCore(
        World world, Card source, int player, int stoppedAt, Decision input,
        AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer,
        string? eventTrigger = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(input);

        var persisted = ContinuationStep(world, source, stoppedAt, tier);
        var continuation = world.Agenda.Current is { What: Steps.ChooseOption, Plan: true }
            && world.Agenda.Occurrence is { } live
            && live.Is(Steps.TurnAction)
                ? live
                : null;
        var cast = Resuming(
            world, source, player, tier, finalStep,
            persisted?.AbilityOccurrence ?? continuation) with
        {
            EachPlayerFrame = eachPlayerFrame,
            FinalPlayer = finalPlayer,
            AbilityPlayer = persisted?.AbilityPlayer ?? player,
            EventTrigger = eventTrigger,
            GainedKeywords = world.Agenda.Current is
                { What: Steps.ChooseOption, SurgeGained: true }
                    ? new HashSet<string>(["surge"], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
        };
        RestorePersisted(cast, persisted);
        if (persisted is
            { AbilityOrdinal: >= 0, AbilityPath: { } path } current)
        {
            var decoded = AbilityContinuationCodec.Decode(
                program, source, current, tier);
            RestoreContinuationCursor(cast, decoded.State);
            RestoreAlteredFromFrames(cast, decoded.State.Frames);
        }
        var choice = AbilityContinuationCodec.Choice(
            program, source, tier, stoppedAt, persisted, AdmissionContext(cast));
        if (cast.AbilityOrdinal >= 0)
        {
            cast.TrackResolution(cast.AbilityOrdinal);
        }
        cast.At(Math.Max(0, stoppedAt - 1));
        cast.SetContinuation(persisted?.AbilityHasContinuation ?? On(source).Any(ability =>
            (tier is null || ability.Trigger.Timing == tier)
            && ability.Effect.OperationName() == "seq"
            && OrderedEffects(ability.Effect).Length > stoppedAt));

        if (choice.OperationName() == "and")
        {
            bool outerContinuation = cast.HasContinuation;
            ApplyStructuralDecision(AbilityStructuralExecution.AnswerSimultaneous(
                StructuralContext(cast), (AbilityEffect.Simultaneous)choice, input), cast);
            if (cast.Suspended) return cast.Events;
            cast.SetContinuation(outerContinuation);

            return Continue(source, cast, stoppedAt);
        }

        if (choice.OperationName() is "enemyAttacks" or "enemySchemes")
        {
            var answer = AbilityStructuralExecution.AnswerActivationOrder(
                StructuralContext(cast), (AbilityEffect.ActivateEnemies)choice, input);
            ApplyStructuralDecision(answer, cast);
            if (cast.Suspended)
            {
                return cast.Events;
            }
            return Continue(source, cast, stoppedAt);
        }

        if (choice.OperationName() == "resolveSpecials")
        {
            var answer = AbilityStructuralExecution.AnswerSpecialChoice(
                StructuralContext(cast), choice, input);
            if (answer is Unsupported unsupported)
                throw new RulesNotImplementedException(unsupported.Reason);
            var command = answer as ResolveSpecialsCommand
                ?? throw new InvalidOperationException("Structural owner did not return Special ordering");

            int round = world.Agenda.Current?.Round ?? 0;
            foreach (var (id, index) in command.Targets.Select((id, index) => (id, index)))
            {
                world.Agenda.Then(new PhaseStep(
                    Steps.ResolveSpecial, round, index + 1, Subject: id, Seat: player,
                    Plan: true, FinalStep: index == input.Targets.Count - 1));
            }
            if (command.Targets.Length > 0)
            {
                cast.ResolveEffect();
            }

            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "payOrEffect")
        {
            var command = AbilityStructuralExecution.AnswerPaymentChoice(
                StructuralContext(cast), (AbilityEffect.PayOrEffect)choice, input);
            if (command is Unsupported unsupported)
                throw new RulesNotImplementedException(unsupported.Reason);
            var payment = (PayOrCommand)command;
            if (payment.Pay)
            {
                string required = EffectOf<AbilityEffect.PayOrEffect>(choice, cast).Resources;
                CardPlay.Spend(
                    world, world.Facts, resourceAbilities,
                    [world.Seats[player].Hand], input.Spent,
                    required.Length, required, -1, player, cast.Events);
                cast.ResolveEffect();
            }
            else if (input.Affordance == 1)
            {
                RunChild(EffectFollowing(choice), new ChoiceOtherwiseFrame(), cast);
                if (cast.Suspended)
                {
                    return cast.Events;
                }
            }
            else
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer option {input.Affordance}");
            }
            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "payOrExhaust")
        {
            var command = AbilityStructuralExecution.AnswerPaymentChoice(
                StructuralContext(cast), (AbilityEffect.PayOrEffect)choice, input);
            if (command is Unsupported unsupported)
                throw new RulesNotImplementedException(unsupported.Reason);
            var payment = (PayOrCommand)command;
            if (payment.Pay)
            {
                string required = EffectOf<AbilityEffect.PayOrEffect>(choice, cast).Resources;
                CardPlay.Spend(
                    world, world.Facts, resourceAbilities,
                    [world.Seats[player].Hand], input.Spent,
                    required.Length, required, itself: -1, player, cast.Events);
                cast.ResolveEffect();
            }
            else if (input.Affordance == 1)
            {
                RunChild(EffectFollowing(choice), new ChoiceOtherwiseFrame(), cast);
                if (cast.Suspended)
                {
                    return cast.Events;
                }
            }
            else
            {
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' offers spend or exhaust, not option {input.Affordance}");
            }

            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "chooseTopForHand")
        {
            var answer = AbilityStructuralExecution.AnswerSpecialChoice(
                StructuralContext(cast), choice, input);
            if (answer is Unsupported unsupported)
                throw new RulesNotImplementedException(unsupported.Reason);
            var command = answer as ChooseTopForHandCommand
                ?? throw new InvalidOperationException("Structural owner did not return top-card selection");
            var top = command.Top.Select(id => world.Cards[id]).ToList();
            var selected = world.Cards[command.Selected];
            AbilityCardStateExecution.ChooseTopForHand(top, selected,
                new AbilityCardStateContext(cast.ExpressionContext(), cast.Trigger,
                    cast.Events, cardPlayAbilities, readinessAbilities,
                    new AbilityCardStateResult()));
            cast.ResolveEffect();

            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "chooseDiscardToShuffle")
        {
            var answer = AbilityStructuralExecution.AnswerSpecialChoice(
                StructuralContext(cast), choice, input);
            if (answer is Unsupported unsupported)
                throw new RulesNotImplementedException(unsupported.Reason);
            var command = answer as ShuffleDiscardCommand
                ?? throw new InvalidOperationException("Structural owner did not return discard selection");
            var selected = command.Targets.Select(id => world.Cards[id]).ToList();
            AbilityCardStateExecution.ShuffleDiscardIntoDeck(selected,
                new AbilityCardStateContext(cast.ExpressionContext(), cast.Trigger,
                    cast.Events, cardPlayAbilities, readinessAbilities,
                    new AbilityCardStateResult()));
            cast.ResolveEffect();
            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "thwartDifferentSchemes")
        {
            var answer = AbilityStructuralExecution.AnswerThwartChoice(
                StructuralContext(cast), (AbilityEffect.ThwartGroup)choice, input);
            if (answer is Unsupported unsupported) throw new RulesNotImplementedException(unsupported.Reason);
            var command = (ThwartSelectionCommand)answer;
            var selected = command.Resolving.Select(id => world.Cards[id]).ToList();

            // rr:then: the second Crisis Interdiction removal is dependent on
            // the first removal fully resolving. The choice is simultaneous,
            // but only the first selected scheme belongs to the pre-then
            // effect, so determine that outcome in isolation before the power
            // receives the targets it will actually resolve against.
            cast.Choose(world.Cards[command.Scheme]);
            var power = ((AbilityEffect.ThwartGroup)choice).Thwart;
            ApplyStructuralDecision(AbilityStructuralExecution.Power(
                StructuralContext(cast), power,
                world.Cards[command.Scheme], selected, -1), cast);
            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "makeTheCall")
        {
            var answer = AbilityStructuralExecution.AnswerMakeTheCall(StructuralContext(cast), input);
            if (answer is Unsupported unsupported) throw new RulesNotImplementedException(unsupported.Reason);
            var ally = world.Cards[((MakeTheCallCommand)answer).Ally];
            long cost = Resources.Cost(ally.FaceId, world.Facts, world.Players) ?? 0;
            CardPlay.Spend(
                world, world.Facts, resourceAbilities,
                [world.Seats[player].Hand], input.Spent,
                cost, Resources.Required(world, ally, world.Facts),
                source.ObjectId, player, cast.Events, payingFor: ally);
            CardPlay.PutAllyIntoPlay(
                world, world.Facts, cardPlayAbilities, ally, player, cast.Trigger, cast.Events);
            cast.ResolveEffect();
            return Continue(source, cast, stoppedAt);
        }
        if (choice.OperationName() == "legalPractice")
        {
            var answer = AbilityStructuralExecution.AnswerThwartChoice(
                StructuralContext(cast), (AbilityEffect.ThwartGroup)choice, input);
            if (answer is Unsupported unsupported) throw new RulesNotImplementedException(unsupported.Reason);
            var command = (ThwartSelectionCommand)answer;
            var scheme = world.Cards[command.Scheme];
            AbilityCardStateExecution.DiscardCards(
                command.Discard.Select(id => world.Cards[id]).ToList(), CardPlay.Verb,
                new AbilityCardStateContext(cast.ExpressionContext(), cast.Trigger,
                    cast.Events, cardPlayAbilities, readinessAbilities,
                    new AbilityCardStateResult()));
            cast.ResolveEffect();
            cast.Choose(scheme);
            ApplyStructuralDecision(AbilityStructuralExecution.Power(
                StructuralContext(cast), ((AbilityEffect.ThwartGroup)choice).Thwart,
                scheme, [scheme], command.PowerAmount), cast);
            return Continue(source, cast, stoppedAt);
        }

        if (choice.OperationName() == "indirectDamage")
        {
            var answer = AbilityStructuralExecution.AnswerIndirectDamage(
                StructuralContext(cast), (AbilityEffect.IndirectDamage)choice, input);
            if (answer is Unsupported unsupported)
                throw new RulesNotImplementedException(unsupported.Reason);
            var assigned = (AssignedDamageCommand)answer;
            Resolve(choice, cast, assigned.Assigned.ToDictionary());
            return Continue(source, cast, stoppedAt);
        }


        ApplyStructuralDecision(AbilityStructuralExecution.AnswerGenericChoice(
            StructuralContext(cast), choice, ContinuationFacts(source, persisted, tier), input), cast);
        if (cast.Suspended)
        {
            return cast.Events;
        }
        return Continue(source, cast, stoppedAt);
    }

    private AbilityContinuationFacts ContinuationFacts(
        Card source, PhaseStep? step, AbilityType? tier)
    {
        if (step is not { AbilityOrdinal: >= 0, AbilityPath: not null } persisted)
            return AbilityContinuationFacts.Empty;
        return AbilityContinuationCodec.Decode(program, source, persisted, tier).Facts;
    }

    /// <summary>Whether the source has a player-card face.</summary>
    private static bool IsPlayerCard(AbilityResolutionState cast) =>
        IsPlayerCard(cast.World.Facts, cast.Source);

    /// <summary>Whether a card face belongs to a player rather than the scenario.</summary>
    private static bool IsPlayerCard(ICardFacts facts, Card card) => AbilityCardQueries.IsPlayerCard(facts, card);

    private static int ControllerOf(World world, Card card) => AbilityCardQueries.ControllerOf(world, card);

}

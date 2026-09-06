using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityCostSelection;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>
    /// What one constant ability grants, as continuous effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A deliberately tiny vocabulary: a sequence, a condition, and a grant.
    /// <c>rr:ability.9</c> is why the condition is here rather than resolved
    /// once — "some constant abilities continuously seek a specific condition
    /// <i>(denoted by words such as 'during', 'if', or 'while')</i>. The effects
    /// of such abilities are active anytime the specific condition is met." So
    /// the test is re-read on every ask, and Unus stops retaliating the moment
    /// Gene Pool is thwarted below three threat.
    /// </para>
    /// <para>
    /// Everything else throws. A constant ability that moves a card or deals
    /// damage is a different shape from this one — it would have to happen at a
    /// moment, and a constant ability has no moment — so the card that needs it
    /// needs a design rather than a case.
    /// </para>
    /// </remarks>
    private static void Grants(AbilityEffect effect, Cast cast, List<ContinuousEffect> found)
    {
        switch (effect)
        {
            case AbilityEffect.Sequence sequence:
                foreach (var step in sequence.Effects)
                {
                    Grants(step, cast, found);
                }
                break;
            case AbilityEffect.Simultaneous simultaneous:
                foreach (var step in simultaneous.Effects)
                {
                    Grants(step, cast, found);
                }
                break;
            case AbilityEffect.Conditional conditional:
                if ((Test(conditional.Test, cast) ? conditional.Then : conditional.Else) is { } taken)
                {
                    Grants(taken, cast, found);
                }
                break;
            case AbilityEffect.GrantField { Until: null } grant:
                foreach (var target in ConstantTargets(grant.Cards, grant.EachCard, cast))
                {
                    found.Add(new ContinuousEffect(
                        EffectSource.ConstantAbility, Kind: grant.Field,
                        Amount: Amount(grant.Amount, cast), Card: cast.Source.ObjectId,
                        Affects: target.ObjectId, Lasts: Duration.WhileInPlay));
                }
                break;
            case AbilityEffect.GrantTrait { Until: null } grant:
                foreach (var target in ConstantTargets(grant.Cards, grant.EachCard, cast))
                {
                    found.Add(new ContinuousEffect(
                        EffectSource.ConstantAbility, Kind: Rules.State.Traits.Granted + grant.Trait,
                        Card: cast.Source.ObjectId, Affects: target.ObjectId, Lasts: Duration.WhileInPlay));
                }
                break;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PreventThreatRemoval }:
                // A prohibition is answered by `CanRemoveThreat`; it is not a
                // numeric modifier and therefore contributes no effect here.
                break;

            case AbilityEffect.DoubleResourceFor:
                // This constant acts while its resource card is spent from
                // hand. `ResourcesGeneratedBy` reads it with the payment's
                // target card, which is context this general effect list does
                // not carry.
                break;

            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.RequireAllyDefender }:
                // Defender declaration carries the attack and its engaged
                // player; `Defenders` reads this constraint in that context.
                break;

            case AbilityEffect.PreventDamageFrom:
            case AbilityEffect.PreventDamageWhile:
                // Damage carries both source and target. `CanTakeDamage`
                // evaluates these prohibitions in that complete context.
                break;

            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PreventReady }:
                // The card to be readied and the source of that instruction
                // are available only when `CanReady` asks the question.
                break;

            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' cannot resolve {effect} as a constant ability");
        }
    }

    private static IReadOnlyList<Card> ConstantTargets(AbilityCardSelection selection, bool each, Cast cast)
    {
        if (each) return Every(selection, cast);
        if (Find(selection, cast) is { } target) return [target];
        if (selection is AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo }) return [];
        throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' card {cast.Source.ObjectId} in "
            + $"{cast.Source.Area.Type} hosted by {cast.Source.Area.Host} would grant "
            + "to a card that is not there");
    }

    private static bool ProhibitsThreatRemoval(AbilityEffect effect, Cast cast, Card scheme)
    {
        return effect switch
        {
            AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
                ProhibitsThreatRemoval(step, cast, scheme)),
            AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
                ProhibitsThreatRemoval(step, cast, scheme)),
            AbilityEffect.Conditional conditional => (Test(conditional.Test, cast) ? conditional.Then : conditional.Else)
                is { } branch && ProhibitsThreatRemoval(branch, cast, scheme),
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.PreventThreatRemoval } prohibition =>
                Find(prohibition.Selection, cast)?.ObjectId == scheme.ObjectId,
            _ => false,
        };
    }

    // `rr:lasting-effects` -- an effect "for a specified duration (such as
    // [...] 'until the end of this attack')".
    private static void GrantUntil(
        AbilityCardSelection card, string kind, AbilityNumber amount, string until, Cast cast)
    {
        var target = ResolveCard(card, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would grant to a card that is not there");
        EnsureLastingPeriodOpen(until, cast);
        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: kind,
            Amount: ResolveAmount(amount, cast),
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.UntilEndOf(until)));

        if (string.Equals(kind, "stalwart", StringComparison.Ordinal))
        {
            Statuses.RemoveAfflictionsIfStalwart(
                cast.World, cast.World.Facts, target, cast.Trigger, cast.Events);
        }
    }

    private static void EnsureLastingPeriodOpen(string until, Cast cast)
    {
        if (!LastingPeriodIsOpen(until, cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' begins a lasting effect outside its named period");
        }
    }

    // rr:delayed-effect.1 resolves an effect "after their specified timing
    // point or future condition occurs or becomes true". The entry is data
    // with an engine-owned kind, not a closure over an executable effect.
    private static void DelayUntil(AbilityEffect.DelayedStun delayed, Cast cast)
    {
        // The damaged character is identified by the future occurrence, not
        // at registration. "This attack" bounds the condition as well: an
        // attack stopped by Tough must not stun a later attack's recipient.
        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.StunTheSubject,
            Card: cast.Source.ObjectId,
            Affects: null,
            Lasts: new Duration(
                Until: delayed.Within,
                OnCondition: Steps.DamageDealt,
                Uses: 1)));
    }

    private static void DelayUntil(AbilityEffect.DelayedDiscard delayed, Cast cast)
    {
        var target = ResolveCard(delayed.Card, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would delay a discard of a card that is not there");

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.DiscardFromPlay,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.NextTime(delayed.Condition)));
    }

    // ---- reading a value ---------------------------------------------------

    /// <summary>
    /// "Flip to alter-ego form" — <c>rr:form-change-form</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It does not use up the turn's flip.</b> <c>rr:form-change-form.3</c>:
    /// "if a card ability causes a player to change forms, it does not count
    /// against the one voluntary form change the player is permitted during
    /// their turn that round." So this goes through <c>Forms.Change</c>, which
    /// turns the card, and leaves <c>Seat.FormChangedInRound</c> alone —
    /// <c>Game</c> sets that when the player takes the turn option.
    /// </para>
    /// <para>
    /// A player already in the named form does nothing. "Flip <b>to</b>
    /// alter-ego form" names a destination, and flipping an alter-ego would
    /// arrive at the wrong one.
    /// </para>
    /// </remarks>
    private static void ChangeForm(AbilityEffect.ChangeForm change, Cast cast)
    {
        var seat = cast.World.Seats[Seat(change.Player, cast)];
        string form = change.Form;
        if (AlreadyInForm(change, cast))
        {
            return;
        }

        string was = seat.IdentityCard.FaceId;
        Forms.Change(seat, cast.World.Facts);
        cast.Events.Add(new CardsFlipped([seat.IdentityCard.ObjectId], true)
        {
            Trigger = cast.Trigger, Verb = "Change_Form",
        });

        if (!Forms.In(cast.World, seat, cast.World.Facts, form))
        {
            throw new RulesNotImplementedException(
                $"flipping '{was}' did not reach {form}");
        }
    }

    private static AbilityEffect.ChangeForm FormChangeOf(AbilityEffect node, Cast cast) =>
        (AbilityEffect.ChangeForm)node;

    private static bool AlreadyInForm(AbilityEffect.ChangeForm change, Cast cast) =>
        AbilityAdmissionFacts.AlreadyInForm(
            cast.World, Seat(change.Player, cast), change.Form);

    /// <summary>"Exhaust …" — <c>rr:exhausted</c>.</summary>
    /// <remarks>
    /// A card already exhausted stays exhausted and reports nothing:
    /// <c>rr:exhausted</c> is a state and not a counter, so exhausting
    /// twice is not two exhaustions and must not be two events on the wire.
    /// </remarks>
    private static void DrawToHandSize(AbilityEffect.DrawToHandSize draw, Cast cast)
    {
        int player = Seat(draw.Player, cast);
        var seat = cast.World.Seats[player];
        // rr:printed reads "physically printed on the card"; an unqualified hand size
        // includes the live modifiers instead.
        long size = draw.Printed
            ? cast.World.Facts.PrintedValue(seat.IdentityCard.FaceId, "HS", cast.World.Players)
            : PhaseEnd.HandSize(cast.World, seat, cast.World.Facts);
        int count = (int)Math.Max(0, size - HandCountDuringEvent(cast, seat));
        Draw.Cards(cast.World, player, count, cast.Trigger, cast.Events);
    }

    private static bool CanDrawToPrintedHandSize(AbilityEffect node, Cast cast)
        => AbilityAdmissionFacts.CanDrawToPrintedHandSize(
            cast.World, cast.Source,
            Seat(EffectOf<AbilityEffect.DrawToHandSize>(node, cast).Player, cast));

    private static int HandCountDuringEvent(Cast cast, Seat seat) =>
        seat.Hand.Cards.Count - (cast.Source.Area == seat.Hand
            && cast.World.Facts.Kind(cast.Source.FaceId) == CardKind.Event ? 1 : 0);

    private static AbilityEffect.RemoveCounters CounterRemovalOf(AbilityEffect node, Cast cast) =>
        (AbilityEffect.RemoveCounters)node;

    /// <summary>
    /// Advances because a card effect says to —
    /// <c>rr:main-scheme-main-scheme-deck.2.2</c>.
    /// </summary>
    /// <remarks>
    /// "If the main scheme advances other than through having threat on it
    /// equal to or greater than its target threat value, that main scheme is
    /// not considered completed." This calls the deck transition directly and
    /// never writes <c>is_completed</c>. The DSL word <c>next</c> is the
    /// engine's choice; stage-addressed advancement needs a separate
    /// implementation.
    /// </remarks>
    private static void AdvanceMainScheme(Cast cast)
    {
        var scheme = cast.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' advances a main scheme that is not in play");
        MainScheme.Advance(
            cast.World, cast.World.Facts, cast.Abilities, scheme,
            cast.Trigger, cast.Events);
    }

    private static bool CanAdvanceMainScheme(Cast cast) =>
        AbilityAdmissionFacts.CanAdvanceMainScheme(cast.World);

    /// <summary>
    /// Reads a named counter pool, or every typed pool when the card says
    /// "all-purpose counter" — <c>rr:all-purpose-counter.1</c> and
    /// <c>rr:all-purpose-counter.2</c>.
    /// </summary>
    /// <remarks>
    /// Counters use the same token inventory as threat, damage, and status
    /// markers because the rules consider them tokens for every game purpose.
    /// The DSL spelling <c>allPurpose</c> is the engine's choice. A reference
    /// to it can see every <c>c_*</c> pool regardless of the type a card gave
    /// that physical counter.
    /// </remarks>
    private static long CounterCount(Card card, string type) =>
        AbilityExpressionEvaluation.CounterCount(card, type);

    private static void CancelWhenRevealed(Cast cast)
    {
        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "cancelWhenRevealed",
            Card: cast.Source.ObjectId,
            Affects: cast.Occurrence.Subject,
            Lasts: new Duration(Uses: 1)));
    }

    /// <summary>
    /// The steps of a <c>seq</c>, from wherever the ability left off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An ability can ask more than once.</b> Eviction Notice says "you may
    /// flip to alter-ego form" and then "choose:", which is two questions in a
    /// row; 36 cards in the pool pair a "may" with a listed choice, and every
    /// "may" is itself a question.
    /// </para>
    /// <para>
    /// A suspended ability stores its exact authored ability and structural
    /// path in <see cref="PhaseStep"/>. Unwinding that path resumes nested
    /// sequences and branches without rerunning completed effects.
    /// </para>
    /// </remarks>
    private static void Sequence(AbilityEffect node, Cast cast, int from)
    {
        bool outerContinuation = cast.HasContinuation;
        var transition = AbilityStructuralExecution.SequenceStart(
            StructuralContext(cast), (AbilityEffect.Sequence)node, from);
        while (transition is RunLeaf leaf
            && leaf.Frames[^1] is SequenceFrame frame)
        {
            RunStructuralLeaf(leaf, cast);
            var observation = new AbilityStructuralObservation(cast.Suspended);
            if (!cast.Suspended)
                cast.SetContinuation(outerContinuation);
            transition = AbilityStructuralExecution.NextSequence(
                StructuralContext(cast), (AbilityEffect.Sequence)node, frame,
                observation);
            if (cast.Suspended)
                return;
        }
        ApplyStructuralCompletion(transition, cast);
        cast.SetContinuation(outerContinuation);
    }

    /// <summary>Repeats one count-based “for each” effect.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:for-each.1-.2</c> makes damage and threat removal without a
    /// “choose” instruction one combined instance against one target. Those
    /// effects therefore multiply before entering the ordinary resolver; a
    /// loop would incorrectly spend Tough on the first point and deal the
    /// remaining points as later instances.
    /// </para>
    /// <para>
    /// <c>rr:for-each.3</c> makes an explicit choice a new decision every
    /// iteration. Each frame is persisted in the ability path so an answer can
    /// finish its iteration, update the board, and then ask the next question
    /// from the board as it now stands. Evaluating the child afresh also makes
    /// an ability modifier part of every instance as required by
    /// <c>rr:for-each.4</c>.
    /// </para>
    /// </remarks>
    private static void ForEach(AbilityEffect node, Cast cast)
    {
        var repeated = (AbilityEffect.ForEach)node;
        bool outerContinuation = cast.HasContinuation;
        var transition = AbilityStructuralExecution.ForEachStart(
            StructuralContext(cast), repeated);
        if (transition is RunCombinedForEach combined)
        {
            RunCombinedForEach(combined, cast);
            return;
        }
        while (transition is RunLeaf leaf
            && leaf.Frames[^1] is ForEachFrame frame)
        {
            RunStructuralLeaf(leaf, cast);
            var observation = new AbilityStructuralObservation(cast.Suspended);
            if (!cast.Suspended)
                cast.SetContinuation(outerContinuation);
            transition = AbilityStructuralExecution.NextForEach(
                StructuralContext(cast), repeated, frame,
                observation);
            if (cast.Suspended)
                return;
        }
        ApplyStructuralCompletion(transition, cast);
        cast.SetContinuation(outerContinuation);
    }

    /// <summary>Interrupts a discard effect once for every matching card.</summary>
    /// <remarks>
    /// <c>rr:alteration-effect</c> says an “each time” effect halts the
    /// preceding ability, resolves in its entirety, and only then lets that
    /// ability continue. Discarding one card per frame makes that ordering
    /// observable: its alteration finishes before the next card is discarded.
    /// The exact-card binding survives an immediate encounter-deck reset.
    /// </remarks>
    private static void EachTime(AbilityEffect node, Cast cast)
        => ContinueEachTime((AbilityEffect.EachTime)node, cast, from: 0, count: null);

    private static void ContinueEachTime(
        AbilityEffect.EachTime repeated, Cast cast, long from, long? count)
    {
        bool outerContinuation = cast.HasContinuation;
        var transition = AbilityStructuralExecution.EachTimeStart(
            StructuralContext(cast), repeated, from, count);
        while (transition is DiscardEachTime discard)
        {
            int before = cast.Discarded.Count;
            if (!TryRunCardState(discard.Effect, cast))
                throw new InvalidOperationException("The card-state owner refused discardTop");
            var discarded = cast.Discarded.Skip(before).SingleOrDefault();
            if (discarded is not null)
                cast.BindAlteration(discarded);
            transition = AbilityStructuralExecution.AfterEachTimeDiscard(
                StructuralContext(cast), repeated, discard.Frame,
                new AbilityStructuralObservation(false, discarded));
            if (transition is not RunLeaf leaf
                || leaf.Frames[^1] is not EachTimeFrame frame)
            {
                continue;
            }

            RunStructuralLeaf(leaf, cast);
            var observation = new AbilityStructuralObservation(cast.Suspended);
            if (!cast.Suspended)
                cast.SetContinuation(outerContinuation);
            transition = AbilityStructuralExecution.NextEachTime(
                StructuralContext(cast), repeated, frame,
                observation);
            if (cast.Suspended)
                return;
        }
        ApplyStructuralCompletion(transition, cast);
        cast.SetContinuation(outerContinuation);
    }

    private static void RunCombinedForEach(RunCombinedForEach combined, Cast cast)
    {
        switch (combined.Effect)
        {
            case AbilityEffect.Damage damage:
                if (DamageTargets(damage.Cards, cast).Count != 1)
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' has a for-each damage effect without "
                        + "choose and does not resolve to one target");
                }
                DealDamage(damage, combined.Effect, cast, combined.Multiplier);
                return;

            case AbilityEffect.RemoveThreat removal:
                if (Every(removal.Schemes, cast).Count != 1)
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' has a for-each threat-removal effect "
                        + "without choose and does not resolve to one target");
                }
                RemoveThreat(removal, cast, combined.Multiplier);
                return;

            default:
                throw new InvalidOperationException(
                    "Structural execution returned an unsupported combined repetition");
        }
    }

    private static void ApplyStructuralCompletion(
        AbilityStructuralTransition transition, Cast cast)
    {
        switch (transition)
        {
            case Complete { Admission: { } admission }:
                _ = ApplyAdmission(admission, cast);
                break;
            case Rejected rejected:
                throw new AbilityException(rejected.Reason);
            case Unsupported unsupported:
                throw new RulesNotImplementedException(unsupported.Reason);
            case Complete:
                break;
            default:
                throw new InvalidOperationException(
                    $"Structural execution stopped at {transition.GetType().Name}");
        }
    }

    private static void RunStructuralLeaf(RunLeaf leaf, Cast cast)
    {
        if (leaf.Admission is { } admission)
            _ = ApplyAdmission(admission, cast);
        var frame = leaf.Frames[^1];
        cast.At(leaf.Position);
        cast.SetContinuation(leaf.HasContinuation);
        cast.StructuralPath.Add(frame);
        try
        {
            Run(leaf.Effect, cast);
        }
        finally
        {
            cast.StructuralPath.RemoveAt(cast.StructuralPath.Count - 1);
        }
    }

    private static void RunChild(
        AbilityEffect node, AbilityStructuralFrame frame, Cast cast)
    {
        cast.StructuralPath.Add(frame);
        try
        {
            Run(node, cast);
        }
        finally
        {
            cast.StructuralPath.RemoveAt(cast.StructuralPath.Count - 1);
        }
    }

    private static int AbilityOrdinal(AbilityEffect node, Cast cast)
    {
        if (cast.AbilityOrdinal >= 0)
        {
            return cast.AbilityOrdinal;
        }

        var runner = (AbilityRunner)cast.Abilities;
        return AbilityContinuationCodec.OrdinalForNode(
            runner.program, cast.Source, cast.AbilityFace, cast.Tier,
            cast.StructuralPath, node);
    }

    private ImmutableArray<CompiledCardAbility> AbilitiesOn(Card source, string? face) =>
        AbilityContinuationCodec.AbilitiesOn(program, source, face);

    private void TrackResolution(Cast cast, CompiledCardAbility ability)
    {
        var sameTier = AbilitiesOn(cast.Source, cast.AbilityFace)
            .Where(candidate => candidate.Trigger.Timing == ability.Trigger.Timing)
            .ToList();
        int ordinal = sameTier.FindIndex(candidate => ReferenceEquals(candidate, ability));
        if (ordinal < 0)
        {
            ordinal = sameTier.IndexOf(ability);
        }
        if (ordinal < 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot identify the ability whose resolution is tracked");
        }
        cast.RestoreAbility(ordinal, []);
        cast.TrackResolution(ordinal);
    }

    private CompiledCardAbility AbilityAt(
        Card source, AbilityType? tier, int ordinal, string? face = null) =>
        AbilityContinuationCodec.AbilityAt(program, source, tier, ordinal, face);

    private static void RestorePersisted(Cast cast, PhaseStep? continuation)
    {
        if (continuation is not { } step)
        {
            return;
        }
        ApplyRestored(cast, AbilityContinuationCodec.RestoreState(
            cast.World.Cards, step.Discarded, step.AbilityResults,
            step.AbilityActor, cast.Source.FaceId));
    }

    private static void RestorePersisted(
        Cast cast, IReadOnlyList<int>? discarded,
        IReadOnlyDictionary<string, long>? results)
    {
        ApplyRestored(cast, AbilityContinuationCodec.RestoreState(
            cast.World.Cards, discarded, results, -1, cast.Source.FaceId));
    }

    private static void ApplyRestored(Cast cast, RestoredContinuationState state)
    {
        cast.Discarded.Clear();
        cast.Discarded.AddRange(state.Discarded);
        foreach (var (name, value) in state.Results)
        {
            cast.Results[name] = value;
        }
        cast.RestoreCrisisIgnoringThwarts(state.CrisisIgnoringThwarts);
        cast.RestoreSourceIncarnation(state.SourceIncarnation);
        if (state.Chosen is { } chosen)
            cast.RestorePersistedSelection(
                cast.World.Cards[chosen.ObjectId], chosen.AreaId, chosen.Incarnation,
                overwriteChosen: false);
        cast.AbilityActor = state.Actor;
    }

    private static PhaseStep? ContinuationStep(
        World world, Card source, int stoppedAt, AbilityType? tier)
        => AbilityContinuationCodec.ContinuationStep(
            world.Agenda.Current, world.Agenda.Outstanding, source.ObjectId, stoppedAt, tier);

}

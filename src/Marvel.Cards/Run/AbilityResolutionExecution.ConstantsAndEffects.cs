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

internal sealed partial class AbilityResolutionExecution
{
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
    private static AbilityEffect.ChangeForm FormChangeOf(AbilityEffect node, AbilityResolutionState cast) =>
        (AbilityEffect.ChangeForm)node;

    private bool AlreadyInForm(AbilityEffect.ChangeForm change, AbilityResolutionState cast) =>
        AbilityAdmissionFacts.AlreadyInForm(
            cast.World, Seat(change.Player, cast), change.Form);

    /// <summary>"Exhaust …" — <c>rr:exhausted</c>.</summary>
    /// <remarks>
    /// A card already exhausted stays exhausted and reports nothing:
    /// <c>rr:exhausted</c> is a state and not a counter, so exhausting
    /// twice is not two exhaustions and must not be two events on the wire.
    /// </remarks>
    private bool CanDrawToPrintedHandSize(AbilityEffect node, AbilityResolutionState cast)
        => AbilityAdmissionFacts.CanDrawToPrintedHandSize(
            cast.World, cast.Source,
            Seat(EffectOf<AbilityEffect.DrawToHandSize>(node, cast).Player, cast));

    private static AbilityEffect.RemoveCounters CounterRemovalOf(AbilityEffect node, AbilityResolutionState cast) =>
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
    private static bool CanAdvanceMainScheme(AbilityResolutionState cast) =>
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
    private void Sequence(AbilityEffect node, AbilityResolutionState cast, int from)
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
    private void ForEach(AbilityEffect node, AbilityResolutionState cast)
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
    private void EachTime(AbilityEffect node, AbilityResolutionState cast)
        => ContinueEachTime((AbilityEffect.EachTime)node, cast, from: 0, count: null);

    private void ContinueEachTime(
        AbilityEffect.EachTime repeated, AbilityResolutionState cast, long from, long? count)
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

    private void RunCombinedForEach(RunCombinedForEach combined, AbilityResolutionState cast)
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
        AbilityStructuralTransition transition, AbilityResolutionState cast)
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

    private void RunStructuralLeaf(RunLeaf leaf, AbilityResolutionState cast)
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

    private void RunChild(
        AbilityEffect node, AbilityStructuralFrame frame, AbilityResolutionState cast)
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

    private int AbilityOrdinal(AbilityEffect node, AbilityResolutionState cast)
    {
        if (cast.AbilityOrdinal >= 0)
        {
            return cast.AbilityOrdinal;
        }

        return AbilityContinuationCodec.OrdinalForNode(
            program, cast.Source, cast.AbilityFace, cast.Tier,
            cast.StructuralPath, node);
    }

    private ImmutableArray<CompiledCardAbility> AbilitiesOn(Card source, string? face) =>
        AbilityContinuationCodec.AbilitiesOn(program, source, face);

    private void TrackResolution(AbilityResolutionState cast, CompiledCardAbility ability)
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

    private static void RestorePersisted(AbilityResolutionState cast, PhaseStep? continuation)
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
        AbilityResolutionState cast, IReadOnlyList<int>? discarded,
        IReadOnlyDictionary<string, long>? results)
    {
        ApplyRestored(cast, AbilityContinuationCodec.RestoreState(
            cast.World.Cards, discarded, results, -1, cast.Source.FaceId));
    }

    private static void ApplyRestored(AbilityResolutionState cast, RestoredContinuationState state)
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

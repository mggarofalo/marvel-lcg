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
        if (from == 0)
        {
            _ = CanInitiateSequence(node, cast);
        }

        var steps = OrderedEffects(node).ToList();
        bool outerContinuation = cast.HasContinuation;
        for (int step = from; step < steps.Count; step++)
        {
            cast.At(step);
            cast.SetContinuation(outerContinuation || step < steps.Count - 1);
            RunChild(steps[step], $"seq:{step}", cast);
            if (cast.Suspended)
            {
                return;
            }
        }
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
        var instruction = ForEachOf(node, cast);
        long count = NonNegativeForEachCount(ResolveAmount(instruction.Count, cast));
        if (count == 0)
        {
            return;
        }

        var effect = EffectBody(node);
        if (!Choices(effect).Any())
        {
            switch (instruction.Effect)
            {
                case AbilityEffect.Damage damage:
                    if (DamageTargets(damage.Cards, cast).Count != 1)
                    {
                        throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' has a for-each damage effect without "
                            + "choose and does not resolve to one target");
                    }
                    DealDamage(damage, effect, cast, count);
                    return;

                case AbilityEffect.RemoveThreat removal:
                    if (Every(removal.Schemes, cast).Count != 1)
                    {
                        throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' has a for-each threat-removal effect "
                            + "without choose and does not resolve to one target");
                    }
                    RemoveThreat(removal, cast, count);
                    return;
            }

            if (ContainsForEachTarget(effect))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a targeted for-each effect without choose "
                    + "whose one target cannot be persisted");
            }
        }

        bool outerContinuation = cast.HasContinuation;
        for (long iteration = 0; iteration < count; iteration++)
        {
            cast.SetContinuation(outerContinuation || iteration < count - 1);
            RunChild(effect, $"forEach:{iteration}:{count}", cast);
            if (cast.Suspended)
            {
                return;
            }
        }
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
    {
        var preceding = EachTimePreceding(node, cast);
        long requested = ResolveAmount(preceding.Count, cast);
        if (requested < 0)
        {
            throw new AbilityException("'eachTime' needs a non-negative discard count");
        }
        if (requested == 0)
        {
            return;
        }
        ValidateEachTimeBody(node, cast);

        var deck = cast.World.AreaOf(DeckType.EncounterDeck);
        var discard = cast.World.AreaOf(DeckType.EncounterDiscardPile);
        long available = deck.Cards.Count > 0 ? deck.Cards.Count : discard.Cards.Count;
        ContinueEachTime(node, cast, from: 0, Math.Min(requested, available));
    }

    private static AbilityEffect.ForEach ForEachOf(AbilityEffect node, Cast cast) =>
        (AbilityEffect.ForEach)node;

    private static AbilityEffect.EachTime EachTimeOf(AbilityEffect node, Cast cast) =>
        (AbilityEffect.EachTime)node;

    private static AbilityEffect.DiscardTop EachTimePreceding(AbilityEffect node, Cast cast)
    {
        if (EachTimeOf(node, cast).Effect is not AbilityEffect.DiscardTop
            { From: AbilitySearchArea.EncounterDeck, Players: null } preceding)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses each-time around an unsupported preceding effect");
        }
        return preceding;
    }

    private static void ValidateEachTimeBody(AbilityEffect node, Cast cast) =>
        AbilityInitiation.ValidateEachTimeBody(node, AdmissionContext(cast));

    private static void ContinueEachTime(
        AbilityEffect node, Cast cast, long from, long count)
    {
        var instruction = EachTimeOf(node, cast);
        bool outerContinuation = cast.HasContinuation;
        for (long iteration = from; iteration < count; iteration++)
        {
            var discarded = EncounterDeck.DiscardTop(
                cast.World, 1, cast.Trigger, cast.Events).SingleOrDefault();
            if (discarded is null)
            {
                break;
            }
            cast.Discarded.Add(discarded);
            cast.BindAlteration(discarded);

            if (!ResolveCondition(instruction.When, cast))
            {
                continue;
            }

            cast.SetContinuation(outerContinuation || iteration < count - 1);
            RunChild(
                EffectFollowing(node),
                $"eachTime:{iteration}:{count}:{discarded.ObjectId}",
                cast);
            if (cast.Suspended)
            {
                return;
            }
        }
        cast.SetContinuation(outerContinuation);
    }

    /// <summary>Whether a repeated effect names a game element it can affect.</summary>
    /// <remarks>
    /// The rulebook decides that a no-choice repetition keeps one target, but
    /// it does not supply a binding for the DSL. Direct damage and threat
    /// removal capture their single target by resolving once above. Other
    /// targeted shapes fail closed until their target can be persisted instead
    /// of running a fresh selector against a changed board.
    /// </remarks>
    private static bool ContainsForEachTarget(AbilityEffect node) =>
        AbilityInitiation.ContainsForEachTarget(node);

    private static void RunChild(AbilityEffect node, string frame, Cast cast)
    {
        cast.AbilityPath.Add(frame);
        try
        {
            Run(node, cast);
        }
        finally
        {
            cast.AbilityPath.RemoveAt(cast.AbilityPath.Count - 1);
        }
    }

    private static int AbilityOrdinal(AbilityEffect node, Cast cast)
    {
        if (cast.AbilityOrdinal >= 0)
        {
            return cast.AbilityOrdinal;
        }

        var runner = (AbilityRunner)cast.Abilities;
        var written = runner.AbilitiesOn(cast.Source, cast.AbilityFace)
            .Where(ability => cast.Tier is null || ability.Trigger.Timing == cast.Tier)
            .ToList();
        var matches = written
            .Select((ability, ordinal) => (Node: TryNodeAtPath(
                ability.Effect, cast.AbilityPath), ordinal))
            .Where(candidate => ReferenceEquals(candidate.Node, node))
            .Select(candidate => candidate.ordinal)
            .ToList();
        return matches.Count == 1
            ? matches[0]
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot identify the exact ability that suspended");
    }

    private ImmutableArray<CompiledCardAbility> AbilitiesOn(Card source, string? face) =>
        string.IsNullOrEmpty(face) ? On(source) : program.On(face);

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
        AbilitiesOn(source, face)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ElementAtOrDefault(ordinal)
        ?? throw new RulesNotImplementedException(
            $"'{source.FaceId}' has no '{tier}' ability {ordinal}");

    private static void RestorePersisted(Cast cast, PhaseStep? continuation)
    {
        if (continuation is not { } step)
        {
            return;
        }
        RestorePersisted(cast, step.Discarded, step.AbilityResults);
        cast.AbilityActor = step.AbilityActor >= 0
            ? cast.World.Cards[step.AbilityActor]
            : null;
    }

    private static void RestorePersisted(
        Cast cast, IReadOnlyList<int>? discarded,
        IReadOnlyDictionary<string, long>? results)
    {
        cast.Discarded.Clear();
        if (discarded is not null)
        {
            cast.Discarded.AddRange(discarded.Select(id => cast.World.Cards[id]));
        }
        foreach (var (name, value) in results
            ?? new Dictionary<string, long>(StringComparer.Ordinal))
        {
            if (name is PersistedChosen or PersistedChosenArea
                or PersistedChosenIncarnation or PersistedSourceIncarnation)
            {
                continue;
            }
            if (cast.RestoreCrisisIgnoringThwart(name, value))
            {
                continue;
            }
            cast.Results[name] = value;
        }
        cast.RestoreSourceIncarnation(
            results?.TryGetValue(PersistedSourceIncarnation, out long incarnation) == true
                ? checked((int)incarnation)
                : -1);
        RestorePersistedChosen(cast, results, overwrite: false);
    }

    private static void RestorePersistedChosen(
        Cast cast, IReadOnlyDictionary<string, long>? results, bool overwrite)
    {
        if (results?.TryGetValue(PersistedChosen, out long chosen) == true)
        {
            if (chosen < 0 || chosen >= cast.World.Cards.Count)
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has invalid persisted chosen-card metadata");
            }
            if (!results.TryGetValue(PersistedChosenArea, out long savedArea)
                || !results.TryGetValue(
                    PersistedChosenIncarnation, out long savedIncarnation))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has persisted chosen-card metadata "
                    + "without target provenance");
            }
            var card = cast.World.Cards[(int)chosen];
            cast.RestorePersistedSelection(
                card, checked((int)savedArea), checked((int)savedIncarnation),
                overwriteChosen: overwrite);
        }
    }

    private static void RestorePathBindings(Cast cast, IReadOnlyList<string> path)
    {
        var frame = path.LastOrDefault(candidate =>
            candidate.StartsWith("eachTime:", StringComparison.Ordinal));
        if (frame is null)
        {
            return;
        }
        var parts = frame.Split(':');
        cast.BindAlteration(cast.World.Cards[ParseEachTimeCard(parts, frame)]);
    }

    private static AbilityEffect? TryNodeAtPath(
        AbilityEffect root, IReadOnlyList<string> path)
    {
        try
        {
            return NodeAtPath(root, path);
        }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or InvalidOperationException
            or RulesNotImplementedException)
        {
            return null;
        }
    }

    private static PhaseStep? ContinuationStep(
        World world, Card source, int stoppedAt, AbilityType? tier)
    {
        bool Matches(PhaseStep step) => step.What == Steps.ChooseOption
            && step.Subject == source.ObjectId
            && step.Index == stoppedAt
            && step.Tier == tier;
        if (world.Agenda.Current is { } current && Matches(current))
        {
            return current;
        }
        for (int index = world.Agenda.Outstanding.Count - 1; index >= 0; index--)
        {
            if (Matches(world.Agenda.Outstanding[index]))
            {
                return world.Agenda.Outstanding[index];
            }
        }
        return null;
    }

    private static AbilityEffect NodeAtPath(
        AbilityEffect root, IReadOnlyList<string> path)
    {
        try
        {
            return NodeAtPathCore(root, path);
        }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or IndexOutOfRangeException
            or InvalidOperationException or InvalidCastException or FormatException)
        {
            throw new RulesNotImplementedException(
                $"ability continuation path '{string.Join("/", path)}' is invalid");
        }
    }

    private static AbilityEffect NodeAtPathCore(
        AbilityEffect root, IReadOnlyList<string> path, int offset = 0)
    {
        var node = root;
        for (int index = offset; index < path.Count; index++)
        {
            var parts = path[index].Split(':');
            node = parts[0] switch
            {
                "seq" => OrderedEffects(node).ElementAt(ParseIndex(parts, path[index])),
                "if" => ContinuationChild(node, parts[1]),
                "then" or "otherwise" => ContinuationChild(node, parts[1]),
                "defense" or "eachPlayer" or "forEach" =>
                    EffectBody(node),
                "eachTime" => EffectFollowing(node),
                "choice" when parts[1] == "option" =>
                    ((AbilityEffect.Choose)node).Options.ElementAt(ParseIndex(parts, path[index], 2)),
                "choice" when parts[1] == "effect" => EffectBody(node),
                "choice" when parts[1] == "otherwise" => EffectFollowing(node),
                "and" => OrderedEffects(node).ElementAt(ParseIndex(parts, path[index])),
                _ => throw new RulesNotImplementedException(
                    $"ability continuation frame '{path[index]}' is not implemented"),
            };
        }
        return node;
    }

    private static void ResumeAfter(
        AbilityEffect node, IReadOnlyList<string> path, Cast cast, int depth = 0,
        int stopBefore = -1)
    {
        try
        {
            ResumeAfterCore(node, path, cast, depth, stopBefore);
        }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or IndexOutOfRangeException
            or InvalidOperationException or InvalidCastException or FormatException)
        {
            throw new RulesNotImplementedException(
                $"ability continuation path '{string.Join("/", path)}' is invalid");
        }
    }

    private static void ResumeAfterCore(
        AbilityEffect node, IReadOnlyList<string> path, Cast cast, int depth = 0,
        int stopBefore = -1)
    {
        if (depth >= path.Count)
        {
            return;
        }

        string frame = path[depth];
        var parts = frame.Split(':');
        if (parts[0] == "eachTime")
        {
            cast.BindAlteration(cast.World.Cards[ParseEachTimeCard(parts, frame)]);
        }
        AbilityEffect child = parts[0] switch
        {
            "seq" => OrderedEffects(node).ElementAt(ParseIndex(parts, frame)),
            "if" => ContinuationChild(node, parts[1]),
            "then" or "otherwise" => ContinuationChild(node, parts[1]),
            "defense" or "eachPlayer" or "forEach" =>
                EffectBody(node),
            "eachTime" => EffectFollowing(node),
            "choice" when parts[1] == "option" =>
                ((AbilityEffect.Choose)node).Options.ElementAt(ParseIndex(parts, frame, 2)),
            "choice" when parts[1] == "effect" => EffectBody(node),
            "choice" when parts[1] == "otherwise" => EffectFollowing(node),
            "and" => OrderedEffects(node).ElementAt(ParseIndex(parts, frame)),
            _ => throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' is not implemented"),
        };

        bool inheritedContinuation = cast.HasContinuation;
        cast.SetContinuation(
            inheritedContinuation || HasRemainingAtFrame(node, parts, frame));
        ResumeAfterCore(child, path, cast, depth + 1, stopBefore);
        if (cast.Suspended || depth <= stopBefore)
        {
            return;
        }

        cast.SetContinuation(inheritedContinuation);
        cast.SetAbilityPath(path.Take(depth));
        switch (parts[0])
        {
            case "seq":
                var steps = OrderedEffects(node).ToList();
                bool outerContinuation = cast.HasContinuation;
                for (int index = ParseIndex(parts, frame) + 1; index < steps.Count; index++)
                {
                    cast.At(index);
                    cast.SetContinuation(outerContinuation || index < steps.Count - 1);
                    RunChild(steps[index], $"seq:{index}", cast);
                    if (cast.Suspended)
                    {
                        return;
                    }
                }
                cast.SetContinuation(outerContinuation);
                break;

            case "then" when parts[1] == "effect":
            case "otherwise" when parts[1] == "effect":
                if (parts.Length < 3
                    || !Enum.TryParse(parts[2], out ResolutionOutcome outcome))
                {
                    throw new RulesNotImplementedException(
                        $"ability continuation frame '{frame}' has no resolution outcome");
                }
                var required = parts[0] == "then"
                    ? ResolutionOutcome.Full
                    : ResolutionOutcome.None;
                if (outcome == required)
                {
                    RunChild(ContinuationChild(node, parts[0]), $"{parts[0]}:{parts[0]}", cast);
                }
                break;

            case "and":
                var effects = OrderedEffects(node).ToList();
                var remaining = ValidRemaining(node, parts, frame);
                var completed = Completed(parts, frame);
                completed.Add(ParseIndex(parts, frame));
                bool outerAndContinuation = cast.HasContinuation;
                for (int position = 0; position < remaining.Count; position++)
                {
                    int index = remaining[position];
                    string after = string.Join(',', remaining.Skip(position + 1));
                    string before = string.Join(',', completed.Concat(remaining.Take(position)));
                    cast.SetContinuation(
                        outerAndContinuation || position < remaining.Count - 1);
                    RunChild(effects[index], $"and:{index}:{after}:{before}", cast);
                    if (cast.Suspended)
                    {
                        return;
                    }
                }
                cast.SetContinuation(outerAndContinuation);
                break;

            case "eachPlayer":
                if (cast.AbilityPlayer >= 0)
                {
                    cast.RestorePlayer(cast.AbilityPlayer);
                }
                break;

            case "forEach":
                long count = ParseForEachCount(parts, frame);
                long completedIteration = ParseIndex(parts, frame);
                var repeated = EffectBody(node);
                bool outerForEachContinuation = cast.HasContinuation;
                for (long iteration = completedIteration + 1; iteration < count; iteration++)
                {
                    cast.SetContinuation(
                        outerForEachContinuation || iteration < count - 1);
                    RunChild(repeated, $"forEach:{iteration}:{count}", cast);
                    if (cast.Suspended)
                    {
                        return;
                    }
                }
                cast.SetContinuation(outerForEachContinuation);
                break;

            case "eachTime":
                ContinueEachTime(
                    node, cast,
                    from: ParseIndex(parts, frame) + 1,
                    count: ParseForEachCount(parts, frame));
                break;
        }
    }

    private static bool HasRemainingAtFrame(
        AbilityEffect node, string[] parts, string frame)
    {
        return parts[0] switch
        {
            "seq" => ParseIndex(parts, frame) < OrderedEffects(node).Length - 1,
            "and" => ValidRemaining(node, parts, frame).Count > 0,
            "forEach" => ParseIndex(parts, frame) + 1
                < ParseForEachCount(parts, frame),
            "eachTime" => ParseIndex(parts, frame) + 1
                < ParseForEachCount(parts, frame),
            "then" when parts[1] == "effect" => DependentContinues(parts, frame, true),
            "otherwise" when parts[1] == "effect" =>
                DependentContinues(parts, frame, false),
            _ => false,
        };
    }

    private static long ParseForEachCount(string[] parts, string frame)
    {
        if (parts.Length < 3
            || !long.TryParse(
                parts[2], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out long count)
            || count < 0)
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no iteration count");
        }
        return count;
    }

    private static int ParseEachTimeCard(string[] parts, string frame)
    {
        if (parts.Length < 4
            || !int.TryParse(
                parts[3], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out int card)
            || card < 0)
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no bound card");
        }
        return card;
    }

    private static bool DependentContinues(string[] parts, string frame, bool onFull)
    {
        if (parts.Length < 3
            || !Enum.TryParse(parts[2], out ResolutionOutcome outcome))
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no resolution outcome");
        }
        return outcome == (onFull ? ResolutionOutcome.Full : ResolutionOutcome.None);
    }

    private static List<int> ValidRemaining(
        AbilityEffect node, string[] parts, string frame)
    {
        var effects = OrderedEffects(node).ToList();
        var remaining = Remaining(parts, frame);
        var completed = Completed(parts, frame);
        var completeOrder = completed
            .Append(ParseIndex(parts, frame))
            .Concat(remaining)
            .ToList();
        if (completeOrder.Count != effects.Count
            || completeOrder.Distinct().Count() != effects.Count
            || completeOrder.Any(index => index < 0 || index >= effects.Count))
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has an invalid remaining order");
        }
        return remaining;
    }

    private static List<int> Remaining(string[] parts, string frame)
        => OrderPart(parts, 2, frame);

    private static List<int> Completed(string[] parts, string frame)
    {
        if (parts.Length < 4)
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no completed order");
        }
        return OrderPart(parts, 3, frame);
    }

    private static List<int> OrderPart(string[] parts, int position, string frame)
    {
        if (parts.Length <= position || string.IsNullOrEmpty(parts[position]))
        {
            return [];
        }
        try
        {
            return parts[position].Split(',').Select(value => int.Parse(
                value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        }
        catch (Exception error) when (error is FormatException or OverflowException)
        {
            throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has an invalid remaining order");
        }
    }

    private static int ParseIndex(string[] parts, string frame, int position = 1) =>
        parts.Length > position
        && int.TryParse(
            parts[position], System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no valid index");

}

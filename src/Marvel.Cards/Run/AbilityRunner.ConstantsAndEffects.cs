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
        var target = Find(card, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would grant to a card that is not there");
        EnsureLastingPeriodOpen(until, cast);
        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: kind,
            Amount: Amount(amount, cast),
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
        var target = Find(delayed.Card, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would delay a discard of a card that is not there");

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.DiscardFromPlay,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.NextTime(delayed.Condition)));
    }

    private static void Discard(AbilityCardSelection selector, Cast cast)
    {
        if (Find(selector, cast) is { } target)
        {
            // rr:target.2 lets a multi-target ability initiate when at least
            // one target is valid. A different component can therefore have
            // an invalid target and simply does not resolve against it.
            if (CanRemoveByEffect(selector, cast, target))
            {
                Rules.Play.Discard.CardFromEffect(
                    cast.World, cast.World.Facts, cast.Source, target,
                    cast.Trigger, cast.Events);
            }
        }
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
        Forms.In(cast.World, cast.World.Seats[Seat(change.Player, cast)],
            cast.World.Facts, change.Form);

    /// <summary>"Remove … from the game" — <c>rr:removed-from-the-game</c>.</summary>
    /// <remarks>
    /// Removed and not discarded: <c>rr:defeat.2</c> keeps the two apart, and a
    /// card in the discard pile can come back where one out of the game cannot.
    /// </remarks>
    private static void RemoveFromGame(AbilityCardSelection selection, Cast cast)
    {
        var card = Find(selection, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would remove a card that is not there");

        if (!CanRemoveByEffect(selection, cast, card))
        {
            // Another component can make a multi-target effect valid under
            // rr:target.3.4; this invalid component simply does not resolve.
            return;
        }

        var from = card.Area;
        var removed = cast.World.AreaOf(DeckType.RemovedArea);
        var constantsEnding = cast.World.Effects.PreflightConstantsEnding(card);
        using var departure = constantsEnding.Begin();
        if (DeckTypes.IsInPlay(from.Type))
        {
            Rules.Play.Discard.Attachments(
                cast.World, card, cast.Trigger, cast.Events);
            Rules.Play.Discard.ResetLeavingState(
                cast.World, card, cast.Trigger, cast.Events);
        }
        World.MoveToTop(card, removed);
        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(removed),
            [new Landing(card.ObjectId, removed.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Remove_From_Game",
        });
        constantsEnding.Complete(cast.Trigger, cast.Events);
    }

    /// <summary>
    /// "Place it here instead" — <c>rr:replacement-effect</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The damage does not happen to the character at all: it is <i>placed</i>
    /// on this card as damage tokens, which is why it goes on with
    /// <c>Card.TakeDamage</c> rather than through <c>Damage.Deal</c>. Dealing it
    /// would start the nine steps of <c>rr:damage</c> again, on a card that is
    /// not a character.
    /// </para>
    /// <para>
    /// What is left afterwards is zero, and <c>rr:replacement-effect.1</c> then
    /// holds for free: the damage is no longer imminent, so nothing later in
    /// the order can respond to it.
    /// </para>
    /// </remarks>
    private static void Soak(AbilityCardSelection card, Cast cast)
    {
        var onto = Find(card, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would soak damage onto a card that is not there");

        long before = onto.Damage;
        onto.TakeDamage(cast.Incoming);
        cast.Events.Add(new FieldSet(onto.ObjectId, "k_damage", before, onto.Damage)
        {
            Trigger = cast.Trigger, Verb = "Place_Damage",
        });

        cast.Replace(0);
    }

    /// <summary>"Exhaust …" — <c>rr:exhausted</c>.</summary>
    /// <remarks>
    /// A card already exhausted stays exhausted and reports nothing:
    /// <c>rr:exhausted</c> is a state and not a counter, so exhausting
    /// twice is not two exhaustions and must not be two events on the wire.
    /// </remarks>
    private static void Exhaust(AbilityCardSelection cards, Cast cast)
    {
        foreach (var target in Every(cards, cast))
        {
            Exhaust(target, cast);
        }
    }

    private static void Exhaust(Card target, Cast cast)
    {
        if (!target.Ready) return;
        target.Exhaust();
        cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = cast.Trigger, Verb = "Exhaust",
        });
    }

    private static void Ready(AbilityCardSelection cards, Cast cast)
    {
        foreach (var target in Every(cards, cast).Where(target =>
            !target.Ready
            && cast.Abilities.CanReady(cast.World, target, cast.Source)))
        {
            target.Refresh();
            cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 1, 0)
            {
                Trigger = cast.Trigger, Verb = "Ready",
            });
        }
    }

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
    {
        int player = Seat(EffectOf<AbilityEffect.DrawToHandSize>(node, cast).Player, cast);
        var seat = cast.World.Seats[player];
        return HandCountDuringEvent(cast, seat) < cast.World.Facts.PrintedValue(
            seat.IdentityCard.FaceId, "HS", cast.World.Players);
    }

    private static int HandCountDuringEvent(Cast cast, Seat seat) =>
        seat.Hand.Cards.Count - (cast.Source.Area == seat.Hand
            && cast.World.Facts.Kind(cast.Source.FaceId) == CardKind.Event ? 1 : 0);

    private static void RemoveCounters(AbilityEffect.RemoveCounters removal, Cast cast)
    {
        var card = Find(removal.Card, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the card paying its counter cost");
        RemoveCounters(card, removal.Counter, removal.Count, cast);
    }

    private static void RemoveCounters(Card card, string type, long count, Cast cast)
    {
        string key = CounterKeyForRemoval(card, type, count)
            ?? throw new RulesNotImplementedException(
                $"'{card.FaceId}' has fewer than {count} {type} counters");
        long before = card.Tokens.GetValueOrDefault(key);

        card.PlaceTokens(key, -count);
        cast.Events.Add(new FieldSet(
            card.ObjectId, key, before, before - count)
        {
            Trigger = cast.Trigger, Verb = "Remove_Counter",
        });

        if (CounterCount(card, "allPurpose") == 0
            && !Characteristics.IsLost(cast.World, card, "uses")
            && cast.Abilities.CounterPool(cast.World, card)?.Uses == true)
        {
            if (!Defeat.ToVictoryDisplay(
                    cast.World, cast.World.Facts, card,
                    cast.Trigger, cast.Events))
            {
                Rules.Play.Discard.Card(
                    cast.World, card, cast.Trigger, cast.Events);
            }
        }
    }

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
        cast.World.TheCardIn(DeckType.MainSchemesArea) is not null
            && cast.World.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0;

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
        string.Equals(type, "allPurpose", StringComparison.Ordinal)
            ? card.Tokens
                .Where(pair => pair.Key.StartsWith("c_", StringComparison.Ordinal))
                .Sum(pair => pair.Value)
            : card.Tokens.GetValueOrDefault("c_" + type);

    /// <summary>Resolves the physical counter removed by a cost.</summary>
    /// <remarks>
    /// If more than one typed pool is present, the rule permits the player to
    /// choose either one. The current action protocol has no counter-choice
    /// affordance, so resolution raises before changing state rather than
    /// choosing an outcome on the player's behalf.
    /// </remarks>
    private static string? CounterKeyForRemoval(Card card, string type, long count)
    {
        if (!string.Equals(type, "allPurpose", StringComparison.Ordinal))
        {
            string typed = "c_" + type;
            return card.Tokens.GetValueOrDefault(typed) >= count ? typed : null;
        }

        string[] pools = [.. card.Tokens
            .Where(pair => pair.Value > 0
                && pair.Key.StartsWith("c_", StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .Order(StringComparer.Ordinal)];
        return pools.Length switch
        {
            0 => null,
            1 when card.Tokens[pools[0]] >= count => pools[0],
            1 => null,
            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' must choose which all-purpose counter to remove"),
        };
    }

    private static void PreventDamage(AbilityEffect.PreventDamage prevention, Cast cast)
    {
        int target = cast.Occurrence.Target >= 0
            ? cast.Occurrence.Target
            : cast.Occurrence.Subject;
        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "preventDamage",
            Amount: Amount(prevention.Amount, cast),
            Card: cast.Source.ObjectId,
            Affects: target,
            Lasts: new Duration(Uses: 1)));
    }

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
    /// "Reveal the top card of the encounter deck" — <c>rr:reveal</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Revealed, not dealt.</b> <c>rr:deal-deal-an-encounter-card</c> puts a
    /// card facedown in a queue to be resolved later; this one is turned over
    /// now. The difference is a whole villain phase, and Under Fire says
    /// "reveal".
    /// </para>
    /// <para>
    /// Scheduled, for the same reason <c>search</c> schedules: revealing an
    /// encounter card is a step with an interrupt window and a response window
    /// around it, and the card revealed may itself ask a player something.
    /// </para>
    /// <para>
    /// <c>EncounterDeck.TakeTop</c> is what draws it, so an empty deck
    /// reshuffles its discard pile first — <c>rr:encounter-deck.3</c> — rather
    /// than this quietly doing nothing.
    /// </para>
    /// </remarks>
    private static Card? TopOfTheEncounterDeck(Cast cast) =>
        EncounterDeck.TakeTop(cast.World, cast.Trigger, cast.Events);

    /// <summary>Reveals one card, wherever it was.</summary>
    /// <remarks>
    /// <b>The card moves now and resolves later.</b> It goes to the revealing
    /// area at once, so a later step of the same ability cannot find it where
    /// it was — Shadow of the Past reveals two cards out of a pile and then
    /// shuffles "the rest" of that pile away, and a reveal that only scheduled
    /// would shuffle the two it had just chosen.
    /// </remarks>
    private static void RevealCard(Card? card, Cast cast)
    {
        if (!ScheduleReveal(card, cast))
        {
            return;
        }

        cast.ResolveEffect();
    }

    /// <summary>Moves one card into the reveal procedure and schedules it.</summary>
    private static bool ScheduleReveal(Card? card, Cast cast)
    {
        if (card is null)
        {
            return false;
        }

        var from = card.Area;
        var revealing = cast.World.AreaOf(DeckType.RevealingArea);
        World.MoveToTop(card, revealing);
        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(revealing),
            [new Landing(card.ObjectId, revealing.Cards.Count - 1)])
        {
            Trigger = cast.Trigger,
            Verb = "Reveal",
        });
        // Scenario setup has no active player's ability to inherit. The first
        // player resolves cards revealed by that mandatory setup instruction;
        // ordinary in-game abilities retain their occurrence's player.
        int revealingPlayer = cast.Player >= 0 ? cast.Player : cast.World.FirstPlayer;
        cast.World.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard,
            cast.World.Agenda.Current?.Round ?? 0,
            4,
            Index: revealingPlayer,
            Subject: card.ObjectId,
            Seat: revealingPlayer));
        return true;
    }

    /// <summary>
    /// "Shuffle the rest of … into the encounter deck" — <c>rr:shuffle</c>.
    /// </summary>
    /// <remarks>
    /// The cards move in the order the query answers and the deck is shuffled
    /// once afterwards, not once per card. The shuffle draws from the game's
    /// single random stream, so how many times it happens is a wire fact and
    /// not a detail.
    /// </remarks>
    private static void ShuffleInto(AbilityEffect.ShuffleInto shuffle, Cast cast)
    {
        var deck = Area(shuffle.Deck, cast);
        bool applied = false;
        foreach (var card in Every(shuffle.Cards, cast))
        {
            var from = card.Area;
            World.MoveToTop(card, deck);
            cast.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(deck),
                [new Landing(card.ObjectId, deck.Cards.Count - 1)])
            {
                Trigger = cast.Trigger, Verb = "Shuffle_Into",
            });
            applied = true;
        }

        applied |= cast.World.Shuffle(deck);
        if (applied)
        {
            cast.ResolveEffect();
        }
    }

    /// <summary>
    /// "Search the encounter deck and discard pile for … and reveal it" —
    /// <c>rr:search</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:search.2</c> — "cards being searched are not considered to leave
    /// the searched area" — so looking costs nothing and only the card found
    /// moves.
    /// </para>
    /// <para>
    /// <b>The reveal is scheduled, not done here.</b> Revealing an encounter
    /// card is a step with an interrupt window and a response window around it,
    /// and a reveal called inline would have neither. The step is the same one
    /// the villain phase uses, so the card found goes through
    /// <c>rr:reveal</c>'s four steps exactly as a dealt card does.
    /// </para>
    /// <para>
    /// <c>rr:search.3</c> — "if any portion of a deck is searched, upon
    /// completion of that game step, game function, or card ability, shuffle
    /// that entire deck." Taken as the ability completing, which is this method
    /// returning; the reveal it scheduled happens afterwards. Nothing in the
    /// pool that is reached this way reads the encounter deck, so the two
    /// readings agree on every board that exists — but this is the one written
    /// down.
    /// </para>
    /// <para>
    /// <c>rr:search.1</c> gives the player the choice when several cards match.
    /// That is a second suspension inside an ability that may already have one,
    /// so it is refused by name until a card needs it.
    /// </para>
    /// </remarks>
    private static void Search(AbilityEffect.Search search, Cast cast)
    {
        string wanted = search.Face;
        var areas = search.Areas.Select(where => Area(where, cast)).ToList();

        var found = areas
            .SelectMany(area => area.Cards)
            .Where(card => string.Equals(card.FaceId, wanted, StringComparison.Ordinal))
            .ToList();

        if (found.Count > 1)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searched and found {found.Count} copies of "
                + $"'{wanted}'; rr:search.1 gives the player that choice and asking is "
                + "not implemented");
        }

        // The identities inspected by a search never enter the public event
        // wire. The resolution still records that knowledge was acquired even
        // when no card matches and a one-card deck consumes no shuffle RNG.
        cast.World.RecordInformation(InformationKind.Search);

        // The found card is added to the revealing area before the searched
        // deck is shuffled. `rr:search` says the found card is added to the
        // indicated area, and the shuffle therefore applies to the cards that
        // remain rather than consuming the wire-format RNG with that card
        // still in its old area.
        bool applied = found.Count == 1 && ScheduleReveal(found[0], cast);

        cast.Results["found"] = found.Count;

        // `rr:search.3`. The discard pile is not a deck and is not shuffled --
        // and shuffling one would consume from the game's single random stream,
        // which is a wire format.
        foreach (var deck in areas.Where(area => area.Type == DeckType.EncounterDeck))
        {
            applied |= cast.World.Shuffle(deck);
        }
        if (applied)
        {
            cast.ResolveEffect();
        }
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
        long count = ForEachCount(node, cast);
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
        long requested = Amount(preceding.Count, cast);
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

    private static void ValidateEachTimeBody(AbilityEffect node, Cast cast)
    {
        if (ContainsUnreconstructibleAfterActivation(
            EffectFollowing(node), cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside an after-activation effect, "
                + "which cannot be reconstructed");
        }
    }

    private static bool ContainsUnreconstructibleAfterActivation(
        AbilityEffect node, Cast cast)
    {
        if (node.OperationName() == "afterActivation")
        {
            return DelayedNeedsContinuationAddress(
                EffectBody(node), cast, hasContinuation: false);
        }
        return ContinuationChildren(node).Any(child =>
            ContainsUnreconstructibleAfterActivation(child, cast));
    }

    private static bool DelayedNeedsContinuationAddress(
        AbilityEffect node, Cast cast, bool hasContinuation)
    {
        if (node.OperationName() == "afterActivation"
            || node.OperationName() == "and" && OrderedEffects(node).Skip(1).Any()
            || IsChoice(node)
            || node.OperationName() is "eachPlayer" or "attack" or "thwart" or "thwartSchemes")
        {
            return true;
        }
        if (node.OperationName() is "placeThreat" or "enemyAttacks" or "enemySchemes")
        {
            return hasContinuation;
        }
        if (node.OperationName() is "seq" or "and")
        {
            var children = OrderedEffects(node).ToList();
            return children.Select((child, index) => (child, index)).Any(entry =>
                DelayedNeedsContinuationAddress(
                    entry.child, cast,
                    hasContinuation || entry.index < children.Count - 1));
        }
        if (node.OperationName() == "if")
        {
            return ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(branch => branch is not null)
                .Any(branch => DelayedNeedsContinuationAddress(
                    branch, cast, hasContinuation));
        }
        if (node.OperationName() is "then" or "otherwise")
        {
            return DelayedNeedsContinuationAddress(
                    EffectBody(node), cast, hasContinuation: true)
                || DelayedNeedsContinuationAddress(
                    EffectFollowing(node), cast, hasContinuation);
        }
        if (node.OperationName() == "forEach")
        {
            if (AmountMayChange(ForEachOf(node, cast).Count))
            {
                return DelayedNeedsContinuationAddress(
                    EffectBody(node), cast, hasContinuation: true);
            }
            long count = ForEachCount(node, cast);
            return count > 0 && DelayedNeedsContinuationAddress(
                EffectBody(node), cast,
                hasContinuation || count > 1);
        }
        if (node.OperationName() == "eachTime")
        {
            if (EachTimeOf(node, cast).Effect is not AbilityEffect.DiscardTop
                { From: AbilitySearchArea.EncounterDeck, Players: null } preceding)
            {
                return true;
            }

            var requested = preceding.Count;
            if (AmountMayChange(requested))
            {
                return true;
            }
            long count = Amount(requested, cast);
            if (count < 0)
            {
                throw new AbilityException("'eachTime' needs a non-negative discard count");
            }
            if (count == 0)
            {
                return false;
            }
            return DelayedNeedsContinuationAddress(
                EffectFollowing(node), cast,
                hasContinuation || count > 1);
        }
        return ContinuationChildren(node).Any(child =>
            DelayedNeedsContinuationAddress(child, cast, hasContinuation));
    }

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

            if (!Test(instruction.When, cast))
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
        node is AbilityEffect.DelayedStun
        || node.OperationName() is "removeFromGame" or "exhaust" or "ready" or "reveal"
            or "returnToHand" or "returnOwnedToHand" or "soakDamage"
            or "addToHand" or "giveStatus" or "attachTo" or "grantUntil"
            or "discard" or "heal" or "placeCounters" or "shuffleInto" or "search"
            or "indirectDamage" or "dealDamage" or "moveDamage"
            or "dealAttackDamage" or "moveAttackDamage" or "placeThreat"
            or "removeThreat" or "replaceThreatWithDamage" or "enemyAttacks"
            or "enemySchemes" or "putIntoPlay" or "placeAtRandom" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice"
        || ContinuationChildren(node).Any(ContainsForEachTarget);

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

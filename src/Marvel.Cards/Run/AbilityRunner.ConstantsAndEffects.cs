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
    private static void Grants(AbilityNode node, Cast cast, List<ContinuousEffect> found)
    {
        switch (node.Kind)
        {
            case "seq":
            case "and":
                foreach (var step in Nodes(node.Argument))
                {
                    Grants(step, cast, found);
                }

                break;

            case "if":
                string branch = Test(Tree(node.Require("test")), cast) ? "then" : "else";
                if (node.Field(branch) is { } taken)
                {
                    Grants(Tree(taken), cast, found);
                }

                break;

            case "grant":
                if (Find(node.Require("card"), cast) is { } grantTarget)
                {
                    found.Add(Grant(node, cast, grantTarget));
                }
                else if (Word(node.Require("card")) is not ("yourHero" or "yourAlterEgo"))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' card {cast.Source.ObjectId} in "
                        + $"{cast.Source.Area.Type} hosted by {cast.Source.Area.Host} would grant "
                        + "to a card that is not there");
                }
                break;

            case "grantEach":
                foreach (var target in Every(node.Require("cards"), cast))
                {
                    found.Add(Grant(node, cast, target));
                }
                break;

            case "preventThreatRemoval":
                // A prohibition is answered by `CanRemoveThreat`; it is not a
                // numeric modifier and therefore contributes no effect here.
                break;

            case "doubleResourceFor":
                // This constant acts while its resource card is spent from
                // hand. `ResourcesGeneratedBy` reads it with the payment's
                // target card, which is context this general effect list does
                // not carry.
                break;

            case "requireAllyDefender":
                // Defender declaration carries the attack and its engaged
                // player; `Defenders` reads this constraint in that context.
                break;

            case "preventDamageFrom":
            case "preventDamageWhile":
                // Damage carries both source and target. `CanTakeDamage`
                // evaluates these prohibitions in that complete context.
                break;

            case "preventReady":
                // The card to be readied and the source of that instruction
                // are available only when `CanReady` asks the question.
                break;

            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' has a constant ability using the node "
                    + $"'{node.Kind}', which a constant ability cannot be written with");
        }
    }

    private static bool ProhibitsThreatRemoval(AbilityNode node, Cast cast, Card scheme)
    {
        return node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument).Any(step =>
                ProhibitsThreatRemoval(step, cast, scheme)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch && ProhibitsThreatRemoval(Tree(branch), cast, scheme),
            "preventThreatRemoval" => Find(node.Argument, cast)?.ObjectId == scheme.ObjectId,
            _ => false,
        };
    }

    /// <summary>One keyword a constant ability gives something.</summary>
    /// <remarks>
    /// <para>
    /// <b>The amount defaults to one, not to zero.</b> "Unus gains retaliate 1"
    /// states its number and "Unus also gains stalwart" does not, because a
    /// keyword without one is simply present — and the engine asks whether a
    /// card is stalwart by reading the field and comparing it to zero
    /// (<c>Statuses</c>, <c>rr:stalwart.1</c>). A grant defaulting to zero would
    /// parse, register, and mean the opposite of what the card says.
    /// </para>
    /// <para>
    /// The keyword is held against the fields the engine actually reads, for
    /// the reason the whole dataset is: <c>stallwart</c> would otherwise sit in
    /// the file looking implemented and grant nothing for ever.
    /// </para>
    /// </remarks>
    private static ContinuousEffect Grant(AbilityNode node, Cast cast)
    {
        var target = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would grant to a card that is not there");

        return Grant(node, cast, target);
    }

    private static ContinuousEffect Grant(AbilityNode node, Cast cast, Card target)
    {

        // `rr:traits.1` -- "traits have no inherent effects on the game.
        // Instead, some card abilities reference cards that possess or lack
        // specific traits." So a granted trait carries no amount and is not
        // held against the printed fields: it is a name other cards ask about,
        // and the pool spells one in capitals.
        if (node.Field("trait") is { } gained)
        {
            return new ContinuousEffect(
                EffectSource.ConstantAbility,
                Kind: Rules.State.Traits.Granted + Word(gained),
                Card: cast.Source.ObjectId,
                Affects: target.ObjectId,
                Lasts: Duration.WhileInPlay);
        }

        string keyword = Word(node.Require("keyword"));
        if (!StateFields.IsModifiable(keyword) && !Keywords.Granted.Contains(keyword))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' grants '{keyword}', which is not a keyword or "
                + "field the engine reads modifiers into");
        }

        return new ContinuousEffect(
            EffectSource.ConstantAbility,
            Kind: keyword,
            Amount: node.Field("amount") is { } amount ? Amount(amount, cast) : 1,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.WhileInPlay);
    }

    // `rr:lasting-effects` -- an effect "for a specified duration (such as
    // [...] 'until the end of this attack')".
    private static void GrantUntil(AbilityNode node, Cast cast)
    {
        var target = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would grant to a card that is not there");

        if (node.Field("trait") is { } gained)
        {
            EnsureLastingPeriodOpen(node, cast);
            cast.World.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: Rules.State.Traits.Granted + Word(gained),
                Amount: 1,
                Card: cast.Source.ObjectId,
                Affects: target.ObjectId,
                Lasts: Duration.UntilEndOf(Word(node.Require("until")))));
            return;
        }

        // Held against the fields the engine actually reads, exactly as a
        // constant ability's grant is: an unrecognised name would register
        // happily, expire on time, and modify nothing in between. "+2 SCH" is
        // the same mechanism as "gains overkill" and reaches it through the
        // same door, which is why `scheme` sits in this vocabulary beside
        // `overkill`.
        string keyword = Word(node.Require("keyword"));
        if (!StateFields.IsModifiable(keyword) && !Keywords.Granted.Contains(keyword))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' grants '{keyword}' for a duration, which is not a "
                + "keyword or field the engine reads modifiers into");
        }

        EnsureLastingPeriodOpen(node, cast);

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: keyword,
            Amount: node.Field("amount") is { } amount ? Amount(amount, cast) : 0,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.UntilEndOf(Word(node.Require("until")))));

        if (string.Equals(keyword, "stalwart", StringComparison.Ordinal))
        {
            Statuses.RemoveAfflictionsIfStalwart(
                cast.World, cast.World.Facts, target, cast.Trigger, cast.Events);
        }
    }

    private static void EnsureLastingPeriodOpen(AbilityNode node, Cast cast)
    {
        if (!LastingPeriodIsOpen(node, cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' begins a lasting effect outside its named period");
        }
    }

    // `rr:delayed-effect.1` -- an effect that resolves "after their specified
    // timing point or future condition occurs or becomes true".
    private static void DelayUntil(AbilityNode node, Cast cast)
    {
        var effect = Tree(node.Require("effect"));

        // "If a character is damaged by this attack, that character is
        // stunned." **The card it acts on does not exist yet** -- the attack
        // has not happened, so there is nobody to name. `Affects` stays null
        // and the occurrence names the card when the effect comes due.
        if (effect.Kind == "giveStatus"
            && Word(effect.Require("card")) == "damaged"
            && Word(effect.Require("status")) == Statuses.Stunned)
        {
            // **Bounded by the attack as well as by the condition.** "If a
            // character is damaged by **this attack**" is false once the attack
            // is over, so an attack that damaged nobody -- `rr:tough.3`, a
            // tough status card ate it -- must not leave the effect waiting for
            // somebody else's. `Duration` carries both: the next time damage is
            // dealt, and not past the end of this attack.
            cast.World.Effects.Register(new ContinuousEffect(
                EffectSource.DelayedEffect,
                Kind: DelayedEffects.StunTheSubject,
                Card: cast.Source.ObjectId,
                Affects: null,
                Lasts: new Duration(
                    Until: node.Field("within") is { } bound ? Word(bound) : null,
                    OnCondition: Word(node.Require("condition")),
                    Uses: 1)));
            return;
        }

        if (effect.Kind != "discard")
        {
            // A delayed effect is data on the board, not a closure, so what it
            // will do has to be a `Kind` the engine can read back after a save.
            // `DelayedEffects` knows one; the rest is the vocabulary that grows.
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' delays '{effect.Kind}', and only 'discard' can be "
                + "written down as a delayed effect");
        }

        var target = Find(effect.Field("card") ?? effect.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would delay a discard of a card that is not there");

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.DiscardFromPlay,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.NextTime(Word(node.Require("condition")))));
    }

    private static void Discard(AbilityNode node, Cast cast)
    {
        var selector = node.Field("card") ?? node.Argument;
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

    /// <summary>Whether this exact selector may make its current target depart.</summary>
    private static bool CanRemoveByEffect(
        AbilityValue selector, Cast cast, Card target)
    {
        // `rr:in-play-and-out-of-play.4`: a retained binding does not authorize
        // a later component to follow a card into an out-of-play area. Cards
        // resolving in the boost/reveal/processing staging areas remain live
        // ability subjects; every other out-of-play target must be found by a
        // selector that expressly names its current area.
        bool reachable = DeckTypes.IsInPlay(target.Area.Type)
            || target.Area.Type is DeckType.BoostingArea
                or DeckType.ProcessingArea
                or DeckType.RevealingArea
            || selector is AbilityValue.Word { Value: "chosen" }
                && cast.WasSelectedInCurrentArea(target)
            || ExplicitlySelectsOutOfPlayCard(selector, cast, target);
        bool bindingIsCurrent = selector switch
        {
            AbilityValue.Word { Value: "this" } =>
                cast.SourceBindingIsCurrent(target),
            AbilityValue.Word { Value: "chosen" } =>
                cast.ChosenBindingIsCurrent(target),
            _ => true,
        };
        return reachable && bindingIsCurrent
            && Rules.Play.Discard.EffectCanRemove(
                cast.World, cast.World.Facts, cast.Source, target);
    }

    /// <summary>Whether a selector names the out-of-play area holding one card.</summary>
    private static bool ExplicitlySelectsOutOfPlayCard(
        AbilityValue value, Cast cast, Card target)
    {
        if (value is AbilityValue.Map map)
        {
            if (map.Entries.Count == 1)
            {
                var (kind, argument) = map.Entries.First();
                if (kind == "cardsIn"
                    && CardsIn(new AbilityNode(kind, argument), cast)
                        .Any(card => card.ObjectId == target.ObjectId))
                {
                    return true;
                }
            }
            return map.Entries.Values.Any(child =>
                ExplicitlySelectsOutOfPlayCard(child, cast, target));
        }

        return value is AbilityValue.List list
            && list.Values.Any(child =>
                ExplicitlySelectsOutOfPlayCard(child, cast, target));
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
    private static void ChangeForm(AbilityNode node, Cast cast)
    {
        var seat = cast.World.Seats[Seat(node.Require("player"), cast)];
        string form = Word(node.Require("to"));
        if (Forms.In(cast.World, seat, cast.World.Facts, form))
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

    /// <summary>"Remove … from the game" — <c>rr:removed-from-the-game</c>.</summary>
    /// <remarks>
    /// Removed and not discarded: <c>rr:defeat.2</c> keeps the two apart, and a
    /// card in the discard pile can come back where one out of the game cannot.
    /// </remarks>
    private static void RemoveFromGame(AbilityNode node, Cast cast)
    {
        var card = Find(node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would remove a card that is not there");

        if (!CanRemoveByEffect(node.Argument, cast, card))
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
    private static void Soak(AbilityNode node, Cast cast)
    {
        var onto = Find(node.Require("onto"), cast)
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
    private static void Exhaust(AbilityNode node, Cast cast)
    {
        foreach (var target in Every(node.Argument, cast).Where(target => target.Ready))
        {
            target.Exhaust();
            cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
            {
                Trigger = cast.Trigger, Verb = "Exhaust",
            });
        }
    }

    private static void Ready(AbilityNode node, Cast cast)
    {
        foreach (var target in Every(node.Argument, cast).Where(target =>
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

    private static void DrawToHandSize(AbilityNode node, Cast cast)
    {
        int player = Seat(node.Argument, cast);
        var seat = cast.World.Seats[player];
        int count = (int)Math.Max(
            0, PhaseEnd.HandSize(cast.World, seat, cast.World.Facts)
                - HandCountDuringEvent(cast, seat));
        Draw.Cards(cast.World, player, count, cast.Trigger, cast.Events);
    }

    /// <summary>"Draw up to your printed hand size" — <c>rr:printed</c>.</summary>
    private static void DrawToPrintedHandSize(AbilityNode node, Cast cast)
    {
        int player = Seat(node.Argument, cast);
        var seat = cast.World.Seats[player];
        long printed = cast.World.Facts.PrintedValue(
            seat.IdentityCard.FaceId, "HS", cast.World.Players);
        int count = (int)Math.Max(0, printed - HandCountDuringEvent(cast, seat));
        Draw.Cards(cast.World, player, count, cast.Trigger, cast.Events);
    }

    private static bool CanDrawToPrintedHandSize(AbilityNode node, Cast cast)
    {
        int player = Seat(node.Argument, cast);
        var seat = cast.World.Seats[player];
        return HandCountDuringEvent(cast, seat) < cast.World.Facts.PrintedValue(
            seat.IdentityCard.FaceId, "HS", cast.World.Players);
    }

    private static int HandCountDuringEvent(Cast cast, Seat seat) =>
        seat.Hand.Cards.Count - (cast.Source.Area == seat.Hand
            && cast.World.Facts.Kind(cast.Source.FaceId) == CardKind.Event ? 1 : 0);

    private static void RemoveCounters(AbilityNode node, Cast cast)
    {
        string type = Word(node.Argument);
        string key = CounterKeyForRemoval(cast.Source, type)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' has no {type} counter to remove");
        long before = cast.Source.Tokens.GetValueOrDefault(key);

        cast.Source.PlaceTokens(key, -1);
        cast.Events.Add(new FieldSet(cast.Source.ObjectId, key, before, before - 1)
        {
            Trigger = cast.Trigger, Verb = "Remove_Counter",
        });

        if (CounterCount(cast.Source, "allPurpose") == 0
            && !Characteristics.IsLost(cast.World, cast.Source, "uses")
            && Reveal.Uses(cast.World.Facts.Attributes(cast.Source.FaceId)).Count > 0)
        {
            if (!Defeat.ToVictoryDisplay(
                    cast.World, cast.World.Facts, cast.Source,
                    cast.Trigger, cast.Events))
            {
                Rules.Play.Discard.Card(
                    cast.World, cast.Source, cast.Trigger, cast.Events);
            }
        }
    }

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
    private static void AdvanceMainScheme(AbilityNode node, Cast cast)
    {
        if (!string.Equals(Word(node.Argument), "next", StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' advances to an unsupported main scheme stage");
        }

        var scheme = cast.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' advances a main scheme that is not in play");
        MainScheme.Advance(
            cast.World, cast.World.Facts, cast.Abilities, scheme,
            cast.Trigger, cast.Events);
    }

    private static bool CanAdvanceMainScheme(AbilityNode node, Cast cast)
    {
        if (!string.Equals(Word(node.Argument), "next", StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' advances to an unsupported main scheme stage");
        }

        return cast.World.TheCardIn(DeckType.MainSchemesArea) is not null
            && cast.World.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0;
    }

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
    private static string? CounterKeyForRemoval(Card card, string type)
    {
        if (!string.Equals(type, "allPurpose", StringComparison.Ordinal))
        {
            string typed = "c_" + type;
            return card.Tokens.GetValueOrDefault(typed) > 0 ? typed : null;
        }

        string[] pools = [.. card.Tokens
            .Where(pair => pair.Value > 0
                && pair.Key.StartsWith("c_", StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .Order(StringComparer.Ordinal)];
        return pools.Length switch
        {
            0 => null,
            1 => pools[0],
            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' must choose which all-purpose counter to remove"),
        };
    }

    private static void PreventDamage(AbilityNode node, Cast cast)
    {
        int target = cast.Occurrence.Target >= 0
            ? cast.Occurrence.Target
            : cast.Occurrence.Subject;
        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "preventDamage",
            Amount: node.Field("amount") is { } amount ? Amount(amount, cast) : long.MaxValue,
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
        cast.World.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard,
            cast.World.Agenda.Current?.Round ?? 0,
            4,
            Index: cast.Player,
            Subject: card.ObjectId,
            Seat: cast.Player));
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
    private static void ShuffleInto(AbilityNode node, Cast cast)
    {
        var deck = Area(Word(node.Require("deck")), cast);
        bool applied = false;
        foreach (var card in Every(node.Require("cards"), cast))
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
    private static void Search(AbilityNode node, Cast cast)
    {
        string wanted = Word(node.Require("for"));
        var searched = Nodes(node.Require("in")).Select(where => where.Kind).ToList();
        var areas = searched.Select(where => Area(where, cast)).ToList();

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

    /// <summary>Which place on the board a word names.</summary>
    private static Area Area(string where, Cast cast) => where switch
    {
        "encounterDeck" => cast.World.AreaOf(DeckType.EncounterDeck),
        "encounterDiscardPile" => cast.World.AreaOf(DeckType.EncounterDiscardPile),
        "yourDeck" => cast.World.Seats[cast.Player].Deck,
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' searches '{where}', which is not implemented"),
    };

    /// <summary>
    /// "Choose to either … or …" — <c>rr:choose-option</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ability stops here.</b> An interpreter that returns a list of
    /// events has nowhere to ask a question, so the choice becomes a step on
    /// the agenda and what resumes the ability is the answer to it. The step
    /// carries the source card and the seat; <see cref="Choice"/> finds the
    /// node again from the card, which is why an ability may hold only one.
    /// </para>
    /// <para>
    /// <c>rr:choose-game-element.1</c> settles who is asked, and it is the
    /// player resolving the ability — not the first player, and not the card's
    /// owner, which an encounter card has not got.
    /// </para>
    /// </remarks>
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
    private static void Sequence(AbilityNode node, Cast cast, int from)
    {
        if (from == 0)
        {
            _ = CanInitiateSequence(node, cast);
        }

        var steps = Nodes(node.Argument).ToList();
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
    private static void ForEach(AbilityNode node, Cast cast)
    {
        long count = Amount(node.Require("count"), cast);
        if (count < 0)
        {
            throw new AbilityException("'forEach' needs a non-negative 'count'");
        }
        if (count == 0)
        {
            return;
        }

        var effect = Tree(node.Require("effect"));
        if (!Choices(effect).Any())
        {
            switch (effect.Kind)
            {
                case "dealDamage":
                    if (DamageTargets(effect.Require("cards"), cast).Count != 1)
                    {
                        throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' has a for-each damage effect without "
                            + "choose and does not resolve to one target");
                    }
                    DealDamage(effect, cast, count);
                    return;

                case "removeThreat":
                    if (Every(effect.Require("scheme"), cast).Count != 1)
                    {
                        throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' has a for-each threat-removal effect "
                            + "without choose and does not resolve to one target");
                    }
                    RemoveThreat(effect, cast, count);
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
    private static void EachTime(AbilityNode node, Cast cast)
    {
        var preceding = EachTimePreceding(node, cast);
        long requested = Amount(preceding.Require("count"), cast);
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

    private static AbilityNode EachTimePreceding(AbilityNode node, Cast cast)
    {
        var preceding = Tree(node.Require("effect"));
        if (preceding.Kind != "discardTop"
            || preceding.Field("player") is not null
            || !string.Equals(
                Word(preceding.Require("from")), "encounterDeck",
                StringComparison.Ordinal))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses each-time around an unsupported preceding effect");
        }
        return preceding;
    }

    private static void ValidateEachTimeBody(AbilityNode node, Cast cast)
    {
        if (ContainsUnreconstructibleAfterActivation(
            Tree(node.Require("then")), cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' suspends inside an after-activation effect, "
                + "which cannot be reconstructed");
        }
    }

    private static bool ContainsUnreconstructibleAfterActivation(
        AbilityNode node, Cast cast)
    {
        if (node.Kind == "afterActivation")
        {
            return DelayedNeedsContinuationAddress(
                Tree(node.Require("effect")), cast, hasContinuation: false);
        }
        return ContinuationChildren(node).Any(child =>
            ContainsUnreconstructibleAfterActivation(child, cast));
    }

    private static bool DelayedNeedsContinuationAddress(
        AbilityNode node, Cast cast, bool hasContinuation)
    {
        if (node.Kind == "afterActivation"
            || node.Kind == "and" && Nodes(node.Argument).Skip(1).Any()
            || IsChoice(node)
            || node.Kind is "eachPlayer" or "attack" or "thwart" or "thwartSchemes")
        {
            return true;
        }
        if (node.Kind is "placeThreat" or "enemyAttacks" or "enemySchemes")
        {
            return hasContinuation;
        }
        if (node.Kind is "seq" or "and")
        {
            var children = Nodes(node.Argument).ToList();
            return children.Select((child, index) => (child, index)).Any(entry =>
                DelayedNeedsContinuationAddress(
                    entry.child, cast,
                    hasContinuation || entry.index < children.Count - 1));
        }
        if (node.Kind == "if")
        {
            return Branches.Select(node.Field)
                .Where(branch => branch is not null)
                .Any(branch => DelayedNeedsContinuationAddress(
                    Tree(branch!), cast, hasContinuation));
        }
        if (node.Kind is "then" or "otherwise")
        {
            return DelayedNeedsContinuationAddress(
                    Tree(node.Require("effect")), cast, hasContinuation: true)
                || DelayedNeedsContinuationAddress(
                    Tree(node.Require(node.Kind)), cast, hasContinuation);
        }
        if (node.Kind == "forEach")
        {
            if (AmountMayChange(node.Require("count")))
            {
                return DelayedNeedsContinuationAddress(
                    Tree(node.Require("effect")), cast, hasContinuation: true);
            }
            long count = ForEachCount(node, cast);
            return count > 0 && DelayedNeedsContinuationAddress(
                Tree(node.Require("effect")), cast,
                hasContinuation || count > 1);
        }
        if (node.Kind == "eachTime")
        {
            var preceding = Tree(node.Require("effect"));
            if (preceding.Kind != "discardTop"
                || preceding.Field("player") is not null
                || !string.Equals(
                    Word(preceding.Require("from")), "encounterDeck",
                    StringComparison.Ordinal))
            {
                return true;
            }

            var requested = preceding.Require("count");
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
                Tree(node.Require("then")), cast,
                hasContinuation || count > 1);
        }
        return ContinuationChildren(node).Any(child =>
            DelayedNeedsContinuationAddress(child, cast, hasContinuation));
    }

    private static void ContinueEachTime(
        AbilityNode node, Cast cast, long from, long count)
    {
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

            if (!Test(Tree(node.Require("when")), cast))
            {
                continue;
            }

            cast.SetContinuation(outerContinuation || iteration < count - 1);
            RunChild(
                Tree(node.Require("then")),
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
    private static bool ContainsForEachTarget(AbilityNode node) =>
        node.Kind is "removeFromGame" or "exhaust" or "ready" or "reveal"
            or "returnToHand" or "returnOwnedToHand" or "soakDamage"
            or "addToHand" or "giveStatus" or "attachTo" or "grantUntil"
            or "discard" or "heal" or "placeCounters" or "shuffleInto" or "search"
            or "indirectDamage" or "dealDamage" or "moveDamage"
            or "dealAttackDamage" or "moveAttackDamage" or "placeThreat"
            or "removeThreat" or "replaceThreatWithDamage" or "enemyAttacks"
            or "enemySchemes" or "putIntoPlay" or "placeAtRandom" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice"
        || ContinuationChildren(node).Any(ContainsForEachTarget);

    private static void RunChild(AbilityNode node, string frame, Cast cast)
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

    private static int AbilityOrdinal(AbilityNode node, Cast cast)
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
            .Where(candidate => candidate.Node == node)
            .Select(candidate => candidate.ordinal)
            .ToList();
        return matches.Count == 1
            ? matches[0]
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot identify the exact ability that suspended");
    }

    private IEnumerable<CardAbility> AbilitiesOn(Card source, string? face) =>
        string.IsNullOrEmpty(face) ? On(source) : book.On(face);

    private void TrackResolution(Cast cast, CardAbility ability)
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

    private CardAbility AbilityAt(
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

    private static AbilityNode? TryNodeAtPath(
        AbilityNode root, IReadOnlyList<string> path)
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

    private static AbilityNode NodeAtPath(
        AbilityNode root, IReadOnlyList<string> path)
    {
        try
        {
            return NodeAtPathCore(root, path);
        }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or IndexOutOfRangeException
            or InvalidOperationException or FormatException)
        {
            throw new RulesNotImplementedException(
                $"ability continuation path '{string.Join("/", path)}' is invalid");
        }
    }

    private static AbilityNode NodeAtPathCore(
        AbilityNode root, IReadOnlyList<string> path, int offset = 0)
    {
        var node = root;
        for (int index = offset; index < path.Count; index++)
        {
            var parts = path[index].Split(':');
            node = parts[0] switch
            {
                "seq" => Nodes(node.Argument).ElementAt(ParseIndex(parts, path[index])),
                "if" => Tree(node.Require(parts[1])),
                "then" or "otherwise" => Tree(node.Require(parts[1])),
                "defense" or "eachPlayer" or "forEach" =>
                    Tree(node.Require("effect")),
                "eachTime" => Tree(node.Require("then")),
                "choice" when parts[1] == "option" =>
                    Nodes(node.Require("options")).ElementAt(ParseIndex(parts, path[index], 2)),
                "choice" when parts[1] == "effect" => Tree(node.Require("effect")),
                "choice" when parts[1] == "otherwise" => Tree(node.Require("otherwise")),
                "and" => Nodes(node.Argument).ElementAt(ParseIndex(parts, path[index])),
                _ => throw new RulesNotImplementedException(
                    $"ability continuation frame '{path[index]}' is not implemented"),
            };
        }
        return node;
    }

    private static void ResumeAfter(
        AbilityNode node, IReadOnlyList<string> path, Cast cast, int depth = 0,
        int stopBefore = -1)
    {
        try
        {
            ResumeAfterCore(node, path, cast, depth, stopBefore);
        }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or IndexOutOfRangeException
            or InvalidOperationException or FormatException)
        {
            throw new RulesNotImplementedException(
                $"ability continuation path '{string.Join("/", path)}' is invalid");
        }
    }

    private static void ResumeAfterCore(
        AbilityNode node, IReadOnlyList<string> path, Cast cast, int depth = 0,
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
        AbilityNode child = parts[0] switch
        {
            "seq" => Nodes(node.Argument).ElementAt(ParseIndex(parts, frame)),
            "if" => Tree(node.Require(parts[1])),
            "then" or "otherwise" => Tree(node.Require(parts[1])),
            "defense" or "eachPlayer" or "forEach" =>
                Tree(node.Require("effect")),
            "eachTime" => Tree(node.Require("then")),
            "choice" when parts[1] == "option" =>
                Nodes(node.Require("options")).ElementAt(ParseIndex(parts, frame, 2)),
            "choice" when parts[1] == "effect" => Tree(node.Require("effect")),
            "choice" when parts[1] == "otherwise" => Tree(node.Require("otherwise")),
            "and" => Nodes(node.Argument).ElementAt(ParseIndex(parts, frame)),
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
                var steps = Nodes(node.Argument).ToList();
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
                    RunChild(Tree(node.Require(parts[0])), $"{parts[0]}:{parts[0]}", cast);
                }
                break;

            case "and":
                var effects = Nodes(node.Argument).ToList();
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
                var repeated = Tree(node.Require("effect"));
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
        AbilityNode node, string[] parts, string frame)
    {
        return parts[0] switch
        {
            "seq" => ParseIndex(parts, frame) < Nodes(node.Argument).Count() - 1,
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
        AbilityNode node, string[] parts, string frame)
    {
        var effects = Nodes(node.Argument).ToList();
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

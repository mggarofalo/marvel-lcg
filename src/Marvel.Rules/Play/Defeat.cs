using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// How the game ended, or that it has not.
/// </summary>
/// <remarks>
/// The rules name two endings and they are not the same fact.
/// <c>rr:main-scheme-main-scheme-deck.2.1</c>: "if the villain completes the
/// final stage of the main scheme deck, <b>the villain wins the game</b>."
/// <c>rr:villain-defeat</c>: "if the final stage of the villain deck is
/// defeated, <b>the players win the game</b>." A boolean can say a game is over
/// and cannot say which of those happened.
/// </remarks>
public enum Outcome
{
    /// <summary>The game is still being played.</summary>
    Unfinished = 0,

    /// <summary>The players defeated the final villain stage.</summary>
    PlayersWin,

    /// <summary>The villain completed the final main scheme.</summary>
    VillainWins,

    /// <summary>
    /// The encounter deck and its discard pile emptied together.
    /// </summary>
    /// <remarks>
    /// <c>rr:encounter-deck.4</c>, and it is worded from the players' side
    /// rather than the villain's: "an infinite loop occurs with an infinite
    /// number of acceleration tokens being placed next to the main scheme deck.
    /// <b>If this happens, the players lose.</b>" Kept apart from
    /// <see cref="VillainWins"/> because the cause is different and a player
    /// asking why they lost deserves the difference.
    /// </remarks>
    PlayersLose,
}

/// <summary>
/// Defeating a character or a scheme — <c>rr:defeat</c>.
/// </summary>
/// <remarks>
/// <para>
/// "If a character has zero or fewer remaining hit points, or if a side scheme
/// has no threat on it, it is defeated." Then <c>rr:defeat.1</c> and
/// <c>.2</c> split what happens next by card type: an ally, minion or side
/// scheme is <b>discarded</b>; an identity or stage of the villain is
/// <b>removed from the game</b>.
/// </para>
/// <para>
/// <b>A defeat is not an occurrence of its own.</b>
/// <c>rr:triggering-condition.2</c> uses this very case as its example — "a
/// single attack causing a character to both take damage and be defeated" is
/// handled "with a single interrupt window and a single response window" — so
/// the defeat joins the occurrence that caused it rather than getting windows
/// of its own. <see cref="Record"/> is where that happens, and it is why a
/// card can answer "after an ally is defeated" at all.
/// </para>
/// <para>
/// <b>The interrupt tier is not in that window.</b> <c>rr:damage</c> numbers
/// nine steps and puts "abilities that trigger <i>when [character] is
/// defeated…</i>" at <c>.step.7</c> — after <c>.step.5</c> places the damage,
/// before <c>.step.8</c> discards the card. So it is reached from inside the
/// damage rather than from the window, which closed before <c>.step.1</c>.
/// <c>ICardAbilities.WhenCardDefeated</c> is that step, and it holds a card's
/// own "When Defeated" and another card's forced interrupt on the same defeat
/// alike, because the parenthesis in <c>.step.7</c> says they are one moment.
/// </para>
/// <para>
/// The earlier "would be defeated" interrupt is <c>rr:damage.step.6</c> and
/// resolves in <c>Damage.Deal</c> before this class is called. If it changes
/// the imminent defeat, <c>rr:would.1</c> means this class is not reached.
/// </para>
/// </remarks>
public static class Defeat
{
    /// <summary>
    /// Defeats a character that has run out of hit points.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="character">Who was defeated.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="how">
    /// What kind of thing did it, in the event stream's verb — an attack,
    /// consequential damage, and so on. Cards ask: Gene Pool answers "after an
    /// ally is defeated <b>by anything other than consequential damage</b>".
    /// </param>
    /// <param name="by">The seat whose character did it, or <c>-1</c>.</param>
    /// <returns>True, so that a caller can report it in one expression.</returns>
    public static bool Character(
        World world, ICardFacts facts, Card character, string trigger, List<GameEvent> events,
        string how = "", int by = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(events);

        var defeated = Record(world, character, by, how);
        var occurrence = world.Agenda.Occurrence!;

        // `rr:damage.step.7` -- "abilities that trigger *when [character] is
        // defeated…* *(including **When Defeated** abilities)*". The card's own
        // and every other card's forced interrupt on the same defeat, in the
        // one place the rule puts them: after `.step.5` has placed the damage
        // and before `.step.8` discards the card.
        //
        // `rr:when-defeated-abilities.2.1` says the same thing from the card's
        // side -- "a defeated card leaves play **after** its When Defeated
        // ability is resolved, if any" -- so this runs while the card is still
        // where it was, which is what lets an ability read its own tokens and
        // what is attached to it.
        //
        // A call and not a window because everything here is forced, and
        // `rr:forced.1` leaves nothing to offer and nothing to decline. The
        // occurrence's interrupt window closed before the damage that caused
        // this was dealt, and `rr:damage`'s own order is what says that is not
        // where these belong.
        events.AddRange(world.Abilities.WhenCardDefeated(world, character, defeated));

        if (!ReferenceEquals(world.Agenda.Occurrence, occurrence))
        {
            Defer(world, occurrence, character, trigger, Steps.FinalizeCharacterDefeat);
            return true;
        }

        FinalizeCharacter(world, facts, character, trigger, events);
        return true;
    }

    /// <summary>Damage step 8: move or eliminate a defeated card.</summary>
    /// <remarks>
    /// This remains a separate operation because a step-7 ability may insert
    /// an occurrence that can ask a player a question. In that case
    /// <see cref="Character"/> or <see cref="Scheme"/> schedules this as a
    /// plan after the nested work. With no suspension it is called inline, so
    /// ordinary defeat retains its existing synchronous result.
    /// </remarks>
    public static void FinalizeCharacter(
        World world, ICardFacts facts, Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        switch (FacedownDrones.Kind(card, facts))
        {
            case CardKind.Ally:
            case CardKind.Minion:
                // `rr:defeat.1` -- discarded, to its owner's pile, which for a
                // minion is the encounter discard. Unless it is worth points.
                MoveDefeatedCard(world, facts, card, trigger, events);

                return;

            case CardKind.EncounterVillain:
                VillainStage(world, facts, card, trigger, events);
                return;

            case CardKind.Hero:
            case CardKind.AlterEgo:
                // `rr:hit-points.2.1` -- "if a player's hit point dial is
                // reduced to zero, that player is defeated and eliminated from
                // the game." What that costs is `rr:player-elimination`.
                Elimination.Eliminate(world, facts, card.Owner, trigger, events);
                return;

            default:
                throw new RulesNotImplementedException(
                    $"a {FacedownDrones.Kind(card, facts)} was defeated, and rr:defeat does not "
                    + "say what happens to one");
        }
    }

    /// <summary>
    /// A side scheme with no threat left on it — <c>rr:defeat</c>,
    /// <c>rr:side-scheme.2</c>.
    /// </summary>
    /// <remarks>
    /// "If a character has zero or fewer remaining hit points, <b>or if a side
    /// scheme has no threat on it</b>, it is defeated", and <c>rr:defeat.1</c>
    /// discards it. The same two destinations as a character: the victory
    /// display if it is worth points, and the discard pile otherwise.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="scheme">The side scheme.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="by">The seat whose character did it, or -1.</param>
    public static void Scheme(
        World world, ICardFacts facts, Card scheme, string trigger, List<GameEvent> events,
        int by = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        ArgumentNullException.ThrowIfNull(facts);

        var defeated = Record(world, scheme, by, BasicPowers.ThwartVerb);
        var occurrence = world.Agenda.Occurrence!;

        // `rr:when-defeated-abilities.2` lists a side scheme among the cards
        // this happens to, and `.2.1` puts it before the card goes.
        events.AddRange(world.Abilities.WhenCardDefeated(world, scheme, defeated));

        if (!ReferenceEquals(world.Agenda.Occurrence, occurrence))
        {
            Defer(world, occurrence, scheme, trigger, Steps.FinalizeSchemeDefeat);
            return;
        }

        FinalizeScheme(world, facts, scheme, trigger, events);
    }

    /// <summary>Move a defeated side scheme after its step-7 abilities resolve.</summary>
    public static void FinalizeScheme(
        World world, ICardFacts facts, Card scheme, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        MoveDefeatedCard(world, facts, scheme, trigger, events);
    }

    /// <summary>Put damage step 8 after nested step-7 agenda work.</summary>
    private static void Defer(
        World world, Timing.Occurrence occurrence, Card card, string trigger, string step)
    {
        var current = world.Agenda.Current
            ?? throw new InvalidOperationException("nested defeat work has no agenda step");
        world.Agenda.Before(
            occurrence,
            new PhaseStep(
                step,
                current.Round,
                current.Number,
                Index: current.Index,
                Subject: card.ObjectId,
                Plan: true,
                Trigger: trigger));
    }

    /// <summary>
    /// Hangs the defeat on the occurrence that caused it —
    /// <c>rr:triggering-condition.2</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If a single game occurrence creates multiple triggering conditions
    /// <i>(such as a single attack causing a character to both take damage and
    /// be defeated)</i>, those triggering conditions are handled with a single
    /// interrupt window and a single response window." The parenthesis is this
    /// method's whole job: the defeat is a second condition of the occurrence
    /// that caused it, and joining it there is what opens a window for a card
    /// answering "after an ally is defeated" — while <b>not</b> opening a
    /// second one that would let an ability fire twice against one moment.
    /// </para>
    /// <para>
    /// <b>A defeat outside any occurrence is refused.</b> Not defensiveness:
    /// every way a card can be defeated is something happening in the game, and
    /// something happening in the game is a step on the agenda. If this throws,
    /// the missing piece is the <i>cause</i> — some way of doing damage or
    /// removing threat that this engine still performs as a call rather than as
    /// a step, and whose own windows are therefore missing too. Silence here
    /// would hide that, and hide it precisely in the cards that were written to
    /// notice.
    /// </para>
    /// </remarks>
    private static Defeated Record(World world, Card card, int by, string how)
    {
        var defeated = new Defeated(card.ObjectId, by, how);

        if (world.Agenda.Occurrence is not { } happening)
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} was defeated by '{how}' and nothing is happening on the "
                + "agenda for rr:triggering-condition.2 to join the defeat to, so no window "
                + "opens around it. Whatever caused the defeat is a call in this engine and "
                + "the rules make it an occurrence");
        }

        happening.Also(defeated);
        return defeated;
    }

    /// <summary>
    /// What a new villain stage keeps from the old one —
    /// <c>rr:villain-defeat.3</c> and <c>.4</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two clauses are the same list with opposite answers, and the title
    /// is what chooses between them. <b>Same title</b> (<c>.3.2</c>):
    /// "attachments, upgrades, status cards, counters, and non-damage tokens on
    /// a villain carry over to the new stage." <b>Different title</b>
    /// (<c>.4.2</c>): they "do <b>not</b> carry over".
    /// </para>
    /// <para>
    /// Rhino's three stages share a title, and Charge attaches to Rhino — so
    /// this is the ordinary case in the one scenario the engine plays, not an
    /// expansion corner.
    /// </para>
    /// <para>
    /// <b>Non-damage tokens.</b> Damage is not a token here (<c>Card.Damage</c>
    /// is its own field, because the digest records remaining <c>health</c> and
    /// no damage key), and <c>rr:villain-defeat.2</c> says excess damage does
    /// not carry over anyway — so every token the old stage held is one that
    /// travels.
    /// </para>
    /// </remarks>
    private static void Inherit(
        World world, ICardFacts facts, Card was, Card now, string trigger,
        List<GameEvent> events)
    {
        bool same = string.Equals(
            facts.Title(was.FaceId), facts.Title(now.FaceId), StringComparison.Ordinal);

        foreach (var area in world.Areas.ToList())
        {
            if (area.Host != was.ObjectId || area.Cards.Count == 0)
            {
                continue;
            }

            var onto = world.AreaOf(area.Type, now.Area.PlayArea, now.ObjectId, area.CardOwner);
            foreach (var card in area.Cards.ToList())
            {
                if (!same)
                {
                    Discard.Card(world, card, trigger, events);
                    continue;
                }

                var from = card.Area;
                World.MoveToTop(card, onto);
                events.Add(new CardsMoved(
                    Places.Reference(from), Places.Reference(onto),
                    [new Landing(card.ObjectId, onto.Cards.Count - 1)])
                {
                    Trigger = trigger, Verb = "Carry_Over",
                });
                events.Add(new CardAttached(card.ObjectId, now.ObjectId)
                {
                    Trigger = trigger, Verb = "Carry_Over",
                });
            }
        }

        if (!same)
        {
            return;
        }

        foreach (var (kind, count) in was.Tokens)
        {
            if (count > 0)
            {
                now.PlaceTokens(kind, count);
                events.Add(new FieldSet(now.ObjectId, kind, 0, count)
                {
                    Trigger = trigger, Verb = "Carry_Over",
                });
            }
        }
    }

    /// <summary>
    /// A defeated card worth points goes to the victory display —
    /// <c>rr:victory-x</c>.
    /// </summary>
    /// <remarks>
    /// <c>rr:victory-x.2</c>: "a character or side scheme with the victory X
    /// keyword is placed in the victory display <b>when it is defeated</b>",
    /// which <c>.1.1</c> writes as "<b>When Defeated</b>: add this card to the
    /// victory display". Instead of the discard pile, not as well as it.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="card">The defeated card.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    /// <returns>Whether it went there.</returns>
    public static bool ToVictoryDisplay(
        World world, ICardFacts facts, Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        // Presence and value are separate: Victory 0 still supplies this
        // replacement ability even though it contributes no points.
        if (!Timing.Keywords.Has(world, card, "victory", facts))
        {
            return false;
        }

        var display = world.AreaOf(DeckType.VictoryDisplay);
        var from = card.Area;
        var constantsEnding = world.Effects.PreflightConstantsEnding(card);
        using var departure = constantsEnding.Begin();
        Discard.Attachments(world, card, trigger, events);
        Discard.ResetLeavingState(world, card, trigger, events);
        World.MoveToTop(card, display);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(display),
            [new Landing(card.ObjectId, display.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Victory",
        });
        constantsEnding.Complete(trigger, events);

        return true;
    }

    /// <summary>Total points currently in the shared victory display.</summary>
    public static long VictoryPoints(World world, ICardFacts facts) =>
        world.AreaOf(DeckType.VictoryDisplay).Cards.Sum(card =>
            StateFields.Modified(world, card, "victory", facts, world.Players));

    /// <summary>Moves victory attachments away before ordinary hosted-card cleanup.</summary>
    private static void VictoryAttachments(
        World world, ICardFacts facts, Card host, string trigger, List<GameEvent> events)
    {
        foreach (var attachment in VictoryAttachmentsOn(world, facts, host))
        {
            ToVictoryDisplay(world, facts, attachment, trigger, events);
        }
    }

    private static List<Card> VictoryAttachmentsOn(
        World world, ICardFacts facts, Card host) =>
    [
        .. world.Areas
            .Where(area => area.Host == host.ObjectId
                && DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => facts.Kind(card.FaceId) is
                CardKind.Attachment or CardKind.Upgrade)
            .Where(card => Timing.Keywords.Has(world, card, "victory", facts)),
    ];

    /// <summary>Preflights the hosted tree in its Victory-interrupt order.</summary>
    private static void PreflightDefeatAttachments(
        World world, ICardFacts facts, Card host)
    {
        var victory = VictoryAttachmentsOn(world, facts, host);

        // A Victory attachment leaves before its host. Permanent on that same
        // card therefore never reaches rr:permanent.5, but any hosted
        // descendant still has to be proved removable from the Victory card.
        foreach (var attachment in victory)
        {
            Discard.PreflightAttachments(world, attachment);
        }

        Discard.PreflightAttachmentsExcept(
            world, host, victory.Select(card => card.ObjectId).ToHashSet());
    }

    /// <summary>Moves Victory interrupts and their defeated host as one transaction.</summary>
    private static void MoveDefeatedCard(
        World world, ICardFacts facts, Card host, string trigger,
        List<GameEvent> events)
    {
        PreflightDefeatAttachments(world, facts, host);
        var constantsEnding = world.Effects.PreflightConstantsEnding(
            DefeatDepartureCards(world, host));
        using var departure = constantsEnding.Begin();

        VictoryAttachments(world, facts, host, trigger, events);
        if (!ToVictoryDisplay(world, facts, host, trigger, events))
        {
            Discard.Card(world, host, trigger, events);
        }

        constantsEnding.Complete(trigger, events);
    }

    /// <summary>The complete physical card set removed by one host defeat.</summary>
    private static List<Card> DefeatDepartureCards(World world, Card host)
    {
        var cards = new List<Card> { host };
        var seen = new HashSet<int> { host.ObjectId };
        var pending = new Stack<Card>(world.Areas
            .Where(area => area.Host == host.ObjectId)
            .SelectMany(area => area.Cards)
            .Reverse());
        while (pending.TryPop(out var card))
        {
            if (!seen.Add(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {card.ObjectId} forms a hosting cycle");
            }
            cards.Add(card);
            foreach (var child in world.Areas
                         .Where(area => area.Host == card.ObjectId)
                         .SelectMany(area => area.Cards)
                         .Reverse())
            {
                pending.Push(child);
            }
        }
        return cards;
    }

    /// <summary>
    /// A villain stage is defeated — <c>rr:villain-defeat</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Remove the current stage of the villain deck from the game. The next
    /// sequential stage of the villain deck is revealed. Set the villain's hit
    /// point dial as indicated by that stage. <b>If the final stage of the
    /// villain deck is defeated, the players win the game.</b>"
    /// </para>
    /// <para>
    /// <c>rr:villain-defeat.2</c>: "excess damage that is dealt to defeat a
    /// villain stage does not carry over to the new stage" — which is why the
    /// new stage starts with no damage rather than inheriting any.
    /// </para>
    /// </remarks>
    private static void VillainStage(
        World world, ICardFacts facts, Card villain, string trigger, List<GameEvent> events)
    {
        var deck = world.AreaOf(DeckType.VillainDeck);
        var following = deck.Cards.Count > 0 ? deck.Cards[^1] : null;
        bool carriesToFollowing = following is not null && string.Equals(
            facts.Title(villain.FaceId), facts.Title(following.FaceId),
            StringComparison.Ordinal);
        var constantsEnding = world.Effects.PreflightConstantsEnding(
            villain, includeHostedCards: !carriesToFollowing);
        using var departure = constantsEnding.Begin();
        if (!carriesToFollowing)
        {
            // Same-title stages inherit hosted cards. Every other departure,
            // including the final stage, discards them before the host moves.
            Discard.Attachments(world, villain, trigger, events);
        }

        var removed = world.AreaOf(DeckType.RemovedArea);
        var from = villain.Area;
        World.MoveToTop(villain, removed);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(removed),
            [new Landing(villain.ObjectId, removed.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Defeat",
        });
        constantsEnding.Complete(trigger, events);

        var next = deck.TakeTop();
        if (next is null)
        {
            world.Finish(Outcome.PlayersWin);
            return;
        }

        var area = world.AreaOf(DeckType.VillainArea);
        World.MoveToTop(next, area);
        next.TurnFaceUp();
        events.Add(new CardsMoved(
            Places.Reference(deck), Places.Reference(area),
            [new Landing(next.ObjectId, area.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Reveal",
        });

        // `rr:villain-defeat.3.2` before either of the two below, so that a
        // tough status card carried over from the old stage is already on the
        // new one when toughness looks for it.
        Inherit(world, facts, villain, next, trigger, events);

        // The stage came out of the villain deck and into the villain's play
        // area, and `rr:enters-play` is "any time when a card transitions from
        // an out-of-play area into play" -- so the keywords that fire on
        // entering play fire here. `rr:villain-defeat.3.1` makes the new stage
        // "the same character" for card abilities, which is a claim about who
        // the character is rather than about the card having been in play: the
        // card itself is a different card, and it was in the deck a moment ago.
        Reveal.EnterPlay(world, facts, next, events);

        // `rr:when-revealed-abilities`: "when a player reveals a card from the
        // encounter deck, a new scheme stage, **or a new villain stage**, all
        // 'When Revealed' abilities on the card resolve." Last, because
        // `rr:reveal.step.3` puts the card's own text after the placement and
        // the keywords -- and `.3` there, with `rr:villain-defeat.1`, is why
        // nothing between here and the deck gets to cancel it.
        events.AddRange(world.Abilities.WhenRevealed(world, next, world.FirstPlayer));
    }
}

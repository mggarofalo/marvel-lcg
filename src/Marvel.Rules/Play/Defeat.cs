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
/// <b>Not implemented here, and it throws by name:</b> the "When Defeated"
/// window. <c>rr:when-defeated-abilities.1</c> makes it a forced interrupt, so
/// it resolves <i>before</i> the card leaves play — which means it belongs on
/// the agenda as a step with a window, not inside this call. Nothing in the
/// dataset has one yet.
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
    /// <returns>True, so that a caller can report it in one expression.</returns>
    public static bool Character(
        World world, ICardFacts facts, Card character, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:when-defeated-abilities.2.1` -- "a defeated card leaves play
        // **after** its When Defeated ability is resolved, if any." So this
        // runs while the card is still where it was, which is what lets the
        // ability read its own tokens and what is attached to it.
        //
        // `.1` makes it "**Forced Interrupt**: when this card is defeated",
        // and a forced interrupt has no choice in it -- there is nothing to
        // offer and nothing to decline, so it resolves here rather than in a
        // window. The window *around* a defeat is a separate thing and is not
        // opened; no card in the pool interrupts one except by this.
        events.AddRange(world.Abilities.WhenDefeated(world, character));

        switch (facts.Kind(character.FaceId))
        {
            case CardKind.Ally:
            case CardKind.Minion:
                // `rr:defeat.1` -- discarded, to its owner's pile, which for a
                // minion is the encounter discard. Unless it is worth points.
                if (!ToVictoryDisplay(world, facts, character, trigger, events))
                {
                    Discard.Card(world, character, trigger, events);
                }

                return true;

            case CardKind.EncounterVillain:
                VillainStage(world, facts, character, trigger, events);
                return true;

            case CardKind.Hero:
            case CardKind.AlterEgo:
                // `rr:hit-points.2.1` -- "if a player's hit point dial is
                // reduced to zero, that player is defeated and eliminated from
                // the game." What that costs is `rr:player-elimination`.
                Elimination.Eliminate(world, facts, character.Owner, trigger, events);
                return true;

            default:
                throw new RulesNotImplementedException(
                    $"a {facts.Kind(character.FaceId)} was defeated, and rr:defeat does not "
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

        world.Defeated = new Defeated(scheme.ObjectId, by);

        // `rr:when-defeated-abilities.2` lists a side scheme among the cards
        // this happens to, and `.2.1` puts it before the card goes.
        events.AddRange(world.Abilities.WhenDefeated(world, scheme));
        world.Defeated = null;
        ArgumentNullException.ThrowIfNull(facts);

        if (!ToVictoryDisplay(world, facts, scheme, trigger, events))
        {
            Discard.Card(world, scheme, trigger, events);
        }
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

        if (facts.PrintedValue(card.FaceId, "Victory", world.Players) <= 0)
        {
            return false;
        }

        var display = world.AreaOf(DeckType.VictoryDisplay);
        var from = card.Area;
        World.MoveToTop(card, display);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(display),
            [new Landing(card.ObjectId, display.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Victory",
        });

        return true;
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
        var removed = world.AreaOf(DeckType.RemovedArea);
        var from = villain.Area;
        World.MoveToTop(villain, removed);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(removed),
            [new Landing(villain.ObjectId, removed.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Defeat",
        });

        var deck = world.AreaOf(DeckType.VillainDeck);
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

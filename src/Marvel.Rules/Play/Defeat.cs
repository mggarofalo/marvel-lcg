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
                // the game". `rr:player-elimination` is a page of consequences
                // -- their cards leave play, their obligations are removed,
                // and at one player the game simply ends -- and none of it is
                // written.
                throw new RulesNotImplementedException(
                    $"{world.Seats[character.Owner].Name} was defeated, and "
                    + "rr:player-elimination is not implemented");

            default:
                throw new RulesNotImplementedException(
                    $"a {facts.Kind(character.FaceId)} was defeated, and rr:defeat does not "
                    + "say what happens to one");
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

        // `rr:villain-defeat.3` and `.4` decide whether attachments, status
        // cards and counters carry over, by whether the new stage has the same
        // title. Nothing is carried here because nothing in the dataset yet
        // attaches to a villain that can be defeated, and carrying the wrong
        // set is worse than refusing.
        if (world.AreaOf(DeckType.UpgradesArea, from.PlayArea, villain.ObjectId).Cards.Count > 0)
        {
            throw new RulesNotImplementedException(
                $"card {villain.ObjectId} was defeated with cards attached, and "
                + "rr:villain-defeat.3.2 decides whether they carry over by title");
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

        _ = facts;
    }
}

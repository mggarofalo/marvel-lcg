using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// Ending a phase: the effects that expire, and the abilities that answer.
/// </summary>
/// <remarks>
/// <para>
/// The rules state this twice, in the same shape, for the two phases:
/// </para>
/// <list type="table">
///   <item>
///     <term><c>rr:villain-phase.step.6</c></term>
///     <description>
///       <i>End of Villain Phase and Round.</i> (a) effects lasting "until the
///       end of the [villain] phase" or "until the end of the round" end;
///       (b) resolve any "when/after the [villain] phase ends" or "when/after
///       the round ends" effects.
///     </description>
///   </item>
///   <item>
///     <term><c>rr:end-of-player-phase.step.4</c> and <c>.step.5</c></term>
///     <description>the same two steps for the player phase.</description>
///   </item>
/// </list>
/// <para>
/// <b>Ending a phase is an occurrence, so it has an interrupt window.</b> That
/// is not read into the rules — <c>rr:temporary.1</c> states it outright, that
/// the temporary keyword "is equivalent to the following triggered ability:
/// <i>Forced Interrupt: When the round ends, discard this card from play</i>".
/// A forced interrupt resolves before its triggering condition
/// (<c>rr:interrupt.3</c>), so a temporary card is discarded <i>before</i> the
/// effects of step 6a expire, not after.
/// </para>
/// <para>
/// The villain phase's ending is one occurrence carrying two conditions, the
/// phase ending and the round ending. <c>rr:triggering-condition.2</c> is why
/// that is one interrupt window and one response window rather than two of
/// each: an ability answering "when the round ends" gets a single turn even
/// though both conditions became true at once.
/// </para>
/// </remarks>
public static class PhaseEnd
{
    /// <summary>"When the villain phase ends", as a triggering condition.</summary>
    public const string VillainPhaseEnds = "WhenVillainPhaseEnds";

    /// <summary>"When the round ends", as a triggering condition.</summary>
    public const string RoundEnds = "WhenRoundEnds";

    /// <summary>"When the player phase ends", as a triggering condition.</summary>
    public const string PlayerPhaseEnds = "WhenPlayerPhaseEnds";

    /// <summary>
    /// End the villain phase, and with it the round —
    /// <c>rr:villain-phase.step.6</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void EndVillainPhase(World world, ICardFacts facts, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:temporary.1` -- "**Forced Interrupt**: when the round ends,
        // discard this card from play." A forced interrupt resolves *before*
        // its triggering condition (`rr:interrupt.3`), so a temporary card goes
        // before step 6a expires anything -- which is why this is here rather
        // than after `End`.
        foreach (var area in world.Areas.ToList())
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards.ToList())
            {
                if (StateFields.Modified(
                        world, card, "temporary", facts, world.Players) > 0)
                {
                    Discard.Card(world, card, "end of round", events);
                }
            }
        }

        End(world,
            new Occurrence(0, [VillainPhaseEnds, RoundEnds]),
            [TimingPoints.EndOfVillainPhase, TimingPoints.EndOfRound, TimingPoints.EndOfTurn],
            events);
    }

    /// <summary>
    /// End the player phase — <c>rr:end-of-player-phase.step.4</c> and
    /// <c>.step.5</c>.
    /// </summary>
    /// <remarks>
    /// Steps 1 to 3 of <c>rr:end-of-player-phase</c> come before this and are
    /// not this method's business: step 1 is a question and lives on the turn
    /// prompt, steps 2 and 3 are <see cref="DrawToHandSize"/> and
    /// <see cref="ReadyCards"/>.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void EndPlayerPhase(World world, List<GameEvent> events) =>
        End(world,
            new Occurrence(0, [PlayerPhaseEnds]),
            [TimingPoints.EndOfPlayerPhase],
            events);

    /// <summary>
    /// Step 1 — one player discards from hand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:end-of-player-phase.step.1</c>: "In player order, each player
    /// <b>may</b> discard any number of cards from their hand, and <b>must</b>
    /// discard down to their hand size if they have more cards than their hand
    /// size."
    /// </para>
    /// <para>
    /// Two clauses and they are different rules. The first makes an empty
    /// answer legal — a player may discard nothing — and the second makes it
    /// illegal for an over-full hand. So this is not a prompt that can simply be
    /// declined, and a decline that leaves too many cards is refused by name
    /// rather than silently discarding for the player: <b>which</b> cards go is
    /// their decision, and the engine has no basis for making it.
    /// </para>
    /// <para>
    /// "In player order" and not "simultaneously", unlike steps 2 and 3 — so
    /// this is asked once per seat, which is why it lives on a prompt rather
    /// than on the agenda.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="seat">Whose hand.</param>
    /// <param name="chosen">The cards they chose to discard, by object id.</param>
    /// <param name="events">Where to record what moved.</param>
    /// <exception cref="RulesNotImplementedException">
    /// A card chosen is not in that hand, or the hand is still over its size.
    /// </exception>
    public static void DiscardToHandSize(
        World world, ICardFacts facts, int seat, IReadOnlyList<int> chosen,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(chosen);
        ArgumentNullException.ThrowIfNull(events);

        var player = world.Seats[seat];
        foreach (int id in chosen)
        {
            var card = world.Cards[id];
            if (card.Area != player.Hand)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is not in {player.Name}'s hand, so it cannot be discarded from it");
            }

            Discard.Card(world, card, "end of player phase", events);
        }

        long limit = HandSize(world, player, facts);
        if (player.Hand.Cards.Count > limit)
        {
            throw new RulesNotImplementedException(
                $"{player.Name} holds {player.Hand.Cards.Count} cards and must discard down to "
                + $"{limit}; rr:end-of-player-phase.step.1 does not let this be declined");
        }
    }

    /// <summary>
    /// Step 2 — each player draws up to their hand size.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:end-of-player-phase.step.2</c>: "Each player <b>simultaneously</b>
    /// draws up to their hand size." Simultaneous is why this is one step for
    /// the table and not one per player — nothing may happen between one
    /// player's draw and another's.
    /// </para>
    /// <para>
    /// <c>rr:hand-size.1</c> makes it card by card: "a player draws cards one
    /// at a time, <b>checking after each card is drawn</b> whether they are at
    /// their hand size." That is not the same as computing a count and taking
    /// that many — a card drawn can change the hand size, and
    /// <c>rr:player-deck.2</c> has the deck run out and reshuffle mid-draw.
    /// </para>
    /// <para>
    /// A hand already over its size is left alone. Step 1 is where a hand comes
    /// down, and it has already happened; drawing cannot discard.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what moved.</param>
    public static void DrawToHandSize(World world, ICardFacts facts, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        foreach (int seat in world.PlayerOrder)
        {
            var player = world.Seats[seat];

            // Bounded by the hand size read before the first draw as well as by
            // the one read after each. The re-read is the rule; the bound is
            // what stops a card that *raises* hand size while being drawn from
            // making this a loop that never ends.
            long limit = HandSize(world, player, facts);
            for (long drawn = 0; drawn < limit; drawn++)
            {
                if (player.Hand.Cards.Count >= HandSize(world, player, facts))
                {
                    break;
                }

                int before = player.Hand.Cards.Count;
                Draw.Cards(world, seat, 1, "end of player phase", events);
                if (player.Hand.Cards.Count == before)
                {
                    // `rr:player-deck.4` -- a deck and a discard pile both
                    // empty. There is no card to draw and that is a legal
                    // board, not a stall.
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Step 3 — every card readies.
    /// </summary>
    /// <remarks>
    /// <c>rr:end-of-player-phase.step.3</c>: "Each player simultaneously
    /// readies all of their cards. <b>Ready each exhausted encounter card.</b>"
    /// The second sentence is why this walks every place in play rather than
    /// each player's: an exhausted minion is nobody's card and readies anyway.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="events">Where to record what changed.</param>
    public static void ReadyCards(World world, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                // `rr:exhausted` is about cards in play. A card in a deck or a
                // hand has no ready state to return to.
                continue;
            }

            foreach (var card in area.Cards)
            {
                if (card.Ready)
                {
                    continue;
                }

                card.Refresh();
                events.Add(new FieldSet(card.ObjectId, "is_exhaust", 1, 0)
                {
                    Trigger = "end of player phase", Verb = "Ready",
                });
            }
        }
    }

    /// <summary>
    /// A player's hand size as the game currently counts it — <c>rr:hand-size</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="player">Whose hand.</param>
    /// <param name="facts">The printed card data.</param>
    public static long HandSize(World world, Seat player, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(player);

        // `rr:hand-size` cites `rr:modifiers`, so this is the modified value
        // and not the printed one -- and it is read off whichever face is
        // showing, because the two sides of an identity print different hand
        // sizes. Peter Parker holds six cards and Spider-Man five.
        return StateFields.Modified(world, player.IdentityCard, "hand_size", facts, world.Players);
    }

    private static void End(
        World world,
        Occurrence occurrence,
        IReadOnlyList<string> expiring,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        {
            // Step 6a / step 4. The phase has now ended, so everything bounded
            // by its ending is gone -- rr:lasting-effects.5.
            foreach (var timingPoint in expiring)
            {
                world.Effects.Expire(timingPoint);
            }

            // Delayed effects waiting on this moment resolve here, "before
            // responses to that point or condition may be used" --
            // rr:delayed-effect.1 -- which is what puts this between the two
            // windows rather than in the response one.
            foreach (var condition in occurrence.Conditions)
            {
                DelayedEffects.Occur(world, condition, events);
            }
        }
    }
}

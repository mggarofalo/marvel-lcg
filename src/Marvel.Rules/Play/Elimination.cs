using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// A player whose identity was defeated — <c>rr:player-elimination</c>.
/// </summary>
/// <remarks>
/// "A player is eliminated from the game if their identity is defeated. This
/// usually occurs when the character's remaining hit points are reduced to
/// zero." The rule then gives five numbered steps, and they are five here —
/// the order matters, because step 1 hands the first player token on before
/// step 5 removes the play area it was in.
/// </remarks>
public static class Elimination
{
    /// <summary>Eliminates a player, in the five steps the rule lists.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Whose identity was defeated.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Eliminate(
        World world, ICardFacts facts, int player, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var seat = world.Seats[player];
        if (seat.Eliminated)
        {
            return;
        }

        Preflight(world, facts, player);

        seat.Eliminated = true;

        // `rr:player-elimination.5.1`: eliminating the attacked player in the
        // middle of an attack ends that attack immediately. Its current
        // occurrence still reaches responses, including after-attack text,
        // but none of its later combat steps resolve.
        if (world.Attack is { Player: var attacked } && attacked == player)
        {
            Attack.EndForEliminatedPlayer(world, events);
        }

        var layout = EliminationLayout.Calculate(new WorldEliminationLayout(world), player);

        // Step 1. "If the eliminated player has the first player token, they
        // pass it to the next clockwise player." Before step 5 takes their play
        // area away.
        if (world.FirstPlayer == player && layout.NextPlayer is { } holder)
        {
            world.FirstPlayer = holder;
        }

        // Step 2. "If there are minions engaged with the eliminated player,
        // each of those minions engages the next clockwise player, **retaining
        // any tokens, attached cards, boost cards, tucked cards, and status
        // cards on them**." Moving the card keeps all of those, because they
        // hang off its object id rather than off the area.
        if (layout.NextPlayer is { } neighbour)
        {
            var onto = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(neighbour));
            foreach (var relocation in layout.Relocations)
            {
                var minion = world.Cards[relocation.Minion];
                var from = minion.Area;
                World.MoveToTop(minion, onto);
                events.Add(new CardsMoved(
                    Places.Reference(from), Places.Reference(onto),
                    [new Landing(minion.ObjectId, onto.Cards.Count - 1)])
                {
                    Trigger = trigger, Verb = "Engage",
                });

                // "Retaining any tokens, attached cards, boost cards, tucked
                // cards, and status cards on them" includes their placement:
                // they move into the next player's play area with their host.
                foreach (int cardId in relocation.Hosted)
                {
                    var card = world.Cards[cardId];
                    var source = card.Area;
                    var destination = world.AreaOf(
                        source.Type, onto.PlayArea, source.Host, source.CardOwner);
                    bool wasFaceUp = card.FaceUp;
                    World.MoveToTop(card, destination);
                    if (wasFaceUp)
                    {
                        card.TurnFaceUp();
                    }
                    else
                    {
                        card.TurnFaceDown();
                    }

                    events.Add(new CardsMoved(
                        Places.Reference(source), Places.Reference(destination),
                        [new Landing(card.ObjectId, destination.Cards.Count - 1)])
                    {
                        Trigger = trigger, Verb = "Engage",
                    });
                }
            }
        }

        // Steps 3 and 4. Cards **in play** leave by ownership: one somebody
        // else owns goes to *their* discard pile (step 3.3), one this player
        // owns goes to theirs (step 4).
        foreach (var area in Mine(world, player))
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards.ToList())
            {
                Leave(world, facts, seat, card, trigger, events);
            }
        }

        // Step 5. "**Remove** the eliminated player's play area and each other
        // game element within it *(hand, deck, discard pile, cards in play, hit
        // point dial, etc.)* **from the game**."
        //
        // Removed, not discarded -- which matters beyond tidiness. Discarding a
        // deck into its own discard pile is `rr:player-deck.1`'s trigger, and an
        // eliminated player would reshuffle a deck they no longer have and deal
        // themselves an encounter card for it.
        foreach (var area in Mine(world, player))
        {
            foreach (var card in area.Cards.ToList())
            {
                Remove(world, card, trigger, events);
            }
        }

        // `rr:player-s-play-area.6` removes the play area itself, not only the
        // game elements that were inside it. Game-area membership is the
        // model's representation of that boundary.
        world.Detach(PlayArea.Of(player), trigger, events);

        // `.4`: "if all players are eliminated, the game ends and the players
        // lose." Asked after the play area is cleared, so a game that ends here
        // ends on a board nobody is still holding cards on.
        if (world.Seats.All(each => each.Eliminated))
        {
            world.Finish(Outcome.PlayersLose);
        }
    }

    /// <summary>
    /// The areas that belong to one play area, as step 5 counts them.
    /// </summary>
    /// <remarks>
    /// A hosted area counts only while its host also belongs to this play
    /// area. Step 2 explicitly relocates retained descendants with their
    /// minion; later departures are read again after each numbered step has
    /// changed placement and settled continuous effects.
    /// </remarks>
    private static List<Area> Mine(World world, int player) =>
        [.. world.Areas.Where(area =>
            area.PlayArea == PlayArea.Of(player)
            && area.Cards.Count > 0
            && (area.Host < 0
                || world.Cards[area.Host].Area.PlayArea == PlayArea.Of(player)))];

    /// <summary>One card leaving an eliminated player's play area.</summary>
    private static void Leave(
        World world, ICardFacts facts, Seat seat, Card card, string trigger,
        List<GameEvent> events)
    {
        if (card.ObjectId == seat.IdentityCard.ObjectId)
        {
            // `rr:defeat.2`: "if an identity or stage of the villain is
            // defeated, it is **removed from the game**."
            Remove(world, card, trigger, events);
            return;
        }

        if (StateFields.Modified(world, card, "permanent", facts, world.Players) > 0)
        {
            // `.1` and `.2`. A non-attachment permanent is removed from the
            // game; an attachment resolves its "attach to" text first, and that
            // text is not modelled.
            if (facts.Kind(card.FaceId) == CardKind.Attachment)
            {
                throw new RulesNotImplementedException(
                    $"card {card.ObjectId} is a permanent attachment on an eliminated "
                    + "player's board, and rr:player-elimination.1 resolves its "
                    + "'attach to' text, which is not modelled");
            }

            Remove(world, card, trigger, events);
            return;
        }

        // `.3` and step 4 are the same instruction from two sides: each card
        // goes to **its owner's** discard pile, which is what `Discard.Card`
        // reads off the card rather than being told.
        Discard.Card(world, card, trigger, events);
    }

    private static void Remove(World world, Card card, string trigger, List<GameEvent> events)
    {
        var constantsEnding = world.Effects.PreflightConstantsEnding(card);
        using var departure = constantsEnding.Begin();
        Discard.Attachments(world, card, trigger, events);
        if (DeckTypes.IsInPlay(card.Area.Type))
        {
            Discard.ResetLeavingState(world, card, trigger, events);
        }
        var removed = world.AreaOf(DeckType.RemovedArea);
        var from = card.Area;
        World.MoveToTop(card, removed);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(removed),
            [new Landing(card.ObjectId, removed.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Eliminate",
        });
        constantsEnding.Complete(trigger, events);
    }

    /// <summary>Proves every step-three/four departure before elimination mutates state.</summary>
    private static void Preflight(World world, ICardFacts facts, int player)
    {
        var layout = EliminationLayout.Calculate(new WorldEliminationLayout(world), player);
        var leaving = layout.Leaving.Select(card => world.Cards[card])
            .Where(card => DeckTypes.IsInPlay(card.Area.Type))
            .ToList();

        foreach (var card in leaving)
        {
            if (facts.Kind(card.FaceId) == CardKind.Attachment
                && StateFields.Modified(
                    world, card, "permanent", facts, world.Players) > 0)
            {
                throw new RulesNotImplementedException(
                    $"card {card.ObjectId} is a permanent attachment on an eliminated "
                    + "player's board, and rr:player-elimination.1 resolves its "
                    + "'attach to' text, which is not modelled");
            }
        }

        foreach (var card in leaving)
        {
            Discard.PreflightAttachments(world, card);
        }
    }
}

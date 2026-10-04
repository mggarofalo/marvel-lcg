using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.Game;

namespace Marvel.Rules.Play;

/// <summary>Engine-authored hand replacement and discard decisions.</summary>
internal static class GameHandOffers
{
    internal static Prompt MulliganPrompt(this Game game)
    {
        var seat = game.world.Seats[game.Active];
        return new Prompt(
            Player: seat.Index,
            Asking: Question.TurnOption,
            When: Timing.TimingPriority.Untimed,
            Trigger: MulliganTrigger,
            Label: $"{seat.Name} resolves mulligans",
            // `rr:appendix-ii-setup.step.15` gives a player one thing to do
            // and lets them do none of it: "each player **may** discard any
            // number of cards from hand". Taking it with an empty list and
            // declining are the same answer, so a cancel would mean the same
            // thing twice.
            Cancellable: false,
            Affordances: [game.HandChoice(seat, ResolveMulligans)])
        {
            DisplayQuestion = "Opening hand",
            PublicKind = PublicDecisionKind.OpeningHand,
        };
    }

    internal static Prompt EndPhasePrompt(this Game game)
    {
        var seat = game.world.Seats[game.Active];
        return new Prompt(
            Player: seat.Index,
            Asking: Question.TurnOption,
            When: Timing.TimingPriority.Untimed,
            Trigger: EndPhaseTrigger,
            Label: $"{seat.Name} End Phase",
            Cancellable: false,
            Affordances:
            [
                // `rr:end-of-player-phase.step.1` is two clauses, and the
                // second is a floor: a player "**must** discard down to their
                // hand size if they have more cards than their hand size". So
                // an over-full hand cannot answer with nothing, and the
                // affordance has to say so — `PhaseEnd.DiscardToHandSize`
                // refuses an answer that leaves too many, and an engine that
                // offers what it will refuse has told the client a lie.
                game.HandChoice(
                    seat,
                    EndPhaseVerb,
                    Math.Max(
                        0,
                        seat.Hand.Cards.Count - (int)PhaseEnd.HandSize(game.world, seat, game.facts))),
            ])
        {
            DisplayQuestion = "Choose end-of-phase discards",
            PublicKind = PublicDecisionKind.EndPhaseDiscards,
            Description = "Discard any number from your hand; discard down to hand size if over it. "
                + "After all players choose, draw up to hand size, then ready player and encounter cards.",
        };
    }

    /// <summary>An affordance offering some number of the player's hand.</summary>
    /// <remarks>
    /// The mulligan and the end phase are nearly the same shape: choose between
    /// <paramref name="least"/> and all of your hand. They differ only in the
    /// floor — <c>rr:appendix-ii-setup.step.15</c> lets a player mulligan "any
    /// number of cards", including none, while the end of the player phase has
    /// a hand size to come down to.
    /// <para>
    /// The candidate list is the hand in its own order, not sorted — the
    /// recorded offer is <c>[42, 45, 37, 9, 47, 46]</c>, which is the hand read
    /// bottom to top, and sorting it would change which card a client
    /// highlights first.
    /// </para>
    /// </remarks>
    internal static Affordance HandChoice(this Game game, Seat seat, string verb, int least = 0)
    {
        var hand = new int[seat.Hand.Cards.Count];
        for (int index = 0; index < hand.Length; index++)
        {
            hand[index] = seat.Hand.Cards[index].ObjectId;
        }

        return game.Anchored(verb, seat) with
        {
            Description = verb == EndPhaseVerb
                ? "Choose cards to discard. Unselected cards stay in your hand." : null,
            Targets = new TargetRequest(
                Legal: hand,
                Min: least,
                Max: hand.Length,
                // This is a presentation marker for choosing from a card
                // collection rather than clicking cards already laid out on
                // the table. The cooperative product exposes player hands by
                // default, so it is not itself an information boundary.
                IsSearch: true)
            {
                Details = hand.ToDictionary(id => id, _ => verb == EndPhaseVerb
                    ? "Discard this card when you confirm phase-end discards."
                    : "Discard and replace this card when you confirm your mulligan."),
            },
        };
    }

}

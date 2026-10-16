namespace Marvel.Rules.State;

/// <summary>Places setup cards and captures deferred encounter entry work.</summary>
internal static class SetupCardPlacement
{
    /// <summary>Records encounter cards that entered play since the last setup step.</summary>
    internal static void TrackSetupEntrants(
        World world,
        ICardFacts facts,
        List<Events.GameEvent> happened,
        ref int cursor,
        List<(int Card, int Incarnation)> pending,
        HashSet<(int Card, int Incarnation)> seen)
    {
        foreach (var moved in happened.Skip(cursor).OfType<Events.CardsMoved>())
        {
            // A reveal procedure resolves its own When Revealed ability, and a
            // card explicitly put into play does not trigger one —
            // rr:when-revealed-abilities.2. This queue is only for setup entry
            // that has not already taken either of those two paths.
            if (moved.From.Zone == DeckType.RevealingArea.ToString()
                || moved.Verb == "Put_Into_Play")
            {
                continue;
            }

            foreach (var landing in moved.Cards)
            {
                var card = world.Cards[landing.Card];
                var address = (card.ObjectId, card.Incarnation);
                if (moved.To.Zone == card.Area.Type.ToString()
                    && DeckTypes.IsInPlay(card.Area.Type)
                    && IsDeferredSetupEncounterCard(facts, card)
                    && seen.Add(address))
                {
                    pending.Add(address);
                }
            }
        }

        cursor = happened.Count;
    }

    private static bool IsDeferredSetupEncounterCard(ICardFacts facts, Card card) =>
        card.Owner == World.Scenario
        && EffectiveCards.Kind(card, facts) is CardKind.Attachment or CardKind.Obligation
            or CardKind.Treachery or CardKind.Minion or CardKind.EncounterSideScheme
            or CardKind.Environment;

    /// <summary>
    /// Step 11 — every card with the setup keyword, put into play.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Put into play, not revealed.</b>
    /// <c>rr:when-revealed-abilities.2</c>: "if an encounter card with a 'When
    /// Revealed' ability is put into play <b>without being revealed</b>, the
    /// 'When Revealed' ability does not trigger." So this places the card and
    /// runs its keywords — <c>rr:enters-play</c> — and nothing else, which is
    /// exactly what <see cref="Play.Reveal.Resolve"/> does.
    /// </para>
    /// <para>
    /// <b>A card it cannot place after scenario setup stops the deal.</b> Where
    /// a card goes comes from its type for a side scheme or an environment, and
    /// from its own text for an attachment (<c>rr:attach-to</c>) or for a
    /// scenario's ally or support (<c>rr:ownership-and-control.2.2</c>, "when a
    /// player takes control of a […] player card with a player card back"). A card whose
    /// text nobody has read is placed nowhere. Scenario setup may first create
    /// the named destination, so unresolved cards are retried through step 12;
    /// leaving one in the pile after that would deal a board quietly missing a
    /// card the rules put on the table.
    /// </para>
    /// </remarks>
    internal static List<Card> SetupCards(
        World world, ICardFacts facts, List<Events.GameEvent> happened,
        IReadOnlyList<Card>? candidates = null)
    {
        var pending = new List<Card>();
        // A copy, because putting a card into play moves it between areas and
        // `World.Cards` is walked by object id rather than by place.
        foreach (var card in candidates ?? world.Cards.ToList())
        {
            if (facts.PrintedValue(card.FaceId, "Setup", world.Players) <= 0
                || DeckTypes.IsInPlay(card.Area.Type))
            {
                continue;
            }

            var from = card.Area;
            int? controller = world.SetupAbilities.SetupController(world, card);
            if (controller is { } player)
            {
                Play.CardControlTransfer.TakeScenarioCardOwnership(facts, card, player);
            }
            Play.Reveal.Resolve(
                world, facts, card, world.FirstPlayer, happened,
                placement: new Play.CardEntryPlacement(SetupController: controller));

            if (card.Area == from)
            {
                pending.Add(card);
            }
        }

        return pending;
    }
}

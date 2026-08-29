using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// The main scheme deck — <c>rr:main-scheme-main-scheme-deck</c>.
/// </summary>
/// <remarks>
/// "The main scheme represents the villain's primary objective." A sequential
/// deck like the villain's, advanced by threat rather than by damage, and the
/// two are symmetrical: the players win by defeating the last villain stage
/// and lose by letting the last main scheme complete.
/// </remarks>
public static class MainScheme
{
    /// <summary>
    /// How much extra threat step 1 places — <c>rr:villain-phase.step.1</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If any acceleration <b>icons or tokens</b> are active, additional
    /// threat equal to the number of such icons and tokens is also placed at
    /// this time." Two sources that count the same and are deliberately not the
    /// same thing — <c>rr:acceleration-icon.3</c> and
    /// <c>rr:acceleration-token.4</c> each say the one is not the other, which
    /// matters to a card that removes one of them.
    /// </para>
    /// <para>
    /// Icons are printed on encounter cards and go away when the card does
    /// (<c>rr:acceleration-icon.2</c>). Tokens are placed by
    /// <c>rr:encounter-deck.1</c> and by card abilities, and the ones on the
    /// main scheme never go away at all.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    public static long Acceleration(World world, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        long total = 0;
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                total += StateFields.Modified(
                    world, card, "acceleration_icon", facts, world.Players);

                // `rr:acceleration-token.2.1`: tokens "placed on cards other
                // than the main scheme still add threat to the main scheme",
                // so this counts them wherever they sit.
                total += card.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
            }
        }

        return total;
    }

    /// <summary>
    /// Whether a crisis icon is in play — <c>rr:crisis-icon</c>.
    /// </summary>
    /// <remarks>
    /// "While <b>at least one</b> crisis icon is in play, threat cannot be
    /// removed from the main scheme by player cards." One is as good as five,
    /// so this is a question and not a count.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    public static bool Crisis(World world, ICardFacts facts) =>
        IconsInPlay(world, facts, "crisis") > 0;

    /// <summary>
    /// How many boost icons every boost card gains — <c>rr:amplify-icon</c>.
    /// </summary>
    /// <remarks>
    /// "When a boost card is turned faceup <b>during an enemy activation</b>,
    /// add one additional boost icon to that card for each amplify icon in
    /// play", written in <c>.1</c> as the constant ability "each boost card
    /// gains [boost]".
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    public static long Amplify(World world, ICardFacts facts) =>
        IconsInPlay(world, facts, "amplify");

    /// <summary>One printed icon, totalled over everything in play.</summary>
    private static long IconsInPlay(World world, ICardFacts facts, string field)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        long total = 0;
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                total += StateFields.Modified(world, card, field, facts, world.Players);
            }
        }

        return total;
    }

    /// <summary>
    /// Advances the main scheme deck — <c>rr:main-scheme-main-scheme-deck.3</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three steps, in this order:
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     "Remove the top main scheme card from the game. Return all tokens
    ///     <b>(except acceleration tokens)</b> that were on that card to the
    ///     token pool and discard each card attached to it."
    ///   </description></item>
    ///   <item><description>
    ///     "Resolve any <b>When Revealed</b> ability on the <b>A</b> side of
    ///     the new top card of the main scheme deck."
    ///   </description></item>
    ///   <item><description>
    ///     "Flip the top card of the main scheme deck to its <b>B</b> side,
    ///     place threat on that card equal to its starting threat value, and
    ///     resolve any <b>When Revealed</b> ability on that side of the card."
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Both sides get a When Revealed window, and they are different
    /// abilities.</b> That is why the card is turned to its A face first rather
    /// than going straight to B: reading only the B side would silently drop
    /// every A-side ability in the pool.
    /// </para>
    /// <para>
    /// <c>rr:main-scheme-main-scheme-deck.4</c>: excess threat does not carry
    /// over — the new stage's threat is its starting threat and nothing else.
    /// <c>.5</c>: acceleration tokens do carry over, which is step 1's
    /// parenthesis.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="scheme">The completed scheme.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Advance(
        World world, ICardFacts facts, ICardAbilities abilities, Card scheme,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        var deck = world.AreaOf(DeckType.MainSchemesDeck);
        var next = deck.Cards.Count > 0 ? deck.Cards[^1] : null;
        if (next is null)
        {
            throw new RulesNotImplementedException(
                $"card {scheme.ObjectId} advanced with an empty main scheme deck");
        }

        // Step 1. Attached cards are discarded before the card leaves, so that
        // a client sees them go rather than vanishing with their host.
        var constantsEnding = world.Effects.PreflightConstantsEnding(scheme);
        using var departure = constantsEnding.Begin();
        Discard.Attachments(world, scheme, trigger, events);

        long carried = scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        var removed = world.AreaOf(DeckType.RemovedArea);
        var from = scheme.Area;
        World.MoveToTop(scheme, removed);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(removed),
            [new Landing(scheme.ObjectId, removed.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Advance",
        });
        constantsEnding.Complete(trigger, events);

        // Step 2. The A side first, and its ability, before anything is placed.
        var area2 = world.AreaOf(DeckType.MainSchemesArea);
        World.MoveToTop(next, area2);
        next.TurnTo(next.Faces[0]);
        next.TurnFaceUp();
        events.Add(new CardsMoved(
            Places.Reference(deck), Places.Reference(area2),
            [new Landing(next.ObjectId, area2.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Advance",
        });

        events.AddRange(abilities.WhenRevealed(world, next, world.FirstPlayer));

        // Step 3. Then the B side, its starting threat, and its ability.
        next.TurnTo(next.Faces[^1]);
        events.Add(new CardsFlipped([next.ObjectId], true)
        {
            Trigger = trigger, Verb = "Advance",
        });

        // `.5` -- the acceleration tokens come across; `.4` -- the threat does
        // not, so this is the starting threat and nothing that was on the old
        // stage.
        if (carried > 0)
        {
            next.PlaceTokens(EncounterDeck.AccelerationToken, carried);
        }

        long starting = facts.PrintedValue(next.FaceId, "StartingThreat", world.Players);
        if (starting > 0)
        {
            next.PlaceTokens("k_threat", starting);
            events.Add(new FieldSet(next.ObjectId, "k_threat", 0, starting)
            {
                Trigger = trigger, Verb = "Advance",
            });
        }

        events.AddRange(abilities.WhenRevealed(world, next, world.FirstPlayer));
    }
}

using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Threat placed on a scheme, and what reaching a target does.
/// </summary>
/// <remarks>
/// <para>
/// <b>Threat placed is threat placed, however it arrived.</b>
/// <c>rr:main-scheme-main-scheme-deck.2</c> completes a scheme the moment its
/// threat reaches its target threat value and says nothing at all about what
/// put the threat there — so the villain's activation, the acceleration field,
/// the incite keyword and a card's own ability are one rule and belong in one
/// place.
/// </para>
/// <para>
/// They were not. The villain phase placed threat and looked; the incite
/// keyword placed threat inline and did not. A scenario one threat short of
/// its target would carry on past its own ending — every later round a round
/// that should never have been played — and thirty-three cards in the pool
/// carry incite.
/// </para>
/// <para>
/// <b>Checked after each placement rather than at the end of a step.</b> A
/// scheme completed halfway through the villain's activation ends the game
/// there, and the encounter cards that would have followed are never dealt.
/// Checking at the end of the step instead would deal them first.
/// </para>
/// </remarks>
public static class Threat
{
    /// <summary>
    /// Places threat on a scheme, and resolves the scheme if that completed it.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do, for a stage that advances.</param>
    /// <param name="scheme">Which scheme.</param>
    /// <param name="amount">How much. Zero does nothing at all, not even an event.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Place(
        World world, ICardFacts facts, ICardAbilities abilities, Card scheme, long amount,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        if (amount != 0)
        {
            long before = scheme.Tokens.GetValueOrDefault("k_threat");
            scheme.PlaceTokens("k_threat", amount);
            events.Add(new FieldSet(scheme.ObjectId, "k_threat", before, before + amount)
            {
                Trigger = trigger, Verb = "Place_Threat",
            });
        }

        Completed(world, facts, abilities, scheme, events);
    }

    /// <summary>
    /// Resolves a main scheme that has reached its target threat —
    /// <c>rr:main-scheme-main-scheme-deck.2</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.2.2</c> is why the completed flag is set here rather than inside
    /// <see cref="MainScheme.Advance"/>: "if the main scheme advances other
    /// than through having threat on it equal to or greater than its target
    /// threat value, that main scheme is <b>not</b> considered completed."
    /// </para>
    /// <para>
    /// <c>.2.1</c> — "if the villain completes the final stage of the main
    /// scheme deck, the villain wins the game." An empty main scheme deck is
    /// therefore an ending and not a missing card.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do, for a stage that advances.</param>
    /// <param name="scheme">Which scheme.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Completed(
        World world, ICardFacts facts, ICardAbilities abilities, Card scheme,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        long held = scheme.Tokens.GetValueOrDefault("k_threat");
        long target = facts.PrintedValue(scheme.FaceId, "TargetThreat", world.Players);
        if (target <= 0 || held < target || scheme.Area.Type != DeckType.MainSchemesArea)
        {
            // A side scheme has a target threat value too, and reaching it does
            // nothing: `rr:side-scheme.2` keeps a side scheme in play "until there is no
            // threat on it", which is the opposite direction.
            return;
        }

        scheme.PlaceTokens("is_completed", 1);
        events.Add(new FieldSet(scheme.ObjectId, "is_completed", 0, 1)
        {
            Trigger = "main scheme completed", Verb = "Complete",
        });

        if (world.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0)
        {
            MainScheme.Advance(world, facts, abilities, scheme, "main scheme completed", events);
            return;
        }

        world.Finish(Outcome.VillainWins);
    }
}

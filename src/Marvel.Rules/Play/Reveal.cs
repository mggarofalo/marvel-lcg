using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Where a revealed encounter card goes — <c>rr:reveal.step.2</c>.
/// </summary>
/// <remarks>
/// <para>
/// The rule is a list by card type and this is that list. It runs <b>before</b>
/// step 3's "When Revealed" abilities, which matters: a minion that entered
/// play is already engaged when its own ability resolves, and a side scheme is
/// already somewhere threat can be placed on it.
/// </para>
/// <para>
/// Three of the seven cases say "place it on the table in front of the player
/// revealing it <i>(it is not in play)</i>", which is the revealing area this
/// engine already uses — so an attachment with no "attach to" text, a treachery
/// and an "other" all stay where they were put, and step 4 discards them.
/// </para>
/// </remarks>
public static class Reveal
{
    /// <summary>
    /// The keywords that fire when a card is revealed —
    /// <c>rr:surge</c> and <c>rr:incite-x</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both are written as "When Revealed" abilities the keyword provides
    /// (<c>rr:surge.1</c>, <c>rr:incite-x.1</c>), which is why they run in step
    /// 3 beside a card's own text rather than in step 2 with the placement.
    /// </para>
    /// <para>
    /// <c>rr:surge.2</c>: "complete the process of resolving the original card,
    /// as well as any response abilities that are triggered by that card being
    /// revealed, <b>before revealing the additional card</b>." The dealt card
    /// joins the queue and step 4 finds it after this one is finished, which is
    /// that clause with nothing extra needed.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="card">The card being revealed.</param>
    /// <param name="player">The seat revealing it.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Keywords(
        World world, ICardFacts facts, Card card, int player, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:incite-x`: "place X threat on the main scheme."
        long incite = facts.PrintedValue(card.FaceId, "Incite", world.Players);
        if (incite > 0 && world.TheCardIn(DeckType.MainSchemesArea) is { } scheme)
        {
            long before = scheme.Tokens.GetValueOrDefault("k_threat");
            scheme.PlaceTokens("k_threat", incite);
            events.Add(new FieldSet(scheme.ObjectId, "k_threat", before, before + incite)
            {
                Trigger = "incite", Verb = "Reveal",
            });
        }

        // `rr:surge`: "the player resolving the card deals themself a facedown
        // encounter card from the top of the encounter deck."
        if (facts.PrintedValue(card.FaceId, "Surge", world.Players) > 0)
        {
            Deal.EncounterCard(world, player, "surge", events);
        }
    }

    /// <summary>Puts a revealed card where its type says it goes.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="card">The card being revealed.</param>
    /// <param name="player">The seat revealing it.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Resolve(
        World world, ICardFacts facts, Card card, int player, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        var into = facts.Kind(card.FaceId) switch
        {
            // `rr:reveal.3` and `rr:minion`: "it enters play in the play area of
            // the player revealing it. **It is considered to engage that
            // player.**" Engagement is not a flag beside the minion -- it is
            // which area the minion is in, which is what `rr:engage` describes.
            CardKind.Minion => world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)),

            // `rr:reveal.5`: "it enters play in the villain's play area."
            CardKind.EncounterSideScheme => world.AreaOf(DeckType.SideSchemesArea),

            // `rr:reveal.4`: "it enters play in the play area of the player
            // revealing it."
            CardKind.Obligation => world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(player)),

            // `rr:reveal.6` and `.7`, and `.1` for an attachment with no
            // "attach to" text: on the table in front of the player, which is
            // not in play. Step 4 discards a treachery from there.
            _ => null,
        };

        if (into is null)
        {
            return;
        }

        var from = card.Area;
        World.MoveToTop(card, into);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)])
        {
            Trigger = "villain phase", Verb = "Reveal",
        });

        EnterPlay(world, facts, card, events);
    }

    /// <summary>
    /// The keywords that fire when a card enters play.
    /// </summary>
    /// <remarks>
    /// <c>rr:reveal.step.3</c> resolves each "When Revealed" ability "<b>(including
    /// those provided by keywords)</b>", and <c>rr:keywords</c> writes these two
    /// out as exactly that.
    /// </remarks>
    public static void EnterPlay(
        World world, ICardFacts facts, Card card, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:toughness.1`: "**Forced Response**: after this character enters
        // play, give it a tough status card." A status is a card, not a flag --
        // see `Statuses`.
        if (facts.PrintedValue(card.FaceId, "Toughness", world.Players) > 0
            && !Statuses.Has(world, card, Statuses.Tough))
        {
            var status = Statuses.Give(world, card, Statuses.Tough);
            events.Add(new CardsCreated(
                Places.Reference(status.Area),
                [new CreatedCard(status.ObjectId, status.FaceId)])
            {
                Trigger = "toughness", Verb = "Enter_Play",
            });
        }

        // `rr:hinder-x`: "a card with the hinder X keyword enters play with X
        // threat on it."
        long hinder = facts.PrintedValue(card.FaceId, "Hinder", world.Players);
        if (hinder > 0)
        {
            card.PlaceTokens("k_threat", hinder);
            events.Add(new FieldSet(card.ObjectId, "k_threat", 0, hinder)
            {
                Trigger = "hinder", Verb = "Enter_Play",
            });
        }

        // `rr:side-scheme`: a side scheme enters play with its starting threat.
        // Separate from hinder and they add -- one is a keyword and the other
        // is the printed field every scheme has.
        long starting = facts.PrintedValue(card.FaceId, "StartingThreat", world.Players);
        if (starting > 0)
        {
            long before = card.Tokens.GetValueOrDefault("k_threat");
            card.PlaceTokens("k_threat", starting);
            events.Add(new FieldSet(card.ObjectId, "k_threat", before, before + starting)
            {
                Trigger = "starting threat", Verb = "Enter_Play",
            });
        }
    }
}

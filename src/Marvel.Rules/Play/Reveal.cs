using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

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
    /// <summary>Resolves every printed keyword reached while revealing a card.</summary>
    public static void Keywords(
        World world, ICardFacts facts, IThreatCardAbilities abilities, Card card, int player,
        List<GameEvent> events, Occurrence? occurrence = null) =>
        RevealKeywords.Keywords(world, facts, abilities, card, player, events, occurrence);

    /// <summary>Keyword-created abilities waiting on a revealed card.</summary>
    public static IReadOnlyList<PendingAbility> KeywordAbilities(
        World world, ICardFacts facts, Card card, int player) =>
        RevealKeywords.KeywordAbilities(world, facts, card, player);

    /// <summary>The player named by a revealed card, if any.</summary>
    public static int? Names(World world, ICardFacts facts, Card card) =>
        RevealKeywords.Names(world, facts, card);

    /// <summary>The stable ledger address of keyword-provided Incite.</summary>
    public const int InciteResolutionOrdinal = -1;

    /// <summary>The stable ledger address of keyword-provided Surge.</summary>
    public const int SurgeResolutionOrdinal = -2;

    /// <summary>
    /// Gives a character a status, and resolves what follows —
    /// <c>rr:status-cards</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one door into <see cref="Statuses"/> for anything that <i>inflicts</i>
    /// a status, because three rules meet here and only one of them is about
    /// the card appearing. <c>rr:status-cards.1</c> caps how many a character
    /// can hold, <c>rr:stalwart</c> makes that cap zero, and
    /// <c>rr:vulnerable</c> discards the character outright.
    /// </para>
    /// <para>
    /// <c>rr:vulnerable.2</c>: "it is <b>discarded</b> before the damage is
    /// applied and is <b>not considered defeated</b>" — so no "When Defeated"
    /// ability fires and nothing reaches the victory display, which is what
    /// separates this from <see cref="Defeat"/>.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="host">The character.</param>
    /// <param name="status">The status's printed id.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>The status card, or null when the character could not take one.</returns>
    public static Card? Afflict(
        World world, ICardFacts facts, Card host, string status,
        string trigger, List<GameEvent> events) =>
        StatusAffliction.Apply(world, facts, host, status, trigger, events);

    /// <summary>Puts a revealed card where its type says it goes.</summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="card">The card being revealed.</param>
    /// <param name="player">The seat revealing it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="occurrence">The reveal occurrence, when this move is part of one.</param>
    /// <param name="placement">An explicit attachment host, setup controller or placement event.</param>
    public static void Resolve(
        World world, ICardFacts facts, Card card, int player, List<GameEvent> events,
        Occurrence? occurrence = null, CardEntryPlacement? placement = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        if (EffectiveCards.Kind(card, facts) != CardKind.EncounterVillain
            && Uniqueness.IsBlocked(world, facts, card))
        {
            // `rr:unique-icon.4.2`: a matching non-villain encounter card is
            // discarded without entering play. When this is a reveal, the
            // revealing player receives one facedown encounter card.
            Discard.Card(world, card, "unique", events);
            if (occurrence is not null)
            {
                Deal.EncounterCard(world, player, "unique", events);
            }
            return;
        }

        placement ??= new CardEntryPlacement();
        Area? into = CardEntryDestination.For(world, facts, card, player, placement);

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
            Trigger = "villain phase",
            Verb = placement.Verb,
        });

        if (into.Host >= 0)
        {
            events.Add(new CardAttached(card.ObjectId, into.Host)
            {
                Trigger = "villain phase",
                Verb = placement.Verb,
            });
        }
        EnterPlay(world, facts, card, events, occurrence);
    }

    /// <summary>
    /// The counters a card enters play with, and what they are called —
    /// <c>rr:uses-x-type</c>.
    /// </summary>
    /// <remarks>
    /// Printed as one field holding both, <c>"3,web"</c>. Sixty-nine cards in
    /// the pool carry one, and the type is a word the card's own ability spends
    /// by name — which is why the count alone would not do.
    /// <para>
    /// Removal belongs to the ability spending a counter; the card DSL's
    /// <c>removeCounters</c> cost also discards a uses card when the last one
    /// leaves it.
    /// </para>
    /// </remarks>
    /// <param name="printed">The card's printed attributes.</param>
    public static (long Count, string Type) Uses(
        IReadOnlyDictionary<string, string> printed)
    {
        ArgumentNullException.ThrowIfNull(printed);
        if (!printed.TryGetValue("Uses", out string? uses))
        {
            return (0, string.Empty);
        }

        string[] parts = uses.Split(',');
        return parts.Length == 2 && long.TryParse(parts[0], out long count)
            ? (count, parts[1])
            : throw new RulesNotImplementedException(
                $"the uses keyword '{uses}' is not a count and a type");
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
        World world, ICardFacts facts, Card card, List<GameEvent> events,
        Occurrence? occurrence = null, ICardPlayAbilities? abilities = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        CardEntryState.EstablishOwnership(world, facts, card);

        // `rr:toughness.1`: "**Forced Response**: after this character enters
        // play, give it a tough status card." A status is a card, not a flag --
        // see `Statuses`.
        CardEntryState.GiveToughness(world, facts, card, events);

        // `rr:uses-x-type`: a Uses pool is placed only while that keyword is
        // active. Ordinary card text can define the same physical starting
        // state without gaining the discard-at-zero rule, so the card layer
        // supplies both the pool and which printed form created it.
        CardEntryState.PlaceCounters(world, card, events, abilities);

        // `rr:hinder-x`: "a card with the hinder X keyword enters play with X
        // threat on it."
        CardEntryState.PlaceThreat(world, facts, card, events);

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
                Trigger = "starting threat",
                Verb = "Enter_Play",
            });
        }

        // Card-specific entry state is still part of the transition and must
        // exist before an "after this card enters play" response is read.
        ActivityBatches.Append(events, (abilities ?? world.CardPlayAbilities).EntersPlay(world, card));

        // A newly active constant may grant stalwart to its own character or
        // to another one already in play. `rr:stalwart.2` removes both kinds of
        // forbidden status card as part of gaining the keyword, before the
        // after-entry response window opens.
        Statuses.RemoveAfflictionsIfStalwart(world, facts, "stalwart", events);
        occurrence?.Also(Steps.CardEntersPlay);
    }

}

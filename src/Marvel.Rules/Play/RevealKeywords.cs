using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves reveal keywords and their ordered ability choices.</summary>
public static class RevealKeywords
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
    /// <param name="abilities">What cards do, for a scheme this completes.</param>
    /// <param name="card">The card being revealed.</param>
    /// <param name="player">The seat revealing it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="occurrence">The reveal occurrence retaining resolution status.</param>
    public static void Keywords(
        World world, ICardFacts facts, IThreatCardAbilities abilities, Card card, int player,
        List<GameEvent> events, Occurrence? occurrence = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        var keywordAbilities = KeywordAbilities(world, facts, card, player);
        if (occurrence is not null && facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            // Keyword-provided abilities have no authored-data ordinal. These
            // stable negative addresses are the engine's spelling, kept apart
            // from every printed/data-order ordinal used by the card DSL.
            occurrence.BeginCard(
                card.ObjectId,
                keywordAbilities);
        }

        foreach (var ability in keywordAbilities)
        {
            ResolveKeyword(
                world, facts, abilities, card, player, ability, events, occurrence);
        }
    }

    /// <summary>Resolve one keyword-provided When Revealed ability by address.</summary>
    public static void ResolveKeyword(
        World world, ICardFacts facts, IThreatCardAbilities abilities, Card card, int player,
        PendingAbility ability, List<GameEvent> events, Occurrence? occurrence = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        if (ability.Card != card.ObjectId)
        {
            throw new RulesNotImplementedException(
                $"keyword ability on card {ability.Card} cannot resolve for card {card.ObjectId}");
        }

        switch (ability.Ordinal)
        {
            case Reveal.InciteResolutionOrdinal:
                long incite = StateFields.Modified(
                    world, card, "incite", facts, world.Players);
                if (incite > 0 && world.TheCardIn(DeckType.MainSchemesArea) is { } scheme)
                {
                    Threat.Place(world, facts, abilities, scheme, incite, "incite", events);
                    CompleteKeyword(occurrence, ability, applied: true);
                }
                else
                {
                    CompleteKeyword(occurrence, ability, applied: false);
                }
                break;

            case Reveal.SurgeResolutionOrdinal:
                bool dealt = Deal.EncounterCard(world, player, "surge", events) is not null;
                CompleteKeyword(occurrence, ability, dealt);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"keyword reveal ability ordinal {ability.Ordinal} is not implemented");
        }
    }

    internal static void CompleteKeyword(
        Occurrence? occurrence, PendingAbility? ability, bool applied)
    {
        if (occurrence is null || ability is not { } pending)
        {
            return;
        }
        if (applied)
        {
            occurrence.Resolve(pending);
        }
        occurrence.Complete(pending);
    }

    /// <summary>The stable ledger addresses of keyword-provided reveal abilities.</summary>
    public static IReadOnlyList<PendingAbility> KeywordAbilities(
        World world, ICardFacts facts, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);

        var abilities = new List<PendingAbility>();
        if (StateFields.Modified(world, card, "incite", facts, world.Players) > 0)
        {
            abilities.Add(new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player,
                Reveal.InciteResolutionOrdinal));
        }
        if (StateFields.Modified(world, card, "surge", facts, world.Players) > 0)
        {
            abilities.Add(new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player,
                Reveal.SurgeResolutionOrdinal));
        }
        return abilities;
    }

    /// <summary>
    /// The seat a card names, when it names one — <c>rr:obligation.4</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If an obligation card is revealed from the encounter deck and that
    /// obligation instructs that it must be given to a specific player
    /// <i>(such as "Give to the Peter Parker player")</i>, place that
    /// obligation into the play area of the player who controls the associated
    /// identity."
    /// </para>
    /// <para>
    /// <b>Matched against every face, not only the one showing.</b> The card
    /// names an alter-ego and the player may be in hero form;
    /// <c>rr:identity.2</c> makes a title name one identity — "if a card refers
    /// to a hero or alter-ego by title, it refers only to the identity with
    /// that title" — so either face answering is the same identity either way.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="card">The card being revealed.</param>
    /// <returns>
    /// The seat, <c>-1</c> when the card names a player who is not in this
    /// game, and <c>null</c> when it names nobody.
    /// </returns>
    public static int? Names(World world, ICardFacts facts, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);

        string wanted = facts.Attributes(card.FaceId).GetValueOrDefault("GiveTo", string.Empty);
        if (wanted.Length == 0)
        {
            return null;
        }

        foreach (int seat in world.PlayerOrder)
        {
            var identity = world.Seats[seat].IdentityCard;
            if (identity.Faces.Any(face => string.Equals(
                facts.Title(face), wanted, StringComparison.Ordinal)))
            {
                return seat;
            }
        }

        return -1;
    }
}

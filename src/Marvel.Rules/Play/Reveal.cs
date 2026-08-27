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
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(events);

        var given = Statuses.Inflict(world, facts, host, status);
        if (given is null)
        {
            return null;
        }

        events.Add(new CardsCreated(
            Places.Reference(given.Area), [new CreatedCard(given.ObjectId, given.FaceId)])
        {
            Trigger = trigger, Verb = "Give_Status",
        });
        events.Add(new CardAttached(given.ObjectId, host.ObjectId)
        {
            Trigger = trigger, Verb = "Give_Status",
        });

        if (Statuses.Vulnerable(world, facts, host))
        {
            Discard.Card(world, host, trigger, events);
        }

        return given;
    }

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
    public static void Keywords(
        World world, ICardFacts facts, ICardAbilities abilities, Card card, int player,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:incite-x`: "place X threat on the main scheme." Through
        // `Threat.Place` and not inline, because threat that reaches the
        // scheme's target completes it whatever put it there -- and this used
        // to place it and not look.
        long incite = facts.PrintedValue(card.FaceId, "Incite", world.Players);
        if (incite > 0 && world.TheCardIn(DeckType.MainSchemesArea) is { } scheme)
        {
            Threat.Place(world, facts, abilities, scheme, incite, "incite", events);
        }

        // `rr:surge`: "the player resolving the card deals themself a facedown
        // encounter card from the top of the encounter deck."
        if (facts.PrintedValue(card.FaceId, "Surge", world.Players) > 0)
        {
            Deal.EncounterCard(world, player, "surge", events);
        }
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

            // `rr:reveal.2`: the same place, and an area of its own because
            // `rr:environment` makes an environment "active so long as it
            // remains in play" rather than something that resolves and goes.
            CardKind.Environment => world.AreaOf(DeckType.EnvironmentArea),

            // `rr:reveal.4`: "it enters play in the play area of the player
            // revealing it."
            CardKind.Obligation => world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(player)),

            // `rr:attach-to`: "if a card uses the phrase 'attach to', it must
            // be attached to *(placed beneath and slightly overlapped by)* the
            // specified game element **as it enters play**." A rule about the
            // phrase rather than an ability, so it is answered here on the way
            // in and not by a "When Revealed" — `rr:when-revealed-abilities.2`
            // does not trigger one on a card put into play without being
            // revealed, and a setup attachment is exactly that.
            //
            // `rr:attach-to.3.1` is the case this does not reach: "the 'attach
            // to' phrase on a card is not resolved if another ability causes
            // that card to attach to a specific game element." An ability that
            // attaches says so with the `attachTo` node instead, and it moves
            // the card itself rather than coming through here.
            CardKind.Attachment when world.Abilities.AttachesTo(world, card) is { } element
                => world.AreaOf(
                    DeckType.UpgradesArea,
                    world.Cards[element].Area.PlayArea,
                    element,
                    world.Cards[element].Area.CardOwner),

            // `rr:reveal.6` and `.7`, and `rr:attach-to.3` for an attachment
            // whose phrase found nothing — "it remains in its prior state or
            // game area", which here is the table in front of the player and is
            // not in play. Step 4 discards it from there, which is the rest of
            // that clause: "if such a card cannot remain in its prior state or
            // game area, discard it."
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
    /// A minion that brings its friends in — <c>rr:teamwork</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "After a minion with teamwork enters play and engages a player, <b>if
    /// there is at least one other minion that shares the specified trait in
    /// play</b>, the minion that just entered play activates against the player
    /// it is engaged with."
    /// </para>
    /// <para>
    /// <b>The trait is the keyword's argument, and it is a word rather than a
    /// number.</b> <c>Teamwork ([[ACOLYTE]])</c> prints as the attribute
    /// <c>Teamwork = "ACOLYTE"</c>, so this reads the raw attribute table:
    /// <c>PrintedValue</c> would answer zero, which is what it answers for a
    /// card with no teamwork at all. Thirty-one minions carry one.
    /// </para>
    /// <para>
    /// <b>The Rules Reference states this twice and the two differ.</b> The
    /// entry says "at least one other minion <i>that shares the specified
    /// trait</i>"; <c>rr:teamwork.1</c>'s equivalent ability says only "another
    /// minion in play". The trait is followed, because it is what the keyword
    /// prints and what the entry says — a reading that ignored it would
    /// activate an Acolyte beside an unrelated Hydra trooper.
    /// </para>
    /// <para>
    /// <b>It activates; it does not attack.</b> That is the difference from
    /// <see cref="Quickstrike"/>, which says outright that the player must be
    /// in hero form. <c>rr:activation.1</c> reads the form to choose between
    /// attacking and scheming, so a teamwork minion engaging an alter-ego
    /// schemes rather than doing nothing.
    /// </para>
    /// <para>
    /// <c>rr:teamwork.2</c> puts it after the minion's own "When Revealed"
    /// abilities, which is where <see cref="Quickstrike"/> sits too.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="card">The minion that just entered play.</param>
    /// <param name="player">The seat it engaged.</param>
    /// <param name="round">Which round, for the step it schedules.</param>
    public static void Teamwork(
        World world, ICardFacts facts, Card card, int player, int round)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);

        if (card.Area.Type != DeckType.EngagedEnemiesArea
            || !facts.Attributes(card.FaceId).TryGetValue("Teamwork", out string? trait))
        {
            return;
        }

        // "In play" and not "engaged with you": a minion in another player's
        // area is in play, and `rr:engage` is about which area it sits in
        // rather than whether it counts.
        bool company = world.Areas
            .Where(area => area.Type == DeckType.EngagedEnemiesArea)
            .SelectMany(area => area.Cards)
            .Any(other => other.ObjectId != card.ObjectId
                && State.Traits.Has(world, other, trait, facts));

        if (!company)
        {
            return;
        }

        world.Agenda.Then(new PhaseStep(
            Forms.In(world, world.Seats[player], facts, Forms.Hero) ? Steps.Attack : Steps.Scheme,
            round, 2, Index: player, Subject: card.ObjectId, Seat: player));
    }

    /// <summary>
    /// A minion that attacks the moment it engages — <c>rr:quickstrike</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "<b>Forced Response (Hero)</b>: after this minion engages a player, it
    /// attacks that player." The <i>(Hero)</i> is the gate: <c>rr:quickstrike</c>
    /// says "a player <b>whose identity is in hero form</b>", so a minion
    /// engaging an alter-ego does nothing.
    /// </para>
    /// <para>
    /// <c>rr:quickstrike.2</c>: "if a minion with the quickstrike keyword is
    /// being revealed, the quickstrike keyword resolves <b>after</b> any 'When
    /// Revealed' abilities on that minion are resolved" — which is why this is
    /// called from step 3's tail rather than from <see cref="Resolve"/>.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="card">The card that was revealed.</param>
    /// <param name="player">The seat it engaged.</param>
    /// <param name="round">Which round this is.</param>
    public static void Quickstrike(
        World world, ICardFacts facts, Card card, int player, int round)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);

        if (card.Area.Type != DeckType.EngagedEnemiesArea
            || StateFields.Modified(world, card, "quickstrike", facts, world.Players) <= 0
            || !Forms.In(world, world.Seats[player], facts, Forms.Hero))
        {
            return;
        }

        // Scheduled rather than resolved, because an attack is six steps and
        // one of them asks a player something. The reveal that caused it is
        // finished by the time this runs.
        world.Agenda.Then(new PhaseStep(
            Steps.Attack, round, 2, Index: player, Subject: card.ObjectId, Seat: player));
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

        // `rr:uses-x-type`: "when a card with this keyword enters play, place X
        // all-purpose counters from the token pool on the card. The word
        // following the value establishes and identifies the type."
        //
        // Printed as `"3,web"`, so the count and the type arrive together and
        // neither is a number `PrintedValue` could read. The counter's key is
        // the digest's `c_<name>` namespace, which
        // `docs/state-digest-v2.md` reserves for exactly this: "token, counter
        // and form keys come from game data, so the key set is open-ended".
        if (Uses(facts.Attributes(card.FaceId)) is var (count, type) && count > 0)
        {
            card.PlaceTokens("c_" + type, count);
            events.Add(new FieldSet(card.ObjectId, "c_" + type, 0, count)
            {
                Trigger = "uses", Verb = "Enter_Play",
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

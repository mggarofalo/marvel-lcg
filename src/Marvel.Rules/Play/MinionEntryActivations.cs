using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Schedules keyword activations caused by a minion entering play.</summary>
public static class MinionEntryActivations
{
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

        if (!TeamworkApplies(world, facts, card))
        {
            return;
        }

        world.Agenda.Then(new PhaseStep(
            Forms.In(world, world.Seats[player], facts, Forms.Hero)
                ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            round,
            2,
            Index: player,
            Subject: card.ObjectId,
            Seat: player
        ));
    }

    /// <summary>Whether Teamwork's forced response is currently eligible.</summary>
    public static bool TeamworkApplies(World world, ICardFacts facts, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);

        if (card.Area.Type != DeckType.EngagedEnemiesArea
            || Characteristics.IsLost(world, card, "teamwork")
            || !facts.Attributes(card.FaceId).TryGetValue("Teamwork", out string? trait))
        {
            return false;
        }

        // "In play" and not "engaged with you": a minion in another player's
        // area is in play, and `rr:engage` is about which area it sits in
        // rather than whether it counts.
        bool company = world.Areas
            .Where(area => area.Type == DeckType.EngagedEnemiesArea)
            .SelectMany(area => area.Cards)
            .Any(other => other.ObjectId != card.ObjectId
                && State.Traits.Has(world, other, trait, facts));

        return company;
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
    /// called from step 3's tail rather than from <see cref="Reveal.Resolve"/>.
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

        if (!QuickstrikeApplies(world, facts, card, player))
        {
            return;
        }

        // Scheduled rather than resolved, because an attack is six steps and
        // one of them asks a player something. The reveal that caused it is
        // finished by the time this runs.
        world.Agenda.Then(new PhaseStep(
            new EnemyAttackOperation(),
            round,
            2,
            Index: player,
            Subject: card.ObjectId,
            Seat: player
        ));
    }

    /// <summary>Whether Quickstrike's forced response is currently eligible.</summary>
    public static bool QuickstrikeApplies(
        World world, ICardFacts facts, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        return card.Area.Type == DeckType.EngagedEnemiesArea
            && StateFields.Modified(world, card, "quickstrike", facts, world.Players) > 0
            && Forms.In(world, world.Seats[player], facts, Forms.Hero);
    }
}

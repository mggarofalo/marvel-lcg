using static Marvel.Rules.Play.BasicPowers;
using static Marvel.Rules.Play.BasicPowerInitiation;
using static Marvel.Rules.Play.BasicPowerResolution;
using static Marvel.Rules.Play.BasicThwartPowers;
using static Marvel.Rules.Play.AllyBasicPowers;
using static Marvel.Rules.Play.BasicPowerStatus;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Runs recovery and ally basic powers.</summary>
public static class AllyBasicPowers
{

    /// <summary>
    /// A basic recovery — <c>rr:recover-recovery</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Who is recovering.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void BasicRecovery(
        World world, ICardFacts facts, int player, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var seat = world.Seats[player];
        Require(world, facts, seat, Forms.AlterEgo, RecoverVerb, "REC");

        if (seat.IdentityCard.Damage == 0)
        {
            throw new RulesNotImplementedException(
                $"{seat.Name} has no damage to heal, and rr:recover-recovery.1 does not "
                + "permit a basic recovery");
        }

        Exhaust(seat.IdentityCard, RecoverVerb, events);
        DamageRecovery.Heal(
            world, facts, seat.IdentityCard,
            StateFields.Modified(world, seat.IdentityCard, "recover", facts, world.Players),
            RecoverVerb, RecoverVerb, events);
    }

    /// <summary>
    /// The allies a player may use to attack or thwart — <c>rr:ally.2</c>.
    /// </summary>
    /// <remarks>
    /// "During a player's turn, they may use <b>any number</b> of allies they
    /// control to attack or thwart. An ally <b>must exhaust</b> to attack or
    /// thwart." Any number, so this is every ready one and not a choice of one.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="player">Whose allies.</param>
    public static IReadOnlyList<Card> Allies(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        return [.. world
            .AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player)
            .Cards
            .Where(ally => ally.Ready)];
    }

    /// <summary>
    /// An ally attacks or thwarts — <c>rr:ally.2</c> and <c>rr:ally.3</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same two powers a hero has, with two differences the rules state
    /// outright. <c>rr:ally.5</c>: an ally's attack "is <b>not</b> considered to
    /// be performed by that player's identity", so the form gate in
    /// <c>rr:player-turn.3</c> does not apply — an ally can attack while its
    /// controller is in alter-ego form.
    /// </para>
    /// <para>
    /// <c>rr:ally.3</c> is the other: "after an ally is used to attack or
    /// thwart, deal consequential damage to that ally equal to the number of
    /// consequential damage icons beneath the ally's ATK or THW field". A hero
    /// takes none — <c>rr:consequential-damage</c> is an ally rule.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="ally">Which ally.</param>
    /// <param name="target">The enemy or scheme.</param>
    /// <param name="verb">Whether this is an attack or a thwart.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void AllyPower(
        World world, ICardFacts facts, Card ally, Card target, string verb,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(ally);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        bool attacking = string.Equals(verb, AttackVerb, StringComparison.Ordinal);
        string field = attacking ? "ATK" : "THW";
        ValidateAllyPower(facts, ally, verb, field);

        // A status-cancelled attempt needs no valid target
        // (`rr:stun-stunned.5.1`, `rr:confuse-confused.5.1`). Check it before
        // target legality so an ally can clear the status when the legal set
        // is empty, and take no consequential damage (`rr:ally.3`).
        string cancellingStatus = attacking ? Statuses.Stunned : Statuses.Confused;
        if (Statuses.Afflicted(world, facts, ally, cancellingStatus))
        {
            Exhaust(ally, verb, events);
            BasicPowerStatus.Cancelled(world, facts, ally, cancellingStatus, events);
            return;
        }

        if (!LegalTargets(world, facts, ally, attacking)
            .Any(option => option.ObjectId == target.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {target.ObjectId} is not something card {ally.ObjectId} can {verb}");
        }

        Exhaust(ally, verb, events);

        // Scheduled, both halves -- and the consequential damage after the
        // power, because `rr:consequential-damage.1` deals it "after resolving
        // abilities that are triggered by the ally attacking or thwarting".
        // Dealt inline it would land before the enemy that answers "after this
        // is attacked" had spoken.
        //
        // `rr:attack-player-ability-type.step.9` says the same thing from the
        // attack's side, and puts it last of all: after `.step.7`'s forced
        // abilities and `.step.8`'s optional ones.
        if (attacking) InitiateAttack(world, ally, target, ally.Owner);
        else InitiateThwart(world, ally, target, ally.Owner);

        world.Agenda.Then(new PhaseStep(
            attacking ? Steps.AllyConsequentialDamage : Steps.AllyThwartConsequentialDamage,
            world.Agenda.Current?.Round ?? 0,
            9,
            Index: ally.Owner,
            Subject: ally.ObjectId,
            Seat: ally.Owner,
            Character: target.ObjectId));
    }

    private static void ValidateAllyPower(
        ICardFacts facts, Card ally, string verb, string field)
    {
        if (!ally.Ready)
            throw new RulesNotImplementedException(
                $"card {ally.ObjectId} is exhausted and must exhaust to {verb}");
        if (!CanUsePower(facts, ally, field))
            throw new RulesNotImplementedException(
                $"card {ally.ObjectId} has no usable {field} value");
    }

    private static IReadOnlyList<Card> LegalTargets(
        World world, ICardFacts facts, Card ally, bool attacking) => attacking
            ? Attackable(world, facts, ally.Owner) : Thwartable(world, facts, ally.Owner);
}

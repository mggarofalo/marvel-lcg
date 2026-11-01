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

/// <summary>Initiates thwart powers and evaluates their legal targets.</summary>
public static class BasicThwartPowers
{

    /// <summary>
    /// A basic thwart — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// "A hero or ally can use their basic thwart power to thwart a scheme. A
    /// character <b>must exhaust</b> to use this power. This removes threat
    /// equal to the character's THW value from the scheme."
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="player">Who is thwarting.</param>
    /// <param name="scheme">Which scheme.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void BasicThwart(
        World world, ICardFacts facts, int player, Card scheme, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        var seat = world.Seats[player];
        var character = seat.IdentityCard;
        ThwartPermissions.Require(world, character);
        Require(
            world, facts, seat, Forms.Hero, ThwartVerb,
            UsesAttack(world, facts, scheme) ? "ATK" : "THW");

        // `rr:thwart.1.1` and `rr:confuse-confused.5.1`, the same pair.
        if (!Statuses.Afflicted(world, facts, character, Statuses.Confused)
            && !Thwartable(world, facts, player).Any(t => t.ObjectId == scheme.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {scheme.ObjectId} is not a scheme {seat.Name} can thwart");
        }

        Exhaust(character, ThwartVerb, events);

        // `rr:confuse-confused.1` and `.5`, the thwart's half of the same rule.
        if (BasicPowerStatus.Cancelled(world, facts, character, Statuses.Confused, events))
        {
            return;
        }

        InitiateThwart(world, character, scheme, player);
    }

    /// <summary>Initiates a card ability labelled as a thwart.</summary>
    public static bool CardThwart(
        World world, ICardFacts facts, CardPowerInvocation power, List<GameEvent> events,
        ThreatPlacement? imminentThreat = null, bool automaticTarget = false, bool nested = false)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(power);
        ArgumentNullException.ThrowIfNull(events);

        var thwarter = power.Performer ?? LabeledAbilities.Begin(
            world, facts, power.Player, power.Source, [ThwartVerb], events);
        if (thwarter is null) return false;

        RequireCardThwartTarget(world, facts, power, automaticTarget);

        ScheduleThwart(world, new CharacterThwart(
            thwarter.ObjectId, power.Target?.ObjectId ?? -1, power.Player, power.Amount,
            power.Source.ObjectId, power.Trigger, power.Continuation, power.Origin, power.SurgeGained,
            imminentThreat, power.Target?.Incarnation ?? -1) { UsesThwartStat = power.UsesThwartStat }, nested);
        return true;
    }

    private static void RequireCardThwartTarget(
        World world, ICardFacts facts, CardPowerInvocation power, bool automaticTarget)
    {
        // rr:labeled-ability.4 and ruling 0fc2a7cc9197e127: an independent
        // sentence can resolve as a thwart without any thwarted scheme.
        if (power.Target is null && (power.Continuation is null || automaticTarget))
            throw new RulesNotImplementedException("a scheme-less thwart requires an interpreter continuation");
        if (power.Target is not null && !power.Initiated && (automaticTarget
            ? !CanAutomaticallyThwart(world, facts, power.Player, power.Target)
            : !Thwartable(world, facts, power.Player).Any(card => card.ObjectId == power.Target.ObjectId)))
        {
            throw new RulesNotImplementedException(
                $"card {power.Target.ObjectId} is not a scheme {world.Seats[power.Player].Name} can thwart");
        }

    }

    /// <summary>Whether a card's already-determined scheme may be thwarted.</summary>
    /// <remarks>
    /// Crisis prohibits removing threat, so it does not prohibit Emergency
    /// from preventing imminent threat. Patrol instead says the engaged player
    /// cannot thwart the main scheme at all, and therefore still applies.
    /// </remarks>
    public static bool CanAutomaticallyThwart(
        World world, ICardFacts facts, int player, Card scheme)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(scheme);

        bool isScheme = scheme.Area.Type is
            DeckType.MainSchemesArea or DeckType.SideSchemesArea;
        return isScheme
            && (scheme.Area.Type != DeckType.MainSchemesArea
                || !Patrolled(world, facts, player));
    }

    /// <summary>Whether a basic thwart against this scheme uses ATK.</summary>
    /// <remarks>
    /// <c>rr:assault.1</c>, as a constant ability: "while a character is making
    /// a basic thwart against this scheme, that character uses its ATK instead
    /// of its THW."
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="scheme">The scheme being thwarted.</param>
    public static bool UsesAttack(World world, ICardFacts facts, Card scheme)
    {
        ArgumentNullException.ThrowIfNull(world);
        return Assaulted(world, facts, scheme);
    }
}

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
        World world, ICardFacts facts, int player, Card source, Card scheme, long amount,
        string trigger, List<GameEvent> events, int abilityIndex = -1,
        int powerOrdinal = 0, int resumeFrom = -1,
        bool finalStep = false, IReadOnlyList<int>? targets = null,
        ThreatPlacement? imminentThreat = null, bool automaticTarget = false,
        bool nested = false, bool surgeGained = false,
        IReadOnlyList<string>? abilityPath = null, string abilityFace = "",
        IReadOnlyDictionary<string, long>? abilityResults = null,
        Occurrence? abilityOccurrence = null, IReadOnlyList<int>? discarded = null,
        bool eachPlayerFrame = false, bool finalPlayer = false, int abilityPlayer = -1,
        bool abilityHasContinuation = false, Card? performer = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        var thwarter = performer ?? LabeledAbilities.Begin(
            world, facts, player, source, [ThwartVerb], events);
        if (thwarter is null)
        {
            return false;
        }

        if (automaticTarget
            ? !CanAutomaticallyThwart(world, facts, player, scheme)
            : !Thwartable(world, facts, player).Any(card => card.ObjectId == scheme.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"card {scheme.ObjectId} is not a scheme {world.Seats[player].Name} can thwart");
        }

        InitiateThwart(
            world, thwarter, scheme, player, amount, source, trigger, abilityIndex,
            powerOrdinal, resumeFrom, finalStep, targets, imminentThreat, nested,
            surgeGained, abilityPath, abilityFace, abilityResults, abilityOccurrence,
            discarded, eachPlayerFrame, finalPlayer, abilityPlayer,
            abilityHasContinuation, thwarter.ObjectId);

        return true;
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

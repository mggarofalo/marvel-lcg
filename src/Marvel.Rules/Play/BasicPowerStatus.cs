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

/// <summary>Applies status-card cancellation and shared basic-power checks.</summary>
public static class BasicPowerStatus
{

    /// <summary>Pay for a status-cancelled basic power with no legal target.</summary>
    internal static void CancelledBasicPower(
        World world, ICardFacts facts, Card character, string verb,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(events);

        bool attacking = string.Equals(verb, AttackVerb, StringComparison.Ordinal);
        string field = attacking ? "ATK" : "THW";
        string status = attacking ? Statuses.Stunned : Statuses.Confused;
        if (!character.Ready || !CanUsePower(facts, character, field)
            || !Statuses.Afflicted(world, facts, character, status))
        {
            throw new RulesNotImplementedException(
                $"card {character.ObjectId} cannot make a targetless {verb} attempt");
        }

        Exhaust(character, verb, events);
        if (!Cancelled(world, facts, character, status, events))
        {
            throw new InvalidOperationException(
                $"card {character.ObjectId}'s targetless {verb} was not cancelled");
        }
    }

    /// <summary>
    /// Whether a minion with <c>rr:patrol</c> is engaged with this player.
    /// </summary>
    internal static bool Patrolled(World world, ICardFacts facts, int player) =>
        world.Areas.FirstOrDefault(area => area.Type == DeckType.EngagedEnemiesArea
                && area.PlayArea == PlayArea.Of(player) && area.Host == -1)?.Cards
            .Any(minion => StateFields.Modified(
                world, minion, "patrol", facts, world.Players) > 0) == true;

    /// <summary>Whether a scheme carries <c>rr:assault</c>.</summary>
    internal static bool Assaulted(World world, ICardFacts facts, Card scheme) =>
        StateFields.Modified(world, scheme, "assault", facts, world.Players) > 0;

    /// <summary>
    /// A status card cancels the action it names — <c>rr:stun-stunned.1</c>,
    /// <c>rr:confuse-confused.1</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "<b>Forced Interrupt</b>: when this character would attack, remove
    /// <b>each</b> stunned status card from it instead." Each, not one — which
    /// is the opposite of <c>rr:tough.2.1</c>, and <c>rr:steady</c> is what
    /// makes the difference visible: a steady character carrying two loses both.
    /// </para>
    /// <para>
    /// <c>rr:stun-stunned.5</c>: "costs associated with the attack attempt,
    /// <b>including exhausting the character</b>, must still be paid." So the
    /// caller exhausts first and asks this second.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="character">Who is acting.</param>
    /// <param name="status">The status that would cancel it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>Whether the action was cancelled.</returns>
    public static bool Cancelled(
        World world, ICardFacts facts, Card character, string status, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(events);

        if (!Statuses.Afflicted(world, facts, character, status))
        {
            return false;
        }

        foreach (var card in Statuses.On(world, character, status).ToList())
        {
            Discard.Card(world, card, status, events);
        }

        return true;
    }

    /// <summary>The seat a minion is engaged with, or -1.</summary>
    internal static int Engaged(World world, Card enemy) =>
        enemy.Area.Type == DeckType.EngagedEnemiesArea ? enemy.Area.PlayArea.Player : -1;

    /// <summary>Every villain and minion in play.</summary>
    internal static List<Card> Enemies(World world, ICardFacts facts)
    {
        var enemies = new List<Card>();
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            // `rr:enemy`: "an enemy is a minion or villain."
            enemies.AddRange(area.Cards.Where(card =>
                CardKinds.IsEnemy(FacedownDrones.Kind(card, facts))));
        }

        return enemies;
    }

    /// <summary>`rr:player-turn.3` -- which powers a form permits.</summary>
    internal static void Require(
        World world, ICardFacts facts, Seat seat, string form, string verb, string field)
    {
        if (!Forms.In(world, seat, facts, form))
        {
            throw new RulesNotImplementedException(
                $"{seat.Name} is not in {form} form, and rr:player-turn.3 permits a basic "
                + $"{verb} only in that form");
        }

        if (!seat.IdentityCard.Ready)
        {
            // `rr:exhausted.2`: "if an exhausted card must exhaust to pay the
            // cost of using its ability, that ability cannot be used until the
            // card is ready."
            throw new RulesNotImplementedException(
                $"{seat.Name} is exhausted and a basic {verb} must exhaust to use");
        }

        if (!CanUsePower(facts, seat.IdentityCard, field))
        {
            throw new RulesNotImplementedException(
                $"{seat.Name}'s {field} value is a dash and cannot be used");
        }
    }

    /// <summary>
    /// A character's THW comes off a scheme — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// Capped by the threat that is there: <c>rr:threat</c> counts tokens, and
    /// a scheme cannot hold a negative number of them. <c>Card.PlaceTokens</c>
    /// clamps too, so without this the board would be right and the event would
    /// report a scheme going below zero.
    /// </remarks>
    internal static void RemoveThreat(
        World world, ICardFacts facts, Card character, Card scheme, List<GameEvent> events)
    {
        string power = Assaulted(world, facts, scheme) ? "attack" : "thwart";
        // Who did it, for the cards that ask. `rr:ownership-and-control.2`
        // puts a card under its owner's control, so an ally's thwart is still
        // its owner's doing -- `rr:you-your.15` keeps it off that player's
        // identity, which is a different question.
        Threat.Remove(
            world, facts, world.ThreatAbilities, scheme,
            StateFields.Modified(world, character, power, facts, world.Players),
            ThwartVerb, ThwartVerb, events, by: character.Owner);
    }

    internal static void Exhaust(Card character, string verb, List<GameEvent> events)
    {
        character.Exhaust();
        events.Add(new FieldSet(character.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = verb,
            Verb = verb,
        });
    }
}

using static Marvel.Rules.Play.Damage;
using static Marvel.Rules.Play.DamagePlacement;
using static Marvel.Rules.Play.DamageAttacks;
using static Marvel.Rules.Play.DamageRecovery;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Applies attack damage, overkill, consequential damage, and retaliation.</summary>
public static class DamageAttacks
{

    /// <summary>
    /// One attack, with the keywords that change how it lands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three keywords sit between an attack and its damage and all three are
    /// about the <i>attack</i> rather than either character, which is why they
    /// are one call rather than three checks scattered across two attack paths.
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>rr:piercing.1</c> — "before this attack deals damage to a
    ///     character, discard each tough status card from that character", and
    ///     <c>.2</c>: an attack dealing no damage discards none.
    ///   </description></item>
    ///   <item><description>
    ///     <c>rr:overkill.1</c> — the damage beyond a defeated ally's hit points
    ///     goes to its controller's identity, and beyond a defeated minion's to
    ///     the villain. <c>.2</c>: it is "damage from an attack, but does not
    ///     constitute an attack against that character", so it retaliates
    ///     against nothing.
    ///   </description></item>
    ///   <item><description>
    ///     <c>rr:ranged.1</c> — "this attack ignores the retaliate keyword".
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="attacker">Who is attacking.</param>
    /// <param name="target">Who is being attacked.</param>
    /// <param name="amount">How much damage.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="verb">What kind of thing caused it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="retaliate">Whether to resolve retaliation before returning.</param>
    /// <returns>
    /// Every character this attack <b>actually damaged</b>, which is not every
    /// character it was aimed at. <c>rr:tough.3</c>: a character whose tough
    /// status card ate the damage "is not considered to have taken damage", and
    /// cards are written against that — "if a character is damaged by this
    /// attack, that character is stunned".
    /// </returns>
    public static AttackDamageResult Attack(
        World world, ICardFacts facts, Card attacker, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, bool retaliate = true)
        => Attack(
            world, facts, attacker, attacker, target, amount, trigger, verb, events, retaliate);

    /// <summary>
    /// One attack whose acting character and damage source are different cards.
    /// </summary>
    /// <remarks>
    /// A card ability labelled as an attack is performed by the resolving
    /// character, but its damage still comes from the card carrying the
    /// ability. Keeping those roles separate lets retaliation hit the actor
    /// while a prohibition such as “cannot take damage from [trait] upgrades”
    /// inspect the actual source. The ordinary character-attack overload uses
    /// the attacker for both roles.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="attacker">The character performing the attack.</param>
    /// <param name="source">The card the damage comes from.</param>
    /// <param name="target">Who is being attacked.</param>
    /// <param name="amount">How much damage.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="verb">What kind of thing caused it.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="retaliate">Whether to resolve retaliation before returning.</param>
    public static AttackDamageResult Attack(
        World world, ICardFacts facts, Card attacker, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, bool retaliate = true) =>
        Resolve(world, facts, new AttackDamageRequest(attacker, source, target,
            amount, trigger, verb, retaliate), events);

    /// <summary>Resolve attack damage with explicit causal activation identity.</summary>
    public static AttackDamageResult Resolve(World world, ICardFacts facts,
        AttackDamageRequest request, List<GameEvent> events) => Begin(world, facts, request, events, false);

    internal static AttackDamageResult Begin(World world, ICardFacts facts,
        AttackDamageRequest request, List<GameEvent> events, bool spillOnly)
    {
        AttackDamageResolution? resolution = null;
        if ((request.Origin ?? world.Agenda.Occurrence) is { } occurrence)
            Apply(occurrence, null);
        else
            RootDamageExecution.Run(world, request.Attacker, request.Target, Apply);

        AbilityExecutionResult Apply(Occurrence cause, AbilityExecution? completion) =>
            world.Agenda.Executions.RunBatch(cause, entry =>
            {
                resolution = new(world, facts, request with { Origin = cause }, entry, spillOnly, completion);
                resolution.Continue(events);
                return entry.Report(events);
            });
        return resolution!.Result;
    }
    internal static void DiscardPiercedTough(
        World world, Card target, Card source, long amount, string trigger, List<GameEvent> events)
    {
        // rr:piercing.2: "If an attack with the piercing keyword would deal no
        // damage to the attacked character, it does not discard tough status
        // cards". A character that cannot take damage is dealt none (ruling
        // 76a9465df95e739c keeps Kang's Tough under that prohibition). An enemy
        // attack whose calculated damage is zero returns before reaching here
        // (AttackDamage), so the amount test guards only other callers. A
        // prevention effect does not withhold the discard: ruling bafde43c1a76c982
        // (RR 1.7-1.8) says "Effects that 'prevent damage' prevent damage taken,
        // not dealt", so an attack whose damage Temporal Shield will prevent still
        // deals damage here and discards Tough first.
        if (amount <= 0 || !world.DamageAbilities.CanTakeDamage(world, target, source))
        {
            return;
        }
        foreach (var tough in Statuses.On(world, target, Statuses.Tough).ToList())
        {
            Discard.Card(world, tough, trigger, events);
        }
    }

    internal static void ResolveLandedDamage(
        World world, int? activationId, Card target, Damage.PlacedDamage placed, List<GameEvent> events)
    {
        if (placed.Landed)
        {
            DelayedEffects.Occur(world, Steps.DamageDealt, target.ObjectId, events, activationId);
        }
    }

    /// <summary>
    /// A character that was attacked hits back — <c>rr:retaliate-x</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "<b>Forced Response</b>: after this character is attacked, deal X damage
    /// to the attacker." So it happens after the attack has resolved, not as
    /// part of the damage.
    /// </para>
    /// <para>
    /// <c>rr:retaliate-x.2</c>: "the character with retaliate X <b>must be in
    /// play after the attack resolves</b> to deal this damage" — an attack that
    /// defeated it kills the retaliation with it.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="attacked">Who was attacked.</param>
    /// <param name="attacker">Who attacked them.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>The damage execution, or null when the attacked character is absent.</returns>
    public static DamageResult? Retaliate(
        World world, ICardFacts facts, Card attacked, Card attacker,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(attacked);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:villain-defeat.3.1`: same-title villain stages are the same
        // character for card abilities. The attacked physical stage may have
        // left play during damage, while the character that was attacked is
        // now represented by the next stage.
        if (!DeckTypes.IsInPlay(attacked.Area.Type)
            && CardKinds.IsVillain(EffectiveCards.Kind(attacked, facts))
            && CurrentStage(world, attacked) is { } next
            && string.Equals(
                facts.Title(attacked.FaceId), facts.Title(next.FaceId),
                StringComparison.Ordinal))
        {
            attacked = next;
        }

        if (!DeckTypes.IsInPlay(attacked.Area.Type))
        {
            return null;
        }

        long retaliate = StateFields.Modified(
            world, attacked, "retaliate", facts, world.Players);

        return DamagePlacement.Resolve(world, facts,
            new(attacked, attacker, retaliate, trigger, "Retaliate"), events);
    }
    private static Card? CurrentStage(World world, Card prior) =>
        world.EncounterTopology is { } topology
            ? topology.Groups.Single(group => group.Stages.Contains(prior)).CurrentVillain
            : CardReferences.TheCardIn(world, DeckType.VillainArea, prior);

}

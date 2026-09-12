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

/// <summary>Resolves scheduled character powers and their consequential damage.</summary>
public static class BasicPowerResolution
{

    /// <summary>
    /// Takes the thwart's threat off — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// "This removes threat equal to the character's THW value from the
    /// scheme." The mirror of <see cref="ResolveCharacterAttack"/>, and the
    /// place a scheme thwarted to zero is defeated from.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="scheduled">The thwart carried by the current agenda step.</param>
    public static void ResolveCharacterThwart(
        World world, ICardFacts facts, List<GameEvent> events,
        CharacterThwart? scheduled = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var thwart = scheduled ?? world.CharacterThwart;
        if (thwart is null)
        {
            throw new RulesNotImplementedException(
                "a character thwart is resolving and the board holds none");
        }

        if (thwart.AbilityIndex < 0
            && (!DeckTypes.IsInPlay(world.Cards[thwart.Scheme].Area.Type)
                || world.Cards[thwart.Scheme].Incarnation != thwart.TargetIncarnation))
        {
            world.Agenda.CancelConsequentialDamage(
                thwart.Thwarter, thwart.Scheme, attack: false);
            return;
        }

        if (thwart.AbilityIndex >= 0)
        {
            var occurrence = world.Agenda.Occurrence
                ?? throw new RulesNotImplementedException(
                    "a character thwart resolved without an occurrence for its response window");
            world.PowerAbilities.ResolveCardThwart(world, thwart, occurrence, events);
            return;
        }

        if (thwart.Amount >= 0)
        {
            Threat.Remove(
                world,
                facts,
                world.ThreatAbilities,
                world.Cards[thwart.Scheme],
                thwart.Amount,
                thwart.Trigger,
                ThwartVerb,
                events,
                thwart.Player);
            return;
        }

        RemoveThreat(
            world, facts, world.Cards[thwart.Thwarter], world.Cards[thwart.Scheme], events);
    }

    /// <summary>
    /// Deals the attack's damage — <c>rr:attack-player-ability-type.1</c>.
    /// </summary>
    /// <remarks>
    /// "This deals damage equal to the character's ATK value to the enemy."
    /// Through <see cref="Damage"/>'s attack primitive, which is the same one an
    /// enemy's attack uses: <c>rr:damage</c> is one rule however the damage
    /// arrived.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="scheduled">The attack carried by the current agenda step.</param>
    public static void ResolveCharacterAttack(
        World world, ICardFacts facts, List<GameEvent> events,
        CharacterAttack? scheduled = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var attack = scheduled ?? world.CharacterAttack;
        if (attack is null)
        {
            throw new RulesNotImplementedException(
                "a character attack is resolving and the board holds none");
        }

        var occurrence = world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "a character attack resolved without an occurrence for its response window");
        if (attack.AbilityIndex < 0 && !TargetRemains(world, attack))
        {
            // `rr:consequential-damage.2` and `.2.1`: if an ally's basic-power
            // target leaves before application, the exhausted ally neither
            // attacked nor takes consequential damage. No AttackEnds condition
            // is added because the attack itself aborted.
            world.Agenda.CancelConsequentialDamage(
                attack.Attacker, attack.Enemy, attack: true);
            return;
        }
        if (attack.AbilityIndex >= 0)
        {
            world.PowerAbilities.ResolveCardAttack(world, attack, occurrence, events);
            occurrence.Also(Steps.AttackEnds);
            return;
        }

        ResolveBasicCharacterAttack(world, facts, events, attack, occurrence);
    }

    private static void ResolveBasicCharacterAttack(
        World world, ICardFacts facts, List<GameEvent> events,
        CharacterAttack attack, Occurrence occurrence)
    {
        Card attacker = world.Cards[attack.Attacker];
        var source = attack.Source >= 0 ? world.Cards[attack.Source] : attacker;
        long amount = attack.Amount >= 0
            ? attack.Amount
            : StateFields.Modified(world, attacker, "attack", facts, world.Players);

        ContinuousEffect? temporaryOverkill = RegisterOverkill(world, attack, source, attacker);
        amount = MoveDamage(world, facts, attack, source, amount, events);

        var damaged = DamageAttacks.Attack(
            world, facts, attacker, source, world.Cards[attack.Enemy], amount,
            attack.Trigger, AttackVerb, events);

        if (temporaryOverkill is not null)
        {
            world.Effects.Use(temporaryOverkill);
        }

        occurrence.Also(Steps.AttackEnds);
        if (damaged.Characters.Count > 0)
        {
            occurrence.Also(Steps.DamageDealt);
        }
    }

    private static bool TargetRemains(World world, CharacterAttack attack) =>
        DeckTypes.IsInPlay(world.Cards[attack.Enemy].Area.Type)
        && world.Cards[attack.Enemy].Incarnation == attack.TargetIncarnation;

    private static ContinuousEffect? RegisterOverkill(
        World world, CharacterAttack attack, Card source, Card attacker)
    {
        if (!attack.Overkill) return null;
        var effect = new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Keywords.Overkill, Amount: 1, Card: source.ObjectId,
            Affects: attacker.ObjectId, Lasts: new Duration(Uses: 1));
        world.Effects.Register(effect);
        return effect;
    }

    private static long MoveDamage(
        World world, ICardFacts facts, CharacterAttack attack, Card source,
        long amount, List<GameEvent> events)
    {
        if (attack.MoveFrom < 0) return amount;
        Card from = world.Cards[attack.MoveFrom];
        amount = Math.Min(amount, from.Damage);
        if (amount <= 0
            || !world.DamageAbilities.CanTakeDamage(world, world.Cards[attack.Enemy], source))
            return 0;
        DamageRecovery.Heal(world, facts, from, amount, attack.Trigger, "Move_Damage", events);
        return amount;
    }

    /// <summary>
    /// An ally's consequential damage —
    /// <c>rr:attack-player-ability-type.step.9</c>.
    /// </summary>
    /// <remarks>
    /// The icons sit under the field that was used, as stars inside
    /// <c>ATK</c>/<c>THW</c> rather than an attribute of their own — so the
    /// printed half is read and the modified half is looked up, the same split
    /// as <c>Damage.Health</c>.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="ally">The ally that attacked or thwarted.</param>
    /// <param name="byAttack">Whether the field used was <c>ATK</c>.</param>
    /// <param name="verb">What the ally did.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Consequential(
        World world, ICardFacts facts, Card ally, bool byAttack, string verb,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(ally);
        ArgumentNullException.ThrowIfNull(events);

        long consequential =
            facts.ConsequentialDamage(ally.FaceId, byAttack ? "ATK" : "THW")
            + StateFields.Modified(
                world, ally,
                byAttack ? "attack_consequential_damage" : "thwart_consequential_damage",
                facts, world.Players);

        DamagePlacement.Deal(
            world, facts, ally, ally, consequential, verb, "Consequential_Damage", events);
    }
}

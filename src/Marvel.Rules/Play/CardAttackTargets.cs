using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Validates the jointly selected recipients of a card-labelled attack.</summary>
internal static class CardAttackTargets
{
    internal static void Validate(World world, ICardFacts facts, CardPowerInvocation power, Card attacker)
    {
        if (power.Targets.IsDefault) return;

        // The engine requires an interpreter continuation for an aggregate:
        // fixed-amount powers have only a single-recipient damage procedure.
        if (power.Continuation is null || power.Targets.IsEmpty
            || !ReferenceEquals(power.Targets[0], power.Target))
        {
            throw new RulesNotImplementedException(
                "A multi-target card attack requires a continuation and its primary target first");
        }

        // rr:choose-game-element.3.1: "The same target cannot be chosen multiple
        // times this way." rr:guard.1: "The engaged player cannot attack any
        // villain." All recipients are checked against the same pre-attack board.
        var candidates = BasicPowers.Attackable(world, facts, power.Player, attacker);
        var chosen = new HashSet<int>();
        foreach (var target in power.Targets)
        {
            if (!chosen.Add(target.ObjectId)
                || !candidates.Any(candidate => ReferenceEquals(candidate, target))
                || !world.DamageAbilities.CanTakeDamage(world, target, power.Source))
            {
                throw new RulesNotImplementedException(
                    $"card {target.ObjectId} is not a distinct legal target of this attack");
            }
        }
    }
}

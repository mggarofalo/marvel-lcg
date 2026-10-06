using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Commits one accepted basic defense and its target transfer.</summary>
internal static class AttackDefenseCommitment
{
    internal static void Apply(World world, ICardFacts facts, Card defender, List<GameEvent> events)
    {
        var attack = AttackCompletion.Current(world);
        // rr:defend-defense.2 and .3 -- both require exhausting the defender.
        defender.Exhaust();
        events.Add(new FieldSet(defender.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = Steps.AttackInitiated,
            Verb = Attack.DefenseVerb,
        });

        // **The defender becomes the target, whoever they are.**
        // `rr:defend-defense.3.1` for an ally: "that ally becomes the target
        // character for that attack, and its controller becomes the target
        // player". `rr:defend-defense.2` for a hero: "any remaining damage is
        // dealt to that hero". And `.5`: "if a player defends against an enemy
        // attack that targets a different player [...] the defending player
        // becomes the new target of that attack."
        //
        // `BasicDefense` is the hero's alone: `rr:defend-defense.2`'s reduction
        // belongs to the basic defense power, and `.3` gives an ally none.
        world.Attack = attack with
        {
            Defender = defender.ObjectId,
            Target = defender.ObjectId,
            Player = defender.Area.PlayArea.Player,
            BasicDefense = EffectiveCards.Kind(defender, facts) != CardKind.Ally,
        };
        if (world.Activation is { Attacking: true } activation)
        {
            world.Activation = activation with { Player = defender.Area.PlayArea.Player };
        }
    }
}

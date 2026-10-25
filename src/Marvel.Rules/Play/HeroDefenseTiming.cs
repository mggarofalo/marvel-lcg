using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Schedules the interrupt opportunity created by a committed hero defense.</summary>
public static class HeroDefenseTiming
{
    /// <summary>Engine vocabulary for the printed "when your hero defends" condition.</summary>
    public const string Defends = "WhenHeroDefends";

    /// <summary>Captures an observed hero defense without reusing the defender-selection window.</summary>
    public static bool Schedule(World world, Card hero)
    {
        // rr:defend-defense.4.2: abilities triggering "when your hero defends against an attack"
        // "can be triggered when resolving a defense-labeled ability."
        if (world.Attack is not { } attack || attack.Defender != hero.ObjectId
            || EffectiveCards.Kind(hero, world.Facts) != CardKind.Hero
            || !world.WindowAbilities.ObservesHeroDefense(world)) return false;
        var enemy = world.Cards[attack.Enemy];
        var operation = new HeroDefenseOperation(hero.ObjectId, hero.Incarnation,
            enemy.ObjectId, enemy.Incarnation, world.Activation?.Id ?? -1,
            OccurrenceCard.Capture(hero, world.Facts), OccurrenceCard.Capture(enemy, world.Facts));
        // Unobserved occurrences are omitted to retain existing-content replay identity.
        // A fresh engine-assigned id separates repeated defenses and nested attacks.
        world.Agenda.Then(new PhaseStep(operation, world.Agenda.Current?.Round ?? 0, 0,
            Subject: hero.ObjectId, Seat: hero.Area.PlayArea.Player)
        { OccurrenceId = world.Agenda.nextPlayerActionOccurrence--, ActivationId = operation.Activation });
        return true;
    }
}

using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Separates the faceup response window from exact boost-ability execution.</summary>
internal static class BoostAbilityResolution
{
    internal static void ResolveFaceup(World world, PhaseStep step)
    {
        // Ruling a66ca21bca34a71f permits the printed Response before Boost resolves.
        world.Agenda.Then(new PhaseStep(new BoostAbilityOperation(step.Require<BoostFaceupOperation>()),
            step.Round, step.Number, Subject: step.Subject, Seat: step.Seat) { ActivationId = step.ActivationId });
    }

    internal static void ResolveAbility(World world, ICardFacts facts, IAttackCardAbilities abilities,
        PhaseStep step, List<GameEvent> events)
    {
        var operation = step.Require<BoostAbilityOperation>().Faceup;
        var activation = AttackCompletion.Activating(world);
        var boost = world.Cards[step.Subject];
        // rr:attack-enemy-activation.step.3.b and rr:boost-boost-icon.2 --
        // resolve the ability before its icons are applied or it is discarded.
        var occurrence = world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "a boost ability has no activation occurrence");
        PhaseStep owner = world.Agenda.Current
            ?? throw new InvalidOperationException("a boost ability has no agenda owner");
        int beforeAbility = world.Agenda.Count;
        if (operation.Ability is { } ability) ability.Started = true;
        if (operation.Ability?.Cancelled != true)
            ActivityBatches.Append(events, abilities.ExecuteBoost(world, boost, activation.Player).Events);
        if (world.Agenda.Count > beforeAbility)
        {
            // An ordinary authored choice is still scheduled after the flip;
            // move that child ahead first. A nested rules procedure has
            // already done this for itself and made its child current.
            if (world.Agenda.Current is { Operation: BoostAbilityOperation })
            {
                world.Agenda.BeforeResponses(owner, occurrence);
            }
            world.Agenda.BeforeOwnerAfterContinuations(
                occurrence,
                owner,
                new PhaseStep(
                    new BoostCompletionOperation(Enemy: activation.Enemy, Attacking: activation.Attacking) { Icons = operation.Icons },
                    world.Agenda.Current?.Round ?? 0,
                    3,
                    Subject: boost.ObjectId,
                    Seat: activation.Player
                ) { ActivationId = activation.Id });
            return;
        }

        AttackBoost.FinishBoostCard(
            world, facts, abilities,
            new PhaseStep(
                new BoostCompletionOperation(Enemy: activation.Enemy, Attacking: activation.Attacking) { Icons = operation.Icons },
                0,
                3,
                Subject: boost.ObjectId,
                Seat: activation.Player
            ) { ActivationId = activation.Id },
            events);
    }

}

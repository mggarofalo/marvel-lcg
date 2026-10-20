using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves assigned simultaneous damage, defeats and retaliation.</summary>
internal static class IndirectAttackResolution
{
    /// <summary>Resolve step 1 for one assigned indirect-damage recipient.</summary>
    public static long? PrepareIndirectDamage(
        World world, PhaseStep step, List<GameEvent> events)
    {
        if (step.Require<IndirectDamageOperation>().Origin is not { } procedure)
        {
            throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        }
        if (step.Require<IndirectDamageOperation>().Amounts is { } amounts
            && amounts.ContainsKey(step.Subject))
        {
            return amounts[step.Subject];
        }

        var attacker = world.Cards[step.Require<IndirectDamageOperation>().Source];
        var target = world.Cards[step.Subject];
        var operation = step.Require<IndirectDamageOperation>();
        if (!IsAssignedIncarnation(operation, target))
        {
            world.Agenda.RecordProcedureAmount(procedure, step.Subject, 0);
            return 0;
        }
        operation.Replacement ??= DamagePlacement.Replace(world,
            new DamageRequest(attacker, target, operation.Amount, Steps.AttackInitiated, "Attack")
                { RecordDefeatOn = procedure }, events);
        if (!operation.Replacement.IsComplete) return null;
        long amount = target.Incarnation == operation.Replacement.TargetIncarnation
            && DeckTypes.IsInPlay(target.Area.Type) ? operation.Replacement.ResolvedAmount : 0;
        world.Agenda.RecordProcedureAmount(procedure, step.Subject, amount);
        return amount;
    }

    private static bool IsAssignedIncarnation(IndirectDamageOperation operation, Card target) =>
        DeckTypes.IsInPlay(target.Area.Type)
        && (operation.Incarnations is not { } incarnations
            || target.Incarnation == incarnations[target.ObjectId]);

    /// <summary>Place an assigned indirect attack after every recipient window.</summary>
    public static void ApplyIndirectDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var occurrence = step.Require<IndirectDamageOperation>().Origin ?? world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        var assigned = step.Require<IndirectDamageOperation>().Amounts
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no prepared recipient results");

        var attacker = world.Cards[step.Subject];
        var current = assigned.Where(pair => IsAssignedIncarnation(
                step.Require<IndirectDamageOperation>(), world.Cards[pair.Key]))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var placed = PrepareIndirectPlacements(world, facts, events, attacker, current);
        foreach (var damage in placed)
        {
            DamagePlacement.ApplyPlaced(
                world, facts, damage, Steps.AttackInitiated, "Attack", events);
        }

        // rr:indirect-damage.3 -- every assigned share is dealt
        // simultaneously. All step-5 placement therefore finishes before a
        // delayed damage effect or defeat can change the board seen by another
        // recipient's already-assigned damage.
        foreach (var damage in placed.Where(damage => damage.Landed))
        {
            DelayedEffects.Occur(
                world, "WhenDamageDealt", damage.Target.ObjectId, events, step.ActivationId < 0 ? null : step.ActivationId);
        }

        AttackDamageAccounting.RecordIndirectDamage(world, occurrence, placed);

        if (FinishIndirectDefeats(
                world, facts, attacker,
                [.. placed.Where(damage => damage.Landed)
                    // Identity elimination clears that player's whole play
                    // area. Finish every ally's simultaneous damage sequence
                    // first so cleanup cannot erase its defeat and callbacks.
                    .OrderBy(damage => world.Seats.Any(seat =>
                        seat.IdentityCard.ObjectId == damage.Target.ObjectId))
                    .Select(damage => damage.Target.ObjectId)],
                step.Require<IndirectDamageOperation>().Character, occurrence, events))
        {
            return;
        }

    }

    private static List<Damage.PlacedDamage> PrepareIndirectPlacements(
        World world, ICardFacts facts, List<GameEvent> events, Card attacker,
        IReadOnlyDictionary<int, long> assigned) =>
        [
            .. assigned.OrderBy(pair => pair.Key).Select(pair =>
                DamagePlacement.PrepareAfterReplacement(
                    world, facts, new DamageRequest(attacker, world.Cards[pair.Key], pair.Value,
                        Steps.AttackInitiated, "Attack")
                    { PreventAllDamage = world.Attack?.PreventAllDamage == true }, events)),
        ];

    /// <summary>Complete indirect damage defeats before the attack reaches its response window.</summary>
    public static void FinishIndirectDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var attacker = world.Cards[step.Subject];
        var occurrence = step.Require<IndirectDamageOperation>().Origin ?? world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage continuation has no occurrence");
        if (FinishIndirectDefeats(
                world, facts, attacker, step.Require<IndirectDamageOperation>().Candidates ?? [],
                step.Require<IndirectDamageOperation>().Character, occurrence, events))
        {
            return;
        }

    }

    internal static bool FinishIndirectDefeats(
        World world, ICardFacts facts, Card attacker,
        IReadOnlyList<int> recipients, int attacked, Occurrence occurrence,
        List<GameEvent> events)
    {
        for (int index = 0; index < recipients.Count; index++)
        {
            var target = world.Cards[recipients[index]];
            if (!DeckTypes.IsInPlay(target.Area.Type))
            {
                continue;
            }

            var outcome = DamagePlacement.FinishPlaced(
                world, facts, attacker,
                new Damage.PlacedDamage(target, Dealt: 0, Taken: 1),
                Steps.AttackInitiated, "Attack", events,
                recordDefeatOn: occurrence);
            if (outcome != Damage.Outcome.Suspended)
            {
                continue;
            }

            world.Agenda.ThenContinuation(
                new PhaseStep(
                    new IndirectDamageOperation(
                        IndirectDamageKind.FinishIndirectAttackDamage,
                        Character: attacked,
                        Candidates: [.. recipients.Skip(index + 1)],
                        Origin: occurrence
                    ),
                    world.Agenda.Current?.Round ?? 0,
                    5,
                    Subject: attacker.ObjectId,
                    Plan: true
                ),
                occurrence);
            return true;
        }

        return false;
    }

}

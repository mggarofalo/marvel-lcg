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
    public static long PrepareIndirectDamage(
        World world, PhaseStep step, List<GameEvent> events)
    {
        if (step.ProcedureOccurrence is not { } procedure)
        {
            throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        }
        if (step.ProcedureAmounts?.ContainsKey(step.Subject) == true)
        {
            return step.ProcedureAmounts[step.Subject];
        }

        var attacker = world.Cards[step.ProcedureSource];
        var target = world.Cards[step.Subject];
        long amount = DamagePlacement.Replace(
            world, target, attacker, step.ProcedureAmount, events);
        world.Agenda.RecordProcedureAmount(procedure, step.Subject, amount);
        return amount;
    }

    /// <summary>Place an assigned indirect attack after every recipient window.</summary>
    public static void ApplyIndirectDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var occurrence = step.ProcedureOccurrence ?? world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        var assigned = step.ProcedureAmounts
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no prepared recipient results");

        var attacker = world.Cards[step.Subject];
        var placed = PrepareIndirectPlacements(world, facts, events, attacker, assigned);
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
                world, "WhenDamageDealt", damage.Target.ObjectId, events);
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
                step.Character, occurrence, events))
        {
            return;
        }

        // Only the declared defender (or the undefended target) was attacked;
        // other recipients merely took damage from that attack.
        if (!Keywords.Has(world, attacker, Keywords.Ranged, facts))
        {
            DamageAttacks.Retaliate(world, facts, world.Cards[step.Character], attacker,
                Steps.AttackInitiated, events);
        }
    }

    private static List<Damage.PlacedDamage> PrepareIndirectPlacements(
        World world, ICardFacts facts, List<GameEvent> events, Card attacker,
        IReadOnlyDictionary<int, long> assigned) =>
        [
            .. assigned.OrderBy(pair => pair.Key).Select(pair =>
                DamagePlacement.PrepareAfterReplacement(
                    world, facts, attacker, world.Cards[pair.Key], pair.Value,
                    Steps.AttackInitiated, events)),
        ];

    /// <summary>Resolve retaliation after an indirect-damage defeat decision.</summary>
    public static void FinishIndirectDamage(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var attacker = world.Cards[step.Subject];
        var occurrence = step.ProcedureOccurrence ?? world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage continuation has no occurrence");
        if (FinishIndirectDefeats(
                world, facts, attacker, step.ProcedureCandidates ?? [],
                step.Character, occurrence, events))
        {
            return;
        }

        if (!Keywords.Has(world, attacker, Keywords.Ranged, facts))
        {
            DamageAttacks.Retaliate(world, facts, world.Cards[step.Character], attacker,
                Steps.AttackInitiated, events);
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
                    Steps.FinishIndirectAttackDamage,
                    world.Agenda.Current?.Round ?? 0,
                    5,
                    Subject: attacker.ObjectId,
                    Character: attacked,
                    ProcedureCandidates: [.. recipients.Skip(index + 1)],
                    ProcedureOccurrence: occurrence,
                    Plan: true),
                occurrence);
            return true;
        }

        return false;
    }

}

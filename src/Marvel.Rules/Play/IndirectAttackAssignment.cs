using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns eligible recipients and the player's indirect attack assignment.</summary>
internal static class IndirectAttackAssignment
{
    internal static void BeginIndirectDamage(
        World world, ICardFacts facts, EnemyAttack attack, long amount,
        List<GameEvent> events)
    {
        var candidates = IndirectCandidates(world, facts, attack.Player);
        long assign = Math.Min(
            amount,
            candidates.Sum(card => DamagePlacement.Health(world, facts, card) - card.Damage));
        if (assign <= 0)
        {
            return;
        }

        var assignment = new PhaseStep(
            Steps.AssignIndirectAttackDamage,
            world.Agenda.Current?.Round ?? 0,
            5,
            Subject: attack.Enemy,
            Seat: attack.Player,
            Character: attack.Target,
            ProcedureAmount: assign,
            ProcedureOccurrence: world.Agenda.Occurrence,
            ProcedureCandidates: [.. candidates.Select(card => card.ObjectId)]);
        if (candidates.Count == 1)
        {
            AssignIndirectDamage(
                world, facts, assignment,
                Decision.Take(
                    attack.Enemy,
                    Enumerable.Repeat(candidates[0].ObjectId, (int)assign).ToList(),
                    []),
                events);
            return;
        }

        var occurrence = world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        world.Agenda.ThenContinuation(assignment, occurrence);
        world.Agenda.BeforeResponses(occurrence);
    }

    /// <summary>Ask the attacked player to assign one indirect attack's damage.</summary>
    public static Prompt IndirectDamagePrompt(
        World world, ICardFacts facts, PhaseStep step)
    {
        var candidates = IndirectCandidates(world, facts, step.Seat)
            .Where(card => (step.ProcedureCandidates ?? []).Contains(card.ObjectId))
            .ToList();
        int amount = checked((int)Math.Min(
            step.ProcedureAmount,
            candidates.Sum(card => DamagePlacement.Health(world, facts, card) - card.Damage)));
        var maximumOccurrences = candidates.ToDictionary(
            card => card.ObjectId,
            card => checked((int)(DamagePlacement.Health(world, facts, card) - card.Damage)));
        return new Prompt(
            step.Seat,
            Question.Element,
            TimingPriority.Untimed,
            Steps.DealAttackDamage,
            $"{world.Seats[step.Seat].Name} assigns {amount} indirect attack damage",
            false,
            [new Affordance(
                step.Subject, "Choose", step.Subject, World.Scenario,
                "indirectDamage",
                new TargetRequest(
                    [.. candidates.Select(card => card.ObjectId)], amount, amount,
                    Rule: "rr:indirect-damage.1",
                    AllowRepeated: true,
                    MaximumOccurrences: maximumOccurrences))]);
    }

    /// <summary>Resolve an assignment without treating every recipient as attacked.</summary>
    public static void AssignIndirectDamage(
        World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        ValidateIndirectAnswerShape(input, step.Subject);

        var occurrence = step.ProcedureOccurrence ?? world.Agenda.Occurrence
            ?? throw new RulesNotImplementedException(
                "indirect attack damage has no containing occurrence");
        var eligible = IndirectCandidates(world, facts, step.Seat)
            .Where(card => (step.ProcedureCandidates ?? []).Contains(card.ObjectId))
            .ToDictionary(card => card.ObjectId);
        int expected = checked((int)Math.Min(
            step.ProcedureAmount,
            eligible.Values.Sum(card => DamagePlacement.Health(world, facts, card) - card.Damage)));
        if (input.Targets.Count != expected)
        {
            throw new RulesNotImplementedException(
                $"indirect attack damage requires {expected} assignments");
        }

        var assigned = AllocateIndirectDamage(world, facts, eligible, input.Targets);

        int round = world.Agenda.Current?.Round ?? 0;
        var windows = assigned
            .OrderBy(pair => pair.Key)
            .Select((pair, index) => new PhaseStep(
                Steps.PrepareIndirectAttackDamage,
                round,
                5,
                Index: index,
                Subject: pair.Key,
                Seat: step.Seat,
                ProcedureSource: step.Subject,
                ProcedureAmount: pair.Value,
                ProcedureAmounts: new Dictionary<int, long>(),
                ProcedureOccurrence: occurrence))
            .ToList();
        windows.Add(new PhaseStep(
            Steps.ApplyIndirectAttackDamage,
            round,
            5,
            Subject: step.Subject,
            Seat: step.Seat,
            Character: step.Character,
            Plan: true,
            ProcedureCandidates: [.. input.Targets],
            ProcedureOccurrence: occurrence,
            ProcedureAmounts: new Dictionary<int, long>()));
        world.Agenda.Now(windows);
    }

    private static void ValidateIndirectAnswerShape(Decision input, int subject)
    {
        if (input.IsDecline || input.Affordance != subject
            || input.Spent.Count > 0 || input.DefinedValues.Count > 0
            || input.Allocated.Count > 0)
        {
            throw new RulesNotImplementedException(
                "the indirect attack damage answer was not the offered assignment");
        }
    }

    private static Dictionary<int, long> AllocateIndirectDamage(
        World world, ICardFacts facts, Dictionary<int, Card> eligible,
        IReadOnlyList<int> targets)
    {
        var assigned = new Dictionary<int, long>();
        foreach (int id in targets)
        {
            if (!eligible.TryGetValue(id, out var card))
            {
                throw new RulesNotImplementedException(
                    $"card {id} cannot receive this indirect attack damage");
            }
            long share = assigned.GetValueOrDefault(id) + 1;
            long room = DamagePlacement.Health(world, facts, card) - card.Damage;
            if (share > room)
            {
                throw new RulesNotImplementedException(
                    $"card {id} has room for {room} indirect attack damage");
            }
            assigned[id] = share;
        }
        return assigned;
    }

    internal static List<Card> IndirectCandidates(World world, ICardFacts facts, int player) =>
    [
        .. world.Cards
            .Where(card => card.Area.PlayArea == PlayArea.Of(player))
            .Where(card => card.ObjectId == world.Seats[player].IdentityCard.ObjectId
                || FacedownDrones.Kind(card, facts) == CardKind.Ally)
            .Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && DamagePlacement.Health(world, facts, card) - card.Damage > 0
                && world.DamageAbilities.CanTakeDamage(world, card, world.Cards[AttackCompletion.Current(world).Enemy]))
            .OrderBy(card => card.ObjectId),
    ];
}

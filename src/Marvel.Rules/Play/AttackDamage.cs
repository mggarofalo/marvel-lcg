using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal static class AttackDamage
{
    public static void CalculateDamage(World world, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        if (AttackCompletion.Over(world))
        {
            return;
        }

        AttackCompletion.RefreshDefender(world, facts);
        var attack = AttackCompletion.Current(world);
        world.Attack = attack with { CalculatedDamage = AttackCompletion.Amount(world, facts, attack) };
    }

    /// <summary>Make the current enemy attack deal indirect damage.</summary>
    public static void MakeIndirect(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.Attack = AttackCompletion.Current(world) with { Indirect = true };
    }

    /// <summary>
    /// Step 5. Deal the damage calculated in step 4 —
    /// <c>rr:attack-enemy-activation.step.5</c>.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void DealDamage(World world, ICardFacts facts, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:activation.6` -- "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve."
        if (AttackCompletion.Over(world))
        {
            return;
        }

        AttackCompletion.RefreshDefender(world, facts);
        var attack = AttackCompletion.Current(world);
        long amount = attack.CalculatedDamage
            ?? throw new RulesNotImplementedException(
                "attack damage reached step 5 before step 4 calculated it");

        // The departure rule applies only before attack damage is dealt. The
        // step is resolved even when its calculated amount is zero, so mark it
        // before that early return. The marker also prevents a nested damage
        // consequence from rewriting the attack after assignment has begun.
        MarkDamageStepResolved(world, attack);
        if (amount <= 0)
        {
            return;
        }

        if (attack.Indirect)
        {
            BeginIndirectDamage(world, facts, attack, amount, events);
            return;
        }

        // Through the same primitive a hero's basic attack uses. `rr:damage` is
        // one rule however the damage arrived, and so is `rr:defeat` -- an
        // enemy attack that defeated a character down a separate path would be
        // a second place for the defeat rules to be wrong.
        // One call, because `rr:piercing`, `rr:overkill` and `rr:ranged` are all
        // properties of the attack rather than of either character.
        var damage = DamageAttacks.Attack(
            world, facts, world.Cards[attack.Enemy], world.Cards[attack.Target], amount,
            Steps.AttackInitiated, "Deal_Damage", events);

        // Recorded on the attack rather than derived later, because by the time
        // `rr:attack-enemy-activation.step.6.a`'s abilities run the damage is on
        // a dial that had damage on it before. `damaged` is the list
        // `rr:tough.3` shortens -- a character whose tough card absorbed the
        // attack "is not considered to have taken damage" -- so an attack that
        // hit a tough card did not damage anybody.
        RecordAttackDamage(world, attack, damage);
    }

    private static void MarkDamageStepResolved(World world, EnemyAttack attack) =>
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: AttackDamageResolved,
            Affects: attack.Target,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));

    private static void BeginIndirectDamage(
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

    private static void RecordAttackDamage(
        World world, EnemyAttack attack, Damage.AttackResult damage)
    {
        if (damage.Characters.Count > 0)
        {
            MarkAttackDamaged(world, attack);
            world.Agenda.Occurrence?.Also(Steps.DamageDealt);
        }
        AddActivationDamage(world, damage.Dealt);
    }

    private static void MarkAttackDamaged(World world, EnemyAttack attack)
    {
        if (world.Attack is not null)
        {
            world.Attack = attack with { Damaged = true };
        }
        else if (world.FinishedAttack is { } finishedAttack)
        {
            world.FinishedAttack = finishedAttack with { Damaged = true };
        }
    }

    private static void AddActivationDamage(World world, long dealt)
    {
        if (world.Activation is { } activation)
        {
            world.Activation = activation with { DamageDealt = activation.DamageDealt + dealt };
        }
        else if (world.FinishedActivation is { } finishedActivation)
        {
            world.FinishedActivation = finishedActivation with
            {
                DamageDealt = finishedActivation.DamageDealt + dealt,
            };
        }
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

        RecordIndirectDamage(world, occurrence, placed);

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

    private static void RecordIndirectDamage(
        World world, Occurrence occurrence, IReadOnlyList<Damage.PlacedDamage> placed)
    {
        if (placed.Any(damage => damage.Landed))
        {
            if (world.Attack is { } currentAttack)
            {
                world.Attack = currentAttack with { Damaged = true };
            }
            else if (world.FinishedAttack is { } finishedAttack)
            {
                world.FinishedAttack = finishedAttack with { Damaged = true };
            }
            occurrence.Also(Steps.DamageDealt);
        }
        AddActivationDamage(world, placed.Sum(damage => damage.Dealt));
    }

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

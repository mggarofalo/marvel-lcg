using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Divides one hero basic attack or thwart among several legal targets.</summary>
/// <remarks>
/// Printed 13001c: "Threat you remove using your basic thwart power (THW) can be
/// divided among schemes as you choose" and "Damage you deal using your basic
/// attack power (ATK) can be divided among enemies as you choose."
/// pack:mc13:wasp-hero-pack#rules-clarifications: "Each scheme is considered to
/// have been thwarted" and "Each enemy is considered to have been attacked."
/// Ruling 78f96d5c7974afaa: the hero first declares one target, and the amount
/// that use finally applies, including its interrupts, is what is divided.
/// Ruling 116e678d0ad83200: an enemy is attacked only when it is dealt at least 1,
/// and Guard prevents assigning any damage to the villain. The allocation is
/// therefore a list of legal recipients in which each entry is one point; an enemy
/// or scheme receiving no point is not attacked or thwarted. Rulings
/// aefac89a00641dd4 and f216f6359fb0bf96 make the division simultaneous.
/// The modifier names and the allocation prompt are engine choices.
/// </remarks>
public static class BasicPowerDivision
{
    /// <summary>Engine-chosen modifier permitting a hero to divide basic attack damage.</summary>
    public const string AttackField = "basic_attack_divided";
    /// <summary>Engine-chosen modifier permitting a hero to divide basic thwart threat removal.</summary>
    public const string ThwartField = "basic_thwart_divided";
    /// <summary>Engine-chosen verb of the allocation affordance.</summary>
    public const string Verb = "Divide";

    /// <summary>The allocation prompt for this power step, or null when nothing can be divided.</summary>
    internal static Prompt? Describe(World world, ICardFacts facts, PhaseStep step) =>
        Plan(world, facts, step) is { } plan ? Prompt(step, plan) : null;

    /// <summary>Validates the allocation and resolves the divided power.</summary>
    internal static void Answer(World world, ICardFacts facts, PhaseStep step, Decision input,
        List<GameEvent> events)
    {
        var plan = Plan(world, facts, step)
            ?? throw new RulesNotImplementedException("the basic power no longer has anything to divide");
        var offered = Prompt(step, plan).Affordances[0];
        if (input.IsDecline || input.Affordance != offered.Id || !offered.Targets!.Allows(input.Targets))
            throw new ArgumentException("Allocate exactly the offered amount among legal targets", nameof(input));
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a divided basic power has no occurrence");
        // Stable object-id order is the engine's event-stream spelling for simultaneous shares.
        var shares = input.Targets.GroupBy(id => id).OrderBy(group => group.Key)
            .Select(group => (Card: world.Cards[group.Key], Amount: (long)group.Count())).ToArray();
        if (step.Operation is CharacterAttackOperation { Attack: var attack })
            BasicAttackDivisionResolution.Begin(world, facts, attack, plan.Performer, shares, occurrence, events);
        else
            RemoveThreat(world, plan, shares, occurrence, events);
    }

    private static void RemoveThreat(World world, BasicDivisionPlan plan,
        IReadOnlyList<(Card Card, long Amount)> shares, Occurrence occurrence, List<GameEvent> events)
    {
        int by = LabeledAbilities.IdentityPlayer(world, plan.Performer);
        ThreatBatchRemoval.Remove(world, world.ThreatAbilities, [.. shares.Select(share =>
            new ThreatRemovalRequest(share.Card, share.Amount, BasicPowers.ThwartVerb, BasicPowers.ThwartVerb, by)
            { ResponsiblePlayer = plan.Player, ByThwarting = true })], events);
        occurrence.Also(CharacterPowerTiming.ThwartEnds);
        occurrence.Also(BasicPowerTiming.ThwartEnds);
    }

    private static Prompt Prompt(PhaseStep step, BasicDivisionPlan plan)
    {
        string what = plan.Attack ? "damage" : "threat removal";
        string among = plan.Attack ? "enemies" : "schemes";
        string description = plan.Attack
            ? $"Assign each of {plan.Count} damage to an enemy you can attack. Each enemy dealt damage is attacked; damage is dealt simultaneously."
            : $"Assign each of {plan.Count} threat removal to a scheme you can thwart. Each scheme given threat removal is thwarted; threat is removed simultaneously.";
        return new Prompt(plan.Player, Question.Element, TimingPriority.Untimed, step.What,
            $"Divide {plan.Count} {what} among {among}", false,
            [new Affordance(plan.Performer.ObjectId, Verb, plan.Performer.ObjectId, plan.Player,
                $"Divide {what}", new TargetRequest([.. plan.Candidates.Select(card => card.ObjectId)],
                    plan.Count, plan.Count, AllowRepeated: true, MaximumOccurrences: plan.Capacities),
                Description: description)
            { DisplayLabel = $"Divide {what}", CommitLabel = plan.Attack ? "Deal damage" : "Remove threat" }])
        {
            DisplayQuestion = $"Divide {plan.Count} {what} among {among}",
            Description = description,
            CauseCardIds = [plan.Performer.ObjectId],
            ContextCardIds = plan.Declared >= 0 ? [plan.Declared] : [],
        };
    }

    private static BasicDivisionPlan? Plan(World world, ICardFacts facts, PhaseStep step) => step.Operation switch
    {
        CharacterAttackOperation { Attack: var attack } => AttackPlan(world, facts, attack),
        CharacterThwartOperation { Thwart: var thwart } => ThwartPlan(world, facts, thwart),
        _ => null,
    };

    private static BasicDivisionPlan? AttackPlan(World world, ICardFacts facts, CharacterAttack attack)
    {
        bool basic = attack.Amount < 0 && attack.Source < 0 && attack.MoveFrom < 0 && attack.Continuation is null;
        if (!basic || !Current(world, attack.Enemy, attack.TargetIncarnation)) return null;
        var performer = world.Cards[attack.Attacker];
        if (!Divides(world, facts, performer, AttackField)) return null;
        long amount = StateFields.Modified(world, performer, "attack", facts, world.Players) + Bonus(world);
        var candidates = BasicPowers.Attackable(world, facts, attack.Player, performer);
        return Divisible(amount, candidates)
            ? new(performer, attack.Player, true, attack.Enemy, candidates, checked((int)amount), null) : null;
    }

    private static BasicDivisionPlan? ThwartPlan(World world, ICardFacts facts, CharacterThwart thwart)
    {
        bool basic = thwart.Amount < 0 && thwart.Source < 0 && thwart.Continuation is null;
        if (!basic || !Current(world, thwart.Scheme, thwart.TargetIncarnation)) return null;
        var performer = world.Cards[thwart.Thwarter];
        if (!Divides(world, facts, performer, ThwartField)) return null;
        // rr:assault.1 fixes the statistic from the declared scheme for the whole use.
        string power = BasicPowerStatus.Assaulted(world, facts, world.Cards[thwart.Scheme]) ? "attack" : "thwart";
        long amount = ThwartStatSubstitution.Modified(world, performer, power) + Bonus(world);
        var candidates = BasicPowers.Thwartable(world, facts, thwart.Player);
        var capacities = candidates.ToDictionary(card => card.ObjectId,
            card => checked((int)Math.Min(amount, card.Tokens.GetValueOrDefault("k_threat"))));
        long count = Math.Min(amount, capacities.Values.Sum(value => (long)value));
        return Divisible(count, candidates)
            ? new(performer, thwart.Player, false, thwart.Scheme, candidates, checked((int)count), capacities) : null;
    }

    private static long Bonus(World world) => world.Agenda.Occurrence?.BasicUse?.Bonus ?? 0;

    private static bool Divisible(long amount, IReadOnlyList<Card> candidates) => amount >= 1 && candidates.Count >= 2;

    private static bool Divides(World world, ICardFacts facts, Card performer, string field) =>
        DeckTypes.IsInPlay(performer.Area.Type)
        && EffectiveCards.Kind(performer, facts) == CardKind.Hero
        && (world.Agenda.Occurrence?.BasicUse is not { } use || use.IsCurrent(world))
        && StateFields.Modified(world, performer, field, facts, world.Players) > 0;

    private static bool Current(World world, int card, int incarnation) => card >= 0
        && DeckTypes.IsInPlay(world.Cards[card].Area.Type) && world.Cards[card].Incarnation == incarnation;

    private sealed record BasicDivisionPlan(Card Performer, int Player, bool Attack, int Declared,
        IReadOnlyList<Card> Candidates, int Count, IReadOnlyDictionary<int, int>? Capacities);
}

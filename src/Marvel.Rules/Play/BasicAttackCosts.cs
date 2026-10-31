using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns additional hand-discard and resource costs before a basic attack attempt.</summary>
public static class BasicAttackCosts
{
    /// <summary>The engine-selected modifier name for additional discarded hand cards.</summary>
    public const string DiscardField = "attack_hand_discard_cost";
    /// <summary>The engine-selected modifier name for additional resources of any type.</summary>
    public const string ResourceField = "attack_resource_cost";

    /// <summary>The current additional discard cost, without changing the board.</summary>
    public static int Count(World world, Card character) => Modified(world, character, DiscardField);

    /// <summary>The current additional resource cost, without changing the board.</summary>
    public static int Resources(World world, Card character) => Modified(world, character, ResourceField);

    /// <summary>Whether the controller can pay every additional attack cost.</summary>
    public static bool CanPay(World world, Card character)
    {
        int count = Count(world, character);
        int resources = Resources(world, character);
        if (count == 0 && resources == 0) return true;
        int player = CardControl.ControllerOf(world, character);
        if (player < 0) return false;
        // Engine choice: one attack is never asked to pay both kinds; no admitted
        // card combines them, so the shared-card question is not answered.
        if (count > 0 && resources > 0)
            throw new RulesNotImplementedException("a basic attack with both discard and resource additional costs");
        return count > 0 ? world.Seats[player].Hand.Cards.Count >= count
            : Play.Resources.Pays(string.Concat(Generators(world, character, player).SelectMany(source => source.Generates)), resources);
    }

    private static int Modified(World world, Card character, string field) => checked((int)Math.Max(0,
        StateFields.Modified(world, character, field, world.Facts, world.Players)));

    private static IReadOnlyList<ResourceSource> Generators(World world, Card character, int player) =>
        CardPayment.Generators(world, world.Facts, world.Seats[player], world.ResourceAbilities, abilitySource: character);

    internal static bool Schedule(World world, Card character, Card? enemy, bool exhaust)
    {
        int count = Count(world, character);
        int resources = Resources(world, character);
        if (count == 0 && resources == 0) return false;
        if (!CanPay(world, character))
            throw new RulesNotImplementedException("the character cannot pay its additional attack cost");
        int player = CardControl.ControllerOf(world, character);
        RequireTarget(world, character, enemy, player);
        world.Agenda.Then(new PhaseStep(
            new BasicAttackCostOperation(enemy?.ObjectId ?? -1, character.Incarnation, count, exhaust) { Resources = resources },
            world.Agenda.Current?.Round ?? 0, 0, Subject: character.ObjectId, Seat: player)
        { OccurrenceId = world.Agenda.nextPlayerActionOccurrence-- });
        return true;
    }

    private static void RequireTarget(World world, Card character, Card? enemy, int player)
    {
        if (!Statuses.Afflicted(world, world.Facts, character, Statuses.Stunned)
            && (enemy is null || !BasicPowers.Attackable(world, world.Facts, player, character).Contains(enemy)))
            throw new RulesNotImplementedException("the additional-cost attack has no legal target");
    }

    internal static Prompt Describe(World world, PhaseStep step)
    {
        var cost = step.Require<BasicAttackCostOperation>();
        var character = CurrentCharacter(world, step, cost);
        return new Prompt(step.Seat, Question.Element, TimingPriority.Untimed, step.What,
            "Pay the additional attack cost", false, [Offer(world, step, cost, character)])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = "Pay the additional attack cost",
            Description = (cost.Resources > 0 ? $"Spend {cost.Resources} resource(s) of any type."
                    : $"Choose {cost.Count} card(s) from your hand to discard.")
                + (cost.Exhaust ? " The attacker exhausts when you pay." : " This attack does not require exhaustion."),
            CauseCardIds = [character.ObjectId],
            ContextCardIds = cost.Enemy >= 0 ? [cost.Enemy] : [],
        };
    }

    private static Affordance Offer(World world, PhaseStep step, BasicAttackCostOperation cost, Card character) =>
        cost.Resources > 0
            ? new Affordance(character.ObjectId, "PayAttackCost", character.ObjectId, step.Seat, "Spend resources",
                Costs: [new CostOption(character.ObjectId,
                    cost.Resources.ToString(System.Globalization.CultureInfo.InvariantCulture), [],
                    Sources: Generators(world, character, step.Seat))])
            { DisplayLabel = $"Spend {cost.Resources} resource(s)", CommitLabel = "Pay and attack" }
            : new Affordance(character.ObjectId, "PayAttackCost", character.ObjectId, step.Seat,
                "Discard from hand", new TargetRequest(
                    [.. world.Seats[step.Seat].Hand.Cards.Select(card => card.ObjectId)], cost.Count, cost.Count,
                    Rule: "rr:cost.10"))
            { DisplayLabel = $"Discard {cost.Count} from hand", CommitLabel = "Pay and attack" };

    internal static void Answer(World world, PhaseStep step, Decision input, List<GameEvent> events)
    {
        var cost = step.Require<BasicAttackCostOperation>();
        var character = CurrentCharacter(world, step, cost);
        var offered = Describe(world, step).Affordances[0];
        if (input.IsDecline || input.Affordance != offered.Id
            || cost.Resources == 0 && !offered.Targets!.Allows(input.Targets))
            throw new ArgumentException("Choose exactly the offered hand cards for the attack cost", nameof(input));
        // rr:cost.10: additional costs are paid "simultaneously with the cost that is being added to".
        var spent = cost.Resources > 0 ? Spend(world, step, cost, character, input, events) : [];
        foreach (int id in cost.Resources > 0 ? [] : input.Targets)
            Discard.Card(world, world.Cards[id], BasicPowers.AttackVerb, events);
        if (cost.Exhaust) BasicPowerStatus.Exhaust(character, BasicPowers.AttackVerb, events);
        // Responses to the spent cards follow the completed cost and precede the attack.
        ResourceSpentWindow.Open(world, spent, step.Seat);
        if (cost.Enemy < 0)
        {
            BasicPowerStatus.Cancelled(world, world.Facts, character, Statuses.Stunned, events);
            return;
        }
        BasicPowerInitiation.CommitPaidAttack(world, world.Facts, character, world.Cards[cost.Enemy], events);
    }

    private static IReadOnlyList<SpentResourceCard> Spend(World world, PhaseStep step,
        BasicAttackCostOperation cost, Card character, Decision input, List<GameEvent> events)
    {
        // Validate the proposed generators before any contribution is committed.
        var price = new CostOption(character.ObjectId,
            cost.Resources.ToString(System.Globalization.CultureInfo.InvariantCulture), [],
            Sources: Generators(world, character, step.Seat));
        var paying = input.Resources ?? [];
        var allocations = input.Allocations is { Count: > 0 } given ? given : ResourcePayment.Allocate(price, paying)
            ?? throw new ArgumentException("The chosen resources do not pay the attack cost", nameof(input));
        var receipt = AbilityResourcePayment.Spend(world, world.ResourceAbilities,
            new(character, step.Seat, paying, cost.Resources, string.Empty) { Price = price, Allocations = allocations }, events);
        if (!receipt.IsComplete)
            throw new RulesNotImplementedException("an attack cost payment whose resource generation suspends");
        return receipt.SpentCards;
    }

    private static Card CurrentCharacter(World world, PhaseStep step, BasicAttackCostOperation cost)
    {
        var character = world.Cards[step.Subject];
        if (character.Incarnation != cost.Incarnation || !DeckTypes.IsInPlay(character.Area.Type)
            || CardControl.ControllerOf(world, character) != step.Seat || cost.Exhaust && !character.Ready)
            throw new RulesNotImplementedException("the character awaiting attack payment is no longer available");
        return character;
    }
}

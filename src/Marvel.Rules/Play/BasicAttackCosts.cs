using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns additional hand-discard costs before a basic attack attempt.</summary>
public static class BasicAttackCosts
{
    /// <summary>The engine-selected modifier name for additional discarded hand cards.</summary>
    public const string DiscardField = "attack_hand_discard_cost";

    /// <summary>The current additional discard cost, without changing the board.</summary>
    public static int Count(World world, Card character) => checked((int)Math.Max(0,
        StateFields.Modified(world, character, DiscardField, world.Facts, world.Players)));

    /// <summary>Whether the controller can pay all additional hand discards.</summary>
    public static bool CanPay(World world, Card character)
    {
        int count = Count(world, character);
        if (count == 0) return true;
        int player = CardControl.ControllerOf(world, character);
        return player >= 0 && world.Seats[player].Hand.Cards.Count >= count;
    }

    internal static bool Schedule(World world, Card character, Card? enemy, bool exhaust)
    {
        int count = Count(world, character);
        if (count == 0) return false;
        if (!CanPay(world, character))
            throw new RulesNotImplementedException("the character cannot pay its additional attack discard cost");
        int player = CardControl.ControllerOf(world, character);
        if (!Statuses.Afflicted(world, world.Facts, character, Statuses.Stunned)
            && (enemy is null || !BasicPowers.Attackable(world, world.Facts, player, character).Contains(enemy)))
            throw new RulesNotImplementedException("the additional-cost attack has no legal target");
        world.Agenda.Then(new PhaseStep(
            new BasicAttackCostOperation(enemy?.ObjectId ?? -1, character.Incarnation, count, exhaust),
            world.Agenda.Current?.Round ?? 0, 0, Subject: character.ObjectId, Seat: player)
        { OccurrenceId = world.Agenda.nextPlayerActionOccurrence-- });
        return true;
    }

    internal static Prompt Describe(World world, PhaseStep step)
    {
        var cost = step.Require<BasicAttackCostOperation>();
        var character = CurrentCharacter(world, step, cost);
        return new Prompt(step.Seat, Question.Element, TimingPriority.Untimed, step.What,
            "Pay the additional attack cost", false,
            [new Affordance(character.ObjectId, "PayAttackCost", character.ObjectId, step.Seat,
                "Discard from hand", new TargetRequest(
                    [.. world.Seats[step.Seat].Hand.Cards.Select(card => card.ObjectId)], cost.Count, cost.Count,
                    Rule: "rr:cost.10"))
            { DisplayLabel = $"Discard {cost.Count} from hand", CommitLabel = "Pay and attack" }])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = "Pay the additional attack cost",
            Description = $"Choose {cost.Count} card(s) from your hand to discard."
                + (cost.Exhaust ? " The attacker exhausts when you pay." : " This attack does not require exhaustion."),
            CauseCardIds = [character.ObjectId],
            ContextCardIds = cost.Enemy >= 0 ? [cost.Enemy] : [],
        };
    }

    internal static void Answer(World world, PhaseStep step, Decision input, List<GameEvent> events)
    {
        var cost = step.Require<BasicAttackCostOperation>();
        var character = CurrentCharacter(world, step, cost);
        var offered = Describe(world, step).Affordances[0];
        if (input.IsDecline || input.Affordance != offered.Id || !offered.Targets!.Allows(input.Targets))
            throw new ArgumentException("Choose exactly the offered hand cards for the attack cost", nameof(input));
        // rr:cost.10: additional costs are paid "simultaneously with the cost that is being added to".
        foreach (int id in input.Targets) Discard.Card(world, world.Cards[id], BasicPowers.AttackVerb, events);
        if (cost.Exhaust) BasicPowerStatus.Exhaust(character, BasicPowers.AttackVerb, events);
        if (cost.Enemy < 0)
        {
            BasicPowerStatus.Cancelled(world, world.Facts, character, Statuses.Stunned, events);
            return;
        }
        BasicPowerInitiation.CommitPaidAttack(world, world.Facts, character, world.Cards[cost.Enemy], events);
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

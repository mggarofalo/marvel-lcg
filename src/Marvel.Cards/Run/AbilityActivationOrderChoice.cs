using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityStructuralPowerExecution;

namespace Marvel.Cards.Run;

/// <summary>Offers and validates the order of one ability’s enemy activations.</summary>
internal static class AbilityActivationOrderChoice
{
    internal static Prompt DescribeActivationOrder(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation)
    {
        var enemies = ActivationCandidates(context, activation);
        var ids = enemies.Select(card => card.ObjectId).ToList();
        // Resolve the same attacked seats that scheduling will use. An explicit
        // "against" binding or an engaged hero can differ from the ability player.
        if (Activation(context, activation, enemies) is not ScheduleActivationsCommand schedule)
            throw new RulesNotImplementedException(
                $"'{context.SourceFace}' cannot identify an attacked player for activation order");
        var attackedSeats = schedule.Targets.Select(target => target.Seat).Distinct().ToList();
        // The engine chooses the first player for one table-wide order spanning
        // attacked players; a single attacked player owns their own order.
        int attackedPlayer = schedule.Targets.Length == enemies.Count && attackedSeats.Count == 1
            ? attackedSeats[0] : context.Expressions.World.FirstPlayer;
        return new Prompt(attackedPlayer, Question.Order, TimingPriority.Untimed,
            Steps.CardRevealed, $"{context.SourceFace}: order enemy activations", false,
            [new Affordance(context.Expressions.Source.ObjectId, "Order", context.Expressions.Source.ObjectId,
                attackedPlayer, "enemy activations",
                new TargetRequest(ids, ids.Count, ids.Count, Rule: "rr:activation.5"))]);
    }

    internal static AbilityStructuralTransition AnswerActivationOrder(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation, Decision answer)
    {
        var legal = ActivationCandidates(context, activation).Select(card => card.ObjectId).ToHashSet();
        if (answer.IsDecline || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Targets.Count != legal.Count || answer.Targets.Distinct().Count() != legal.Count
            || answer.Targets.Any(id => !legal.Contains(id)))
        {
            return new Unsupported(
                $"'{context.SourceFace}' requires one permutation of all {legal.Count} enemy activations");
        }
        return Activation(context, activation,
            [.. answer.Targets.Select(id => context.Expressions.World.Cards[id])]);
    }

}

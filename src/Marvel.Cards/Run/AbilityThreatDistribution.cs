using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Allocates one printed total and removes each scheme's share once.</summary>
internal static class AbilityThreatDistribution
{
    internal static bool HasTargets(AbilityEffect.ThwartGroup group, AbilityAdmission.AbilityAdmissionScope cast) =>
        Eligible(group, cast.Context.Query).Count > 0 && Total(group, cast.World, cast.Source) > 0;

    private static IReadOnlyList<Card> Eligible(AbilityEffect.ThwartGroup group, AbilityQueryContext query)
    {
        var selected = new AbilitySelectorEvaluation(query).Every(group.Schemes)
            .Select(card => card.ObjectId).ToHashSet();
        return [.. BasicPowers.Thwartable(query.World, query.World.Facts, query.Player)
            .Where(card => selected.Contains(card.ObjectId))];
    }

    private static long Total(AbilityEffect.ThwartGroup group, World world, Card source) =>
        Math.Max(0, AbilityAmounts.SaturatingSum(group.DistributionTotal,
            [AbilityEventModifiers.Amount(world, source, "eventThreatRemoval")]));

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.ThwartGroup group)
    {
        var cards = Eligible(group, context.Expressions.Bindings);
        int count = checked((int)Math.Min(Total(group, context.Expressions.World, context.Expressions.Source),
            cards.Sum(card => card.Tokens.GetValueOrDefault("k_threat"))));
        var actor = context.AbilityActor ?? context.Expressions.World.Seats[context.Player].IdentityCard;
        bool confused = Statuses.Afflicted(context.Expressions.World, context.Expressions.World.Facts, actor, Statuses.Confused);
        string explanation = confused ? "Confused cancels this thwart; no threat will be removed."
            : $"Allocate {count} threat among the schemes. Each point removes one threat; later effects can change the result.";
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: allocate threat removal", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, "distributeThreat",
                new TargetRequest([.. cards.Select(card => card.ObjectId)], count, count,
                    AllowRepeated: true, MaximumOccurrences: cards.ToDictionary(card => card.ObjectId,
                        card => checked((int)Math.Min(count, card.Tokens.GetValueOrDefault("k_threat"))))),
                Description: explanation) { DisplayLabel = "Allocate threat removal", CommitLabel = "Remove threat" }])
        { DisplayQuestion = $"Allocate {count} threat removal", Description = explanation };
    }

    internal static AbilityStructuralTransition Answer(AbilityStructuralContext context,
        AbilityEffect.ThwartGroup group, Decision answer)
    {
        var assessment = DecisionAssessment.Assess(Describe(context, group), answer);
        if (!assessment.IsAccepted) return new Rejected(assessment.Reason ?? "Invalid threat allocation");
        if (answer.Targets.Count == 0) return new Rejected("No threat can be removed");
        return new ThwartSelectionCommand(answer.Targets[0], [.. answer.Targets], [], -1);
    }

    internal static AbilityStructuralOutcome Remove(AbilityDamageAndThreatContext context,
        AbilityDamageAndThreatState state)
    {
        // Printed total allocation is one thwart, with one removal instance per scheme.
        // Stable object-id order is an engine event-stream choice for simultaneous shares.
        var legal = BasicPowers.Thwartable(context.World, context.World.Facts, context.Player)
            .Select(card => card.ObjectId).ToHashSet();
        long removed = 0;
        var targets = context.Expressions.Bindings.PowerTargets;
        foreach (var group in targets.GroupBy(card => card.ObjectId).OrderBy(group => group.Key))
        {
            if (!legal.Contains(group.Key)) continue;
            long held = group.First().Tokens.GetValueOrDefault("k_threat");
            var owner = context.World.Agenda.Current;
            var occurrence = context.World.Agenda.Occurrence;
            var removal = Threat.RemoveWithOutcome(context.World, context.World.Facts, context.ThreatAbilities,
                new ThreatRemovalRequest(group.First(), group.Count(), context.Trigger, "Remove_Threat",
                    AbilityEffectAttribution.IdentityPlayer(context))
                { RecordDefeatOn = context.World.Agenda.Occurrence }, context.Events);
            AbilityThreatExecution.RetainSuspension(state, removal, owner, occurrence);
            long taken = removal.Removed;
            removed += taken;
            if (taken > 0 && taken == held) state.SchemesCleared++;
        }
        return AbilityCompletion.Amount(removed, targets.Length);
    }
}

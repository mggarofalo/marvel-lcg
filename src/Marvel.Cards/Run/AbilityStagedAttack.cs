using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>One subsequent recipient choice inside a retained attack execution.</summary>
internal static class AbilityStagedAttack
{
    internal static ImmutableArray<int> CaptureIdentity(World world, Card first)
    {
        // ruling:9aa6221cd0950fbb: the next villain stage is not another enemy.
        if (!CardKinds.IsVillain(EffectiveCards.Kind(first, world.Facts))) return [first.ObjectId];
        return [.. VillainStageIdentity.Successors(world, first).Select(stage => stage.ObjectId)];
    }

    private static Card[] Candidates(AbilityResolutionState cast, AbilityProgram program)
    {
        var power = cast.PowerExecution ?? throw new RulesNotImplementedException(
            "attackAnotherEnemy requires a retained attack");
        if (power.Kind != BasicPowers.AttackVerb)
            throw new RulesNotImplementedException("attackAnotherEnemy requires an attack");
        return BasicPowers.Attackable(cast.World, cast.World.Facts, cast.AbilityPlayer)
            .Where(card => (AbilityStagedAttackLowering.IsSplashBody(power.Decoded.Body)
                    || !power.ExcludedEnemyStages.Contains(card.ObjectId)
                    || (!CardKinds.IsVillain(EffectiveCards.Kind(card, cast.World.Facts))
                        && card.Incarnation != power.FirstEnemyIncarnation))
                && AbilityProgramQueries.CanTakeDamage(cast.World, program, card, cast.Source)).ToArray();
    }

    internal static void Start(AbilityResolutionExecution execution, AbilityResolutionState cast,
        AbilityEffect.AttackAnotherEnemy effect)
    {
        // rr:ability.4: resolve the ability one sentence at a time. The preceding
        // damage's owned procedure has settled before execution reaches this node.
        var offered = Candidates(cast, execution.program);
        if (offered.Length == 0) return;
        cast.PowerExecution = cast.PowerExecution! with
        { OfferedEnemies = offered.ToImmutableDictionary(card => card.ObjectId, card => card.Incarnation) };
        execution.SuspendForChoice(effect, cast);
    }

    internal static Prompt Describe(AbilityResolutionState cast, AbilityEffect.AttackAnotherEnemy effect,
        AbilityProgram program) => new(cast.AbilityPlayer, Question.Element,
            TimingPriority.Untimed, Steps.TurnAction, $"{cast.Source.FaceId}: choose {(effect.AnyEnemy ? "an" : "another")} enemy", false,
            OfferedCandidates(cast, program).Select(card => new Affordance(card.ObjectId,
                AbilityStructuralExecution.ChooseVerb, card.ObjectId, card.Owner, card.FaceId)
            {
                DisplayLabel = EffectiveCards.Title(card, cast.World.Facts),
                CommitLabel = "Deal attack damage",
            }).ToArray())
        {
            DisplayQuestion = effect.AnyEnemy ? "Choose an enemy" : "Choose another enemy",
            Description = $"Deal {effect.Amount} attack damage before modifiers to {(effect.AnyEnemy ? "an" : "another")} enemy.",
            ContextCardIds = [cast.Source.ObjectId],
        };

    private static IEnumerable<Card> OfferedCandidates(AbilityResolutionState cast, AbilityProgram program)
    {
        var offered = cast.PowerExecution!.OfferedEnemies;
        // rr:leaves-play.1: a returning minion is a new copy. An answer names
        // only the incarnation captured when this prompt was first offered.
        return Candidates(cast, program).Where(card => offered.TryGetValue(card.ObjectId, out int incarnation)
            && incarnation == card.Incarnation);
    }

    internal static void ValidateAnswer(AbilityResolutionState cast, AbilityProgram program, Decision answer)
    {
        if (!OfferedCandidates(cast, program).Any(card => card.ObjectId == answer.Affordance))
            throw new ArgumentException("The second enemy is no longer eligible", nameof(answer));
    }

    internal static void Answer(AbilityResolutionExecution execution, AbilityResolutionState cast,
        AbilityEffect.AttackAnotherEnemy effect, Decision answer)
    {
        var target = OfferedCandidates(cast, execution.program).SingleOrDefault(card => card.ObjectId == answer.Affordance)
            ?? throw new ArgumentException("The second enemy is no longer eligible", nameof(answer));
        cast.Choose(target);
        var damage = new AbilityEffect.AttackDamage(
            new AbilityCardSelection.Bound(AbilityCardBinding.Chosen), new AbilityNumber.Constant(effect.Amount), false);
        execution.ApplyDamageAndThreat(AbilityDamageAndThreatExecution.Run(damage, effect,
            execution.DamageAndThreatContext(cast)), effect, cast);
    }
}

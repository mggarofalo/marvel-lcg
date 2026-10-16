using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// A preview has no execution cursor and makes no claim about structural ancestors.
// Resumption always requires the distinct, game-owned continuation contract.
internal static class AbilityChoicePreview
{
    internal static Prompt? Describe(
        AbilityResolutionExecution execution, World world, Card source, int player,
        int position, AbilityType? tier, bool finalStep)
    {
        var cast = execution.Resolving(world, source, player, tier, finalStep);
        var candidates = execution.On(source)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .Select(ability => At(ability.Effect, position))
            .SelectMany(effect => AbilityChoiceAnalysis.ActiveChoices(effect, execution.AdmissionContext(cast)))
            .ToArray();
        if (candidates.Length != 1)
            throw new RulesNotImplementedException("a choice preview requires exactly one active choice");
        return execution.DescribeChoice(source, tier, null, candidates[0], cast);
    }

    private static AbilityEffect At(AbilityEffect effect, int position) =>
        effect is AbilityEffect.Sequence sequence && position > 0 && position <= sequence.Effects.Length
            ? sequence.Effects[position - 1] : effect;
}

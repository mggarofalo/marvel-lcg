using Marvel.Rules.Play;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Engine choice: lasting numeric bindings are closed data, never interpreter closures.
internal static class AbilityLastingAmount
{
    internal static ContinuousAmount? Capture(
        AbilityNumber number, AbilityAdmissionContext context)
    {
        return number switch
        {
            AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result
                or AbilityNumber.ResolutionValue or AbilityNumber.PrintedResourcesDiscarded
                or AbilityNumber.DiscardedWithResource => null,
            AbilityNumber.Count { Cards: AbilityCardSelection.Query
                { Kind: AbilityCardQuery.UpgradesYouControl } } =>
                new ContinuousAmount.ControlledUpgrades(AbilityCardQueries.Resolver(context.Query)),
            AbilityNumber.Count { Cards: AbilityCardSelection.WithTrait
                { Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.MinionsEngagedWithYou },
                    Exclude: false } trait } =>
                new ContinuousAmount.EngagedMinionsWithTrait(
                    AbilityCardQueries.Resolver(context.Query), trait.Trait),
            AbilityNumber.Counters { Card: AbilityCardSelection.Query
                { Kind: AbilityCardQuery.MainScheme } } count =>
                new ContinuousAmount.MainSchemeCounters(count.Counter),
            _ => throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' uses an unsupported live lasting amount"),
        };
    }

}

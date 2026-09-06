using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private static Card? Named(AbilityCardBinding name, AbilityResolutionState cast) =>
        AbilityCardQueries.Named(name, cast.QueryContext());

    private static int Resolver(AbilityResolutionState cast) =>
        AbilityCardQueries.Resolver(cast.QueryContext());

    private static Card ChosenPlayer(AbilityResolutionState cast) =>
        AbilityCardQueries.ChosenPlayer(cast.QueryContext());
}

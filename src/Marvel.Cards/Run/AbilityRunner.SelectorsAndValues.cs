using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static IEnumerable<AbilityEffect> ReachableMutationBranches(
        AbilityEffect conditional, Cast cast)
    {
        var test = ConditionalOf(conditional, cast).Test;
        bool canSwitch = PriorStepCanChange(test, cast)
            || cast.PaymentMayMutate && PaymentCanChange(test)
            || cast.PriorBindingMayChange && BindingCanChange(test);
        if (canSwitch)
        {
            return ConditionalBranches((AbilityEffect.Conditional)conditional)
                .Where(value => value is not null)
                .Select(value => value);
        }
        return ConditionalBranch(conditional, Test(test, cast) ? "then" : "else") is { } active
            ? [active]
            : [];
    }

    private static HashSet<DeckType> SearchAreaTypes(
        AbilityEffect search, Cast cast) =>
        EffectOf<AbilityEffect.Search>(search, cast).Areas
            .Select(where => Area(where, cast).Type)
            .ToHashSet();

    private static Card? Named(AbilityCardBinding name, Cast cast) =>
        AbilityCardQueries.Named(name, cast.QueryContext());

    private static int Resolver(Cast cast) =>
        AbilityCardQueries.Resolver(cast.QueryContext());

    private static Card ChosenPlayer(Cast cast) =>
        AbilityCardQueries.ChosenPlayer(cast.QueryContext());

    private static long StartingHealth(Card identity, Cast cast)
    {
        if (FacedownDrones.Kind(identity, cast.World.Facts)
            is not (CardKind.Hero or CardKind.AlterEgo))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for starting hit points of "
                + $"non-identity card {identity.ObjectId}");
        }

        return FacedownDrones.BaseValue(
            identity, cast.World.Facts, "HP", cast.World.Players);
    }


}

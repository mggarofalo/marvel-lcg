using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Removes actual status components in deterministic host/kind/card order.</summary>
internal static class AbilityStatusRemoval
{
    internal static IReadOnlyList<Card> Candidates(AbilityEffect.RemoveStatuses removal, AbilityAdmissionScope scope) =>
        Cards(scope.World, AbilityInitiationPrimitives.Every(removal.Cards, scope), removal.Statuses);

    internal static void Execute(AbilityEffect.RemoveStatuses removal, AbilityCardStateContext context)
    {
        var hosts = AbilityCardStateExecution.Every(removal.Cards, context);
        foreach (var status in Cards(context.World, hosts, removal.Statuses))
            Rules.Play.Discard.Card(context.World, status, context.Trigger, context.Events, "Remove_Status");
    }

    private static IReadOnlyList<Card> Cards(World world, IReadOnlyList<Card> hosts, IReadOnlyList<string> statuses) =>
        [.. hosts.Where(host => DeckTypes.IsInPlay(host.Area.Type)
            && CardKinds.IsCharacter(EffectiveCards.Kind(host, world.Facts)))
            .SelectMany(host => statuses.SelectMany(status => Statuses.On(world, host, status)))];
}


using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Projected membership and attachment hosts for constant threat permissions.</summary>
internal sealed record AbilityThreatPermissionProjection(
    World World, IReadOnlySet<int> Departed, IReadOnlySet<int> Entered,
    IReadOnlyDictionary<int, int> Hosts)
{
    private Card? source;

    internal bool InPlay(Card card) => !Departed.Contains(card.ObjectId)
        && (Entered.Contains(card.ObjectId) || DeckTypes.IsInPlay(card.Area.Type));

    internal AbilityThreatPermissionProjection ForSource(Card card) => this with { source = card };

    internal Card? Find(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.This } => source,
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.AttachedTo } => AttachedTo(),
        _ => throw new RulesNotImplementedException(
            "projected threat-removal prohibition requires a supported projected selection"),
    };

    private Card? AttachedTo()
    {
        int host = Hosts.GetValueOrDefault(source!.ObjectId, source.Area.Host);
        return host >= 0 && !Departed.Contains(host) ? World.Cards[host] : null;
    }

    internal static AbilityThreatPermissionProjection From(World world, AreaProjectionState state) =>
        new(world, state.Departed, state.Entered, state.Hosts);

    internal static AbilityThreatPermissionProjection From(World world, AbilityPowerState state) =>
        new(world, state.Discarded, new HashSet<int>(), new Dictionary<int, int>());
}

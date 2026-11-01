using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Quotes Make the Call's candidates: allies in player discard piles and each one's price.
internal static class MakeTheCallOffers
{
    // Player order and then pile order match the rules query; absent piles are empty.
    internal static IReadOnlyList<Card> AlliesInPlayerDiscards(World world) =>
    [
        .. world.PlayerOrder.SelectMany(player => world.Areas.FirstOrDefault(area =>
                area.Type == DeckType.DiscardPile && area.PlayArea == PlayArea.Of(player)
                && area.Host == -1)?.Cards ?? [])
            .Where(card => EffectiveCards.Kind(card, world.Facts) == CardKind.Ally),
    ];

    internal static CostOption Price(World world, Card ally, IReadOnlyList<ResourceSource> sources) =>
        new(ally.ObjectId,
            (Resources.Cost(ally.FaceId, world.Facts, world.Players) ?? 0)
                .ToString(System.Globalization.CultureInfo.InvariantCulture),
            Resources.Required(world, ally, world.Facts) is { Length: > 0 } rule ? [rule] : null,
            Sources: sources);

    internal static bool Affordable(World world, Card ally, IReadOnlyList<ResourceSource> sources) =>
        ResourcePayment.Allocate(Price(world, ally, sources),
            sources.Select(source => source.Effect).ToArray()) is not null;

    internal static IReadOnlyList<ResourceSource> Sources(
        World world, int player, Card source, Card ally,
        IResourceCardAbilities resourceAbilities) =>
    [
        .. CardPayment.Generators(
                world, world.Facts, world.Seats[player], resourceAbilities, payingFor: ally)
            .Where(generator => generator.Effect != source.ObjectId),
    ];
}

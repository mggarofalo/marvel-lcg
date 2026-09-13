using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Builds compact table metadata after visibility filtering.</summary>
internal static class TableDescriptorProjection
{
    internal static WorldDescriptor WithContext(
        WorldDescriptor world,
        Prompt? prompt,
        ViewScope scope,
        int active,
        int firstPlayer) => world with
    {
        Table = new TableContextDescriptor(
            PromptOwner: prompt?.Player,
            ViewedPrivateSeat: scope.SoleSeat,
            ActivePlayer: active,
            FirstPlayer: firstPlayer,
            PublicFocusSeat: active),
        PlayerSummaries = PlayerSummaries(world, prompt),
        Relationships = Relationships(world),
    };

    internal static IReadOnlyList<PlayerSummaryDescriptor> PlayerSummaries(
        WorldDescriptor world,
        Prompt? prompt) => [.. world.Players.Select(player => Summary(world, prompt, player))];

    internal static List<TableRelationshipDescriptor> Relationships(WorldDescriptor world)
    {
        CardDescriptor[] cards = [.. world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null)];
        HashSet<int> ids = [.. cards.Select(card => card.Id!.Value)];
        var relationships = new List<TableRelationshipDescriptor>();
        foreach (CardDescriptor card in cards)
        {
            int id = card.Id!.Value;
            if (card.Host >= 0 && ids.Contains(card.Host))
            {
                relationships.Add(new TableRelationshipDescriptor(
                    RelationshipKind.Attachment, id, card.Host));
            }
            if (card.Location?.EngagedWith is int seat && seat >= 0)
            {
                relationships.Add(new TableRelationshipDescriptor(
                    RelationshipKind.Engagement, id, Related: null, Seat: seat));
            }
        }

        return relationships;
    }

    private static PlayerSummaryDescriptor Summary(
        WorldDescriptor world,
        Prompt? prompt,
        PlayerDescriptor player)
    {
        CardDescriptor? identity = Identity(world, player);
        return new PlayerSummaryDescriptor(
            player.Seat,
            identity?.Id,
            identity?.Face?.Kind.ToString(),
            identity?.State?.Values.GetValueOrDefault("health"),
            Engaged(world, player),
            identity?.State?.Statuses ?? [],
            Defenders(world, prompt, player));
    }

    private static CardDescriptor? Identity(WorldDescriptor world, PlayerDescriptor player) =>
        world.Areas.Where(area => area.Owner == player.Seat
                && area.Zone == DeckType.HeroArea.ToString())
            .SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.Id is not null && card.Face is not null);

    private static int[] Engaged(WorldDescriptor world, PlayerDescriptor player) =>
        [.. world.Areas.Where(area => area.Owner == player.Seat
                && area.Zone == DeckType.EngagedEnemiesArea.ToString())
            .SelectMany(area => area.Cards)
            .Where(card => card.Id is not null)
            .Select(card => card.Id!.Value)];

    private static int[] Defenders(
        WorldDescriptor world,
        Prompt? prompt,
        PlayerDescriptor player)
    {
        if (prompt is null) return [];
        HashSet<int> visible = [.. world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null).Select(card => card.Id!.Value)];
        return [.. prompt.Affordances
            .Where(option => string.Equals(option.Verb, "Defend", StringComparison.Ordinal)
                && option.AnchorPlayer == player.Seat)
            .Select(option => option.AnchorId)
            .Where(visible.Contains)];
    }
}

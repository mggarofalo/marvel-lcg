using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A read-only elimination layout over the live board.</summary>
public sealed class WorldEliminationLayout : IEliminationLayout
{
    private readonly World world;
    private readonly ICardFacts facts;

    /// <summary>Read placement in place without creating any game areas.</summary>
    public WorldEliminationLayout(World world, ICardFacts? facts = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        this.world = world;
        this.facts = facts ?? world.Facts;
    }

    /// <inheritdoc />
    public int Players => world.Players;

    /// <inheritdoc />
    public bool IsEliminated(int player) => world.Seats[player].Eliminated;

    /// <inheritdoc />
    public IEnumerable<int> Cards => world.Areas.SelectMany(
        area => area.Cards.Select(card => card.ObjectId));

    /// <inheritdoc />
    public EliminationPlacement Placement(int card)
    {
        var area = world.Cards[card].Area;
        return new EliminationPlacement(
            area.PlayArea, area.Host, area.Type == DeckType.EngagedEnemiesArea);
    }

    /// <inheritdoc />
    public bool RequiresAttachTo(int card)
    {
        var current = world.Cards[card];
        return DeckTypes.IsInPlay(current.Area.Type)
            && facts.Kind(current.FaceId) == CardKind.Attachment
            && StateFields.Modified(world, current, "permanent", facts, world.Players) > 0;
    }
}

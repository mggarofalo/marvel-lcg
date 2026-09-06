using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Placement reads after known departures and engagement changes.</summary>
internal sealed class AbilityEliminationLayout(
    World world, IReadOnlySet<int> departed,
    IReadOnlyDictionary<int, int> engagement) : IEliminationLayout
{
    private readonly WorldEliminationLayout live = new(world);

    public int Players => live.Players;

    public bool IsEliminated(int player) => live.IsEliminated(player)
        || departed.Contains(world.Seats[player].IdentityCard.ObjectId);

    public IEnumerable<int> Cards => live.Cards.Where(card => !departed.Contains(card));

    public bool RequiresAttachTo(int card) => live.RequiresAttachTo(card);

    public EliminationPlacement Placement(int card)
    {
        var placement = live.Placement(card);
        if (!engagement.TryGetValue(card, out int player))
        {
            return placement;
        }

        // Engagement overlays also move hosted cards into their host's new
        // play area. Only a minion itself becomes an engaged root.
        return placement with
        {
            PlayArea = PlayArea.Of(player),
            Engaged = FacedownDrones.Kind(world.Cards[card], world.Facts) == CardKind.Minion,
        };
    }
}

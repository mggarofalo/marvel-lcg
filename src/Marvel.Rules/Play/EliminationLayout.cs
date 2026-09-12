using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The ordered placement and player facts needed by elimination.</summary>
/// <remarks>
/// Implementations may read a live board or a projected overlay. They expose no
/// card text, hidden card faces, mutation, randomness or timing operations.
/// </remarks>
public interface IEliminationLayout
{
    /// <summary>The original number of players, including eliminated seats.</summary>
    int Players { get; }

    /// <summary>Whether a seat has already left the game.</summary>
    bool IsEliminated(int player);

    /// <summary>Present cards in area order, then pile order within each area.</summary>
    IEnumerable<int> Cards { get; }

    /// <summary>The card's current or projected placement.</summary>
    EliminationPlacement Placement(int card);

    /// <summary>Whether a departing card requires permanent attachment resolution.</summary>
    bool RequiresAttachTo(int card);
}

/// <summary>Only the placement facts used by player elimination.</summary>
/// <param name="PlayArea">The play area containing the card.</param>
/// <param name="Host">Its host, or a negative value for an unhosted card.</param>
/// <param name="Engaged">Whether this is a minion in an engaged-enemies area.</param>
public readonly record struct EliminationPlacement(PlayArea PlayArea, int Host, bool Engaged);

/// <summary>One engaged minion and its ordered, retained hosted descendants.</summary>
/// <param name="Minion">The root that engages the next player.</param>
/// <param name="Hosted">Descendants in parent-before-child movement order.</param>
public sealed record EliminationRelocation(int Minion, ImmutableArray<int> Hosted);

/// <summary>A deterministic elimination layout, without executing any departure.</summary>
/// <param name="NextPlayer">The next surviving clockwise player, or none.</param>
/// <param name="Relocations">Minions and hosted cards retained by step 2.</param>
/// <param name="Leaving">Cards left in the eliminated play area after step 2.</param>
public sealed record EliminationLayout(
    int? NextPlayer,
    ImmutableArray<EliminationRelocation> Relocations,
    ImmutableArray<int> Leaving)
{
    /// <summary>All relocated cards, roots before descendants, in movement order.</summary>
    public IEnumerable<int> RelocatedCards => Relocations.SelectMany(
        relocation => relocation.Hosted.Prepend(relocation.Minion));

    /// <summary>Calculate steps 1 and 2 and the remaining departure membership.</summary>
    public static EliminationLayout Calculate(IEliminationLayout read, int player)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, read.Players);

        int? next = FindNextPlayer(read, player);
        // rr:player-elimination.step.1: "the next clockwise player"; .6:
        // "Effects that refer to the players in the game ignore eliminated players".
        // This is a bounded topology read, not a cloned game state. Area and
        // pile order are the engine's deterministic order for moving siblings.
        var cards = read.Cards.Select(id => (Id: id, At: read.Placement(id))).ToArray();
        var placements = cards.ToDictionary(card => card.Id, card => card.At);
        var children = cards.ToLookup(card => card.At.Host, card => card.Id);
        var retained = new HashSet<int>();
        var relocations = BuildRelocations(cards, children, player, next, retained);

        // An area whose host belongs to another play area is not part of this
        // player's departure. Overlay readers supply projected locations here,
        // including cards moved by an earlier elimination in the same ability.
        var leaving = cards.Where(card =>
                card.At.PlayArea == PlayArea.Of(player)
                && !retained.Contains(card.Id)
                && (card.At.Host < 0
                    || placements.TryGetValue(card.At.Host, out var host)
                        && host.PlayArea == PlayArea.Of(player)))
            .Select(card => card.Id).ToImmutableArray();
        ValidateLeaving(read, leaving);
        return new EliminationLayout(next, relocations.ToImmutable(), leaving);
    }

    private static int? FindNextPlayer(IEliminationLayout read, int player)
    {
        for (int offset = 1; offset < read.Players; offset++)
        {
            int candidate = (player + offset) % read.Players;
            if (!read.IsEliminated(candidate)) return candidate;
        }
        return null;
    }

    private static ImmutableArray<EliminationRelocation>.Builder BuildRelocations(
        (int Id, EliminationPlacement At)[] cards, ILookup<int, int> children,
        int player, int? next, HashSet<int> retained)
    {
        var result = ImmutableArray.CreateBuilder<EliminationRelocation>();
        if (next is null) return result;
        foreach (var root in cards.Where(card =>
            card.At.Engaged && card.At.PlayArea == PlayArea.Of(player)))
        {
            var hosted = Hosted(children, root.Id, out HashSet<int> seen);
            retained.UnionWith(seen);
            result.Add(new EliminationRelocation(root.Id, hosted));
        }
        return result;
    }

    private static ImmutableArray<int> Hosted(
        ILookup<int, int> children, int root, out HashSet<int> seen)
    {
        var hosted = ImmutableArray.CreateBuilder<int>();
        seen = [root];
        var pending = new Stack<int>(children[root].Reverse());
        while (pending.TryPop(out int card))
        {
            if (!seen.Add(card))
                throw new RulesNotImplementedException($"attachment {card} forms a hosting cycle");
            hosted.Add(card);
            foreach (int child in children[card].Reverse()) pending.Push(child);
        }
        return hosted.ToImmutable();
    }

    private static void ValidateLeaving(IEliminationLayout read, ImmutableArray<int> leaving)
    {
        foreach (int card in leaving)
            if (read.RequiresAttachTo(card))
                throw new RulesNotImplementedException(
                    $"card {card} is a permanent attachment on an eliminated "
                    + "player's board, and rr:player-elimination.1 resolves its "
                    + "'attach to' text, which is not modelled");
    }
}

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

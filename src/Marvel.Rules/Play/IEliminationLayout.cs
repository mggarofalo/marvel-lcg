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

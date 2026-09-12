using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Only the placement facts used by player elimination.</summary>
/// <param name="PlayArea">The play area containing the card.</param>
/// <param name="Host">Its host, or a negative value for an unhosted card.</param>
/// <param name="Engaged">Whether this is a minion in an engaged-enemies area.</param>
public readonly record struct EliminationPlacement(PlayArea PlayArea, int Host, bool Engaged);

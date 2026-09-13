using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>A play area joined a game area.</summary>
/// <param name="PlayArea">
/// The player's seat, or <c>-1</c> for the villain's play area.
/// </param>
/// <param name="GameArea">The destination game area's identity.</param>
/// <remarks>
/// <para>
/// One event for the play area, not one per card. A game area groups play
/// areas, so every card in the moving play area follows without moving between
/// card areas or changing any card field.
/// </para>
/// <para>
/// <b>Emitted-only.</b> A v2 digest cannot see game-area membership, so no
/// before/after digest comparison can derive this event. The engine can emit it
/// directly because <c>World.Join</c> performs the operation. The split between
/// derivable and emitted-only wire kinds is the engine's choice; the published
/// rule establishes the operation, not its JSON representation.
/// </para>
/// </remarks>
public sealed record PlayAreaJoined(int PlayArea, int GameArea) : GameEvent;

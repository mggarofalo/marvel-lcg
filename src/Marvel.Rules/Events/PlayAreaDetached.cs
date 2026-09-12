using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>A play area left its game area without joining another.</summary>
/// <param name="PlayArea">
/// The player's seat, or <c>-1</c> for the villain's play area.
/// </param>
/// <param name="GameArea">The prior game area's identity.</param>
/// <remarks>
/// <para>
/// One event for the play area, not one per card. The prior game area is part
/// of the payload because the operation removes that membership and leaves no
/// destination from which a client could infer it.
/// </para>
/// <para>
/// <b>Emitted-only.</b> Like <see cref="PlayAreaJoined"/>, this topology change
/// is absent from the v2 digest. <c>World.Detach</c> observes the prior
/// membership before removing it and emits the event directly.
/// </para>
/// </remarks>
public sealed record PlayAreaDetached(int PlayArea, int GameArea) : GameEvent;

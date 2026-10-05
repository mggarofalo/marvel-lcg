namespace Marvel.Rules.Events;

/// <summary>Established participants when an enemy attack ends.</summary>
/// <remarks>
/// This emitted-only wire shape is an engine choice. It records the completed
/// damage step; health changes and prevention retain their own events.
/// </remarks>
/// <param name="Enemy">The attacking enemy.</param>
/// <param name="Target">The attacked character when the attack ended.</param>
/// <param name="Defender">The defending character, or -1 when undefended.</param>
public sealed record AttackCompleted(int Enemy, int Target, int Defender) : GameEvent
{
    /// <summary>Actual attack-step damage, excluding boost abilities; null when not recorded.</summary>
    public long? DamageDealt { get; init; }
}

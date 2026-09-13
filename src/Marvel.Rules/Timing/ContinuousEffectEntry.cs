using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>One registered effect and its remaining uses.</summary>
/// <remarks>
/// The effect itself is immutable, because it has to be writable to a save.
/// How much of it is left over is not part of what the card says, so it
/// lives here.
/// </remarks>
internal sealed class ContinuousEffectEntry(ContinuousEffect effect)
{
    public ContinuousEffect Effect { get; } = effect;

    public int? Remaining { get; set; } = effect.Lasts?.Uses;
}

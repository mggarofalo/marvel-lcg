namespace Marvel.Core.Random;

/// <summary>A captured <see cref="MersenneTwister"/> state.</summary>
public sealed class MersenneTwisterState(IReadOnlyList<uint> words, int index)
{
    /// <summary>The 624 state words.</summary>
    public IReadOnlyList<uint> Words { get; } = words;

    /// <summary>The extraction index, <c>0..624</c>.</summary>
    public int Index { get; } = index;
}

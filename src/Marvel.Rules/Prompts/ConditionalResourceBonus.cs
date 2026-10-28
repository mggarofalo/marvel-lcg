namespace Marvel.Rules.Prompts;

/// <summary>Resources produced if this source pays at least one indicated cost component.</summary>
/// <param name="Generates">Additional resource-type letters.</param>
/// <param name="ComponentMask">Bit N identifies qualifying component N, up to 63.</param>
public sealed record ConditionalResourceBonus(string Generates, ulong ComponentMask)
{
    /// <summary>Whether a component qualifies for this bonus.</summary>
    public bool AppliesTo(int component) => component is >= 0 and < 64
        && (ComponentMask & (1UL << component)) != 0;
}

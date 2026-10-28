using System.Text.Json.Serialization;
using Marvel.Rules.Play;

namespace Marvel.Rules.Prompts;

/// <summary>Resources produced if this source pays at least one indicated cost component.</summary>
/// <param name="Generates">Additional resource-type letters.</param>
/// <param name="ComponentMask">Bit N identifies qualifying component N, up to 63.</param>
public sealed record ConditionalResourceBonus(
    [property: JsonRequired] string Generates,
    [property: JsonRequired] ulong ComponentMask)
{
    /// <summary>Whether this bonus names valid icons and at least one represented component.</summary>
    public bool IsValid(int componentCount) => Generates is { Length: > 0 }
        && Generates.All(Resources.Types.Contains)
        && ComponentMask != 0 && componentCount > 0
        && (componentCount >= 64 || ComponentMask >> componentCount == 0);

    /// <summary>Whether a component qualifies for this bonus.</summary>
    public bool AppliesTo(int component) => component is >= 0 and < 64
        && (ComponentMask & (1UL << component)) != 0;
}

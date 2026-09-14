using Marvel.Rules.Play;

namespace Marvel.Godot;

/// <summary>Names engine resource symbols for accessible decision copy.</summary>
internal static class DecisionResourceName
{
    internal static string For(char resource) => resource switch
    {
        Resources.Mental => "mental",
        Resources.Energy => "energy",
        Resources.Physical => "physical",
        Resources.Wild => "wild",
        _ => $"resource {resource}",
    };
}

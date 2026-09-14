using Godot;

namespace Marvel.Godot;

/// <summary>Maps the optional presentation environment value to a supported scale.</summary>
internal static class ClientThemeScale
{
    internal static InterfaceScale Configured() => OS.GetEnvironment("MARVEL_UI_SCALE").Trim().ToLowerInvariant() switch
    {
        "50" or "50%" => InterfaceScale.Percent50,
        "60" or "60%" => InterfaceScale.Percent60,
        "70" or "70%" => InterfaceScale.Percent70,
        "80" or "80%" or "compact" => InterfaceScale.Percent80,
        "90" or "90%" => InterfaceScale.Percent90,
        "100" or "100%" or "standard" => InterfaceScale.Percent100,
        "110" or "110%" => InterfaceScale.Percent110,
        "120" or "120%" or "large" => InterfaceScale.Percent120,
        "130" or "130%" => InterfaceScale.Percent130,
        "140" or "140%" => InterfaceScale.Percent140,
        "150" or "150%" or "extra-large" => InterfaceScale.Percent150,
        _ => InterfaceScale.Compact,
    };
}

namespace Marvel.Godot;

/// <summary>Keeps a supplied current numeral separate from its printed annotations.</summary>
internal sealed record CardStatValue(string Name, string Printed, string Value,
    bool PerPlayer, int ConsequentialDamage)
{
    internal bool Modified => Printed != Value;
}

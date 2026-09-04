using System.Globalization;

namespace Marvel.Cards.Dsl;

/// <summary>A deterministic source location for authored ability diagnostics.</summary>
/// <remarks>
/// The engine chooses this diagnostic spelling. It is not a session-ledger
/// address and does not change the existing continuation wire format.
/// </remarks>
public sealed record AbilityLocation(string Card, int Ability, string Path)
{
    /// <summary>The location of a named child.</summary>
    public AbilityLocation Child(string name) => this with { Path = Path + "/" + name };

    /// <summary>The location of an item in an authored list.</summary>
    public AbilityLocation Item(int index) => Child(index.ToString(CultureInfo.InvariantCulture));

    /// <summary>An authored-data failure at this location.</summary>
    public AbilityException Error(string message) => new(
        $"'{Card}' ability {Ability.ToString(CultureInfo.InvariantCulture)} at '{Path}': {message}");
}

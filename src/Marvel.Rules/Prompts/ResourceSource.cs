using System.Text.Json.Serialization;

namespace Marvel.Rules.Prompts;

/// <summary>Something that can generate resources toward a cost.</summary>
/// <param name="Effect">
/// The effect that generates. Handed back when the player chooses to use it.
/// </param>
/// <param name="Generates">
/// What it produces, as resource-type letters — one per resource, so a card
/// generating two of a type appears as two letters.
/// </param>
/// <remarks>
/// A generator is itself an effect, not a passive property of a card: using it
/// is a thing the player does, which is why it carries an id that can be handed
/// back in rather than a card reference.
/// </remarks>
public readonly record struct ResourceSource(int Effect, string Generates)
{
    /// <summary>Additional output when this source pays one of the indicated components.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConditionalResourceBonus? ConditionalBonus { get; init; }

    /// <summary>All icons that may be assigned, including conditional output.</summary>
    [JsonIgnore]
    public string PossibleResources => Generates + (ConditionalBonus?.Generates ?? string.Empty);

    /// <summary>Printed spending restriction, evaluated by the engine against the cost's card.</summary>
    public ResourceRestriction Restriction { get; init; }

    /// <summary>The offered resource ability's authored name; null for a hand discard.</summary>
    public string? AbilityName { get; init; }

    /// <summary>Engine-described non-resource commitments of this offered ability.</summary>
    public string? CostDescription { get; init; }
}

namespace Marvel.Rules.Play;

/// <summary>Completion conditions shared by basic and card-labelled character powers.</summary>
public static class CharacterPowerTiming
{
    /// <summary>A character has resolved a basic or card-labelled thwart.</summary>
    /// <remarks>
    /// rr:thwart.2: "resolving that ability is considered to thwart the specified scheme."
    /// The condition's spelling is an engine choice.
    /// </remarks>
    public const string ThwartEnds = "WhenThwartEnds";
}

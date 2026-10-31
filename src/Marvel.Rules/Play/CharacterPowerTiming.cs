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

    /// <summary>A hero's thwart removed the last threat from a scheme.</summary>
    /// <remarks>
    /// Turn the Tide (15015): "After your hero thwarts and removes all threat
    /// from a scheme". The form is read when the threat is removed: ruling
    /// 2f5ce668fd00dbb2 refuses an alter-ego that removed the last threat, and
    /// ruling 0e978ae1cc77dc8c accepts a hero that changed form afterwards. The
    /// condition joins the thwart's occurrence and its response window; its
    /// spelling is an engine choice.
    /// </remarks>
    public const string HeroClearedScheme = "WhenHeroThwartRemovesAllThreat";
}

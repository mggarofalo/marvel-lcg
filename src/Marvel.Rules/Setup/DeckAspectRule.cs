namespace Marvel.Rules.Setup;

/// <summary>Authored identity instructions that replace ordinary aspect construction.</summary>
public enum DeckAspectRule
{
    /// <summary>One chosen aspect and Basic cards.</summary>
    Standard,
    /// <summary>Two chosen aspects, with equal card counts for those aspects.</summary>
    EqualTwoAspects,
}

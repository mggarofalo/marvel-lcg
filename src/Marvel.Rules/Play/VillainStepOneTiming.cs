namespace Marvel.Rules.Play;

/// <summary>Distinguishes positive main-scheme placement from completion of the whole phase step.</summary>
public static class VillainStepOneTiming
{
    /// <summary>Positive placement; the engine retains this authored spelling for replay compatibility.</summary>
    public const string ThreatPlaced = "WhenVillainPhaseStepOneEnds";
    /// <summary>Completion regardless of prevention or replacement of the threat assignment.</summary>
    public const string Completed = "WhenVillainPhaseStepOneCompleted";
}

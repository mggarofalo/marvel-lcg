namespace Marvel.Rules.Play;

/// <summary>Completion conditions specific to a printed basic power.</summary>
public static class BasicPowerTiming
{
    /// <summary>A hero is making a basic attack, before its damage resolves.</summary>
    public const string HeroAttackInitiated = "WhenHeroBasicAttackInitiated";
    /// <summary>A hero is making a basic thwart, before its threat removal resolves.</summary>
    public const string HeroThwartInitiated = "WhenHeroBasicThwartInitiated";
    /// <summary>A hero is making one basic attack or thwart.</summary>
    public const string HeroUseInitiated = "WhenHeroBasicUseInitiated";
    /// <summary>An ally is making a basic attack, before its damage resolves.</summary>
    public const string AllyAttackInitiated = "WhenAllyBasicAttackInitiated";
    /// <summary>An ally is making one basic attack or thwart.</summary>
    public const string AllyUseInitiated = "WhenAllyBasicUseInitiated";
    /// <summary>A successful basic thwart has resolved.</summary>
    public const string ThwartEnds = "WhenBasicThwartEnds";
    /// <summary>A character has used its basic recovery power.</summary>
    public const string RecoveryUsed = "WhenBasicRecoveryUsed";
}

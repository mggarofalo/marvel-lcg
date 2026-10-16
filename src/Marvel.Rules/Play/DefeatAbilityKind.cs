namespace Marvel.Rules.Play;

/// <summary>Closed stages of one rules procedure.</summary>
public enum DefeatAbilityKind
{
    /// <summary>ChooseWouldBeDefeated procedure stage.</summary>
    ChooseWouldBeDefeated,
    /// <summary>ResumeWouldBeDefeated procedure stage.</summary>
    ResumeWouldBeDefeated,
    /// <summary>ChooseCardDefeatedAbility procedure stage.</summary>
    ChooseCardDefeatedAbility,
    /// <summary>ResumeCardDefeatedAbility procedure stage.</summary>
    ResumeCardDefeatedAbility,
}

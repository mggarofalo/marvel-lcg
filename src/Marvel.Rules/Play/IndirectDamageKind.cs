namespace Marvel.Rules.Play;

/// <summary>Closed stages of one rules procedure.</summary>
public enum IndirectDamageKind
{
    /// <summary>AssignIndirectAttackDamage procedure stage.</summary>
    AssignIndirectAttackDamage,
    /// <summary>PrepareIndirectAttackDamage procedure stage.</summary>
    PrepareIndirectAttackDamage,
    /// <summary>ApplyIndirectAttackDamage procedure stage.</summary>
    ApplyIndirectAttackDamage,
    /// <summary>FinishIndirectAttackDamage procedure stage.</summary>
    FinishIndirectAttackDamage,
}

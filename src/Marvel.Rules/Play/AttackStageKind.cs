namespace Marvel.Rules.Play;

/// <summary>Closed stages of one rules procedure.</summary>
public enum AttackStageKind
{
    /// <summary>GiveBoostCard procedure stage.</summary>
    GiveBoostCard,
    /// <summary>DeclareDefender procedure stage.</summary>
    DeclareDefender,
    /// <summary>FlipBoostCards procedure stage.</summary>
    FlipBoostCards,
    /// <summary>CalculateAttackDamage procedure stage.</summary>
    CalculateAttackDamage,
    /// <summary>DealAttackDamage procedure stage.</summary>
    DealAttackDamage,
    /// <summary>NextAttackTarget procedure stage.</summary>
    NextAttackTarget,
}

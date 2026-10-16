namespace Marvel.Rules.Prompts;

/// <summary>The engine-owned reason an answer does not satisfy its current offer.</summary>
public enum DecisionRejection
{
    /// <summary>The answer is accepted.</summary>
    None,
    /// <summary>The handle does not identify exactly one offered action.</summary>
    MissingOffer,
    /// <summary>The answer belongs to another seat.</summary>
    UnauthorizedActor,
    /// <summary>This prompt requires an answer.</summary>
    DeclineNotAllowed,
    /// <summary>A decline or free action carries unrelated answer data.</summary>
    UnexpectedAnswerData,
    /// <summary>The offered action is explicitly unavailable.</summary>
    IllegalOffer,
    /// <summary>The ordered target selection is not accepted.</summary>
    InvalidTargets,
    /// <summary>The draft has not selected an offered cost.</summary>
    CostSelectionRequired,
    /// <summary>The selected cost belongs to another target.</summary>
    CostTargetMismatch,
    /// <summary>The variables do not satisfy one complete offered cost.</summary>
    InvalidVariables,
    /// <summary>The generators and allocations do not pay the offered cost.</summary>
    InvalidPayment,
}

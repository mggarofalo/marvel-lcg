namespace Marvel.Rules.Prompts;

/// <summary>A pure assessment of an answer, without committing gameplay state.</summary>
public readonly record struct DecisionAssessmentResult(DecisionRejection Rejection, string? Reason = null)
{
    /// <summary>Whether every offered constraint is satisfied.</summary>
    public bool IsAccepted => Rejection == DecisionRejection.None;
}

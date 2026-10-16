using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Explains an engine assessment without deciding whether the answer is legal.</summary>
internal static class DecisionAssessmentPresentation
{
    internal static string? Error(DecisionAssessmentResult result) => result.Rejection switch
    {
        DecisionRejection.None => null,
        DecisionRejection.MissingOffer => "Choose an action.",
        DecisionRejection.UnauthorizedActor => "This decision belongs to another player.",
        DecisionRejection.DeclineNotAllowed => "This decision cannot be declined.",
        DecisionRejection.UnexpectedAnswerData => "This action has no payment.",
        DecisionRejection.IllegalOffer => result.Reason,
        DecisionRejection.InvalidTargets => "Complete the offered target selection.",
        _ => PaymentError(result.Rejection),
    };

    private static string PaymentError(DecisionRejection rejection) => rejection switch
    {
        DecisionRejection.CostSelectionRequired => "Choose an offered cost.",
        DecisionRejection.CostTargetMismatch => "Choose the target associated with this cost.",
        DecisionRejection.InvalidVariables => "Define every requested value inside its offered range.",
        DecisionRejection.InvalidPayment => "Assign generated icons to satisfy every offered cost component.",
        _ => throw new ArgumentOutOfRangeException(nameof(rejection)),
    };
}

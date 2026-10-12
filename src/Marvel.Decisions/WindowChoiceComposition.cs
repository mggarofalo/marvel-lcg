using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Composes only a complete intention explicitly offered by the current window.</summary>
public static class WindowChoiceComposition
{
    /// <summary>Selects an exact current source/recipient tuple without answering a future prompt.</summary>
    public static bool SelectWindowChoice(this DecisionComposer composer, int affordanceId, int? targetId)
    {
        ArgumentNullException.ThrowIfNull(composer);
        Affordance? offered = composer.Prompt.Affordances.FirstOrDefault(option => option.Id == affordanceId);
        if (composer.Prompt.Asking != Question.Opportunity || !composer.Prompt.Cancellable
            || offered is null || !offered.WindowChoices.Any(choice => choice.TargetId == targetId))
            return false;
        composer.SelectAffordance(affordanceId);
        composer.SelectTargets(targetId is { } recipient ? [recipient] : []);
        return true;
    }
}

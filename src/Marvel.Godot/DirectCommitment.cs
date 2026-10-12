using Marvel.Decisions;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Ready named opportunity actions commit on their deliberate action click.</summary>
internal static class DirectCommitment
{
    internal static bool IsReady(DecisionComposer composer) => composer.Prompt.PublicKind is
        PublicDecisionKind.MinionActivationOrder or PublicDecisionKind.Response
            or PublicDecisionKind.Interrupt or PublicDecisionKind.Ability or PublicDecisionKind.Choice
        && composer.Selected is { CostOptions.Count: 0 }
        && composer.TryBuild(out _, out _);
}

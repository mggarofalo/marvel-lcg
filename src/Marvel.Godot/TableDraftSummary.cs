using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Summarizes only the engine-authorized values in the current table draft.</summary>
internal static class TableDraftSummary
{
    internal static string? From(DecisionComposer? composer, PromptPresentation? prompt)
    {
        if (composer?.Selected is not { } selected)
        {
            return null;
        }

        DecisionProgressPresentation progress = composer.Progress();
        AffordancePresentation? visible = prompt?.Affordances.FirstOrDefault(
            affordance => affordance.Id == selected.Id);
        string source = visible is null || string.IsNullOrWhiteSpace(visible.Anchor)
            ? selected.Label
            : $"{selected.Label} · {visible.Anchor}";
        var state = new List<string>();
        AddTargets(state, progress.Targets);
        AddPayment(state, progress.Payment);
        string readiness = progress.IsReady ? "READY" : "COMPOSING";
        string resources = progress.Payment.CostState == CostSelectionState.Selected
            ? $" · RES {progress.Payment.SelectedGenerators}"
            : string.Empty;
        return $"SELECTED · {source}\n{readiness}{resources}\n{string.Join(" · ", state)}";
    }

    private static void AddTargets(List<string> state, TargetSelectionProgress targets)
    {
        if (targets.Mode != TargetSelectionMode.None)
        {
            state.Add($"TARGETS {targets.Selected}/{targets.Minimum}–{targets.Maximum}");
        }
    }

    private static void AddPayment(List<string> state, PaymentProgress payment)
    {
        switch (payment.CostState)
        {
            case CostSelectionState.Required:
                state.Add("CHOOSE COST");
                break;
            case CostSelectionState.Selected:
                state.Add($"COST {payment.SelectedCost!.Value + 1}/{payment.CostOptions}");
                break;
            case CostSelectionState.NotRequired:
                state.Add("NO COST");
                break;
        }
    }
}

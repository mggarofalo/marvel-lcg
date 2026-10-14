using Godot;

namespace Marvel.Godot;

internal static class ActivityConnectorOcclusion
{
    private static readonly string[] surfaces =
        ["ContextualDecision", "EventTimeline", "PaymentWorkspace", "AbilityWindow", "CausalReferenceStack"];

    internal static Rect2[] Regions(Main main) =>
        [.. (main.boardRender?.VisibleCardControls() ?? [])
            .Where(card => card.IsVisibleInTree() && card.Modulate.A > 0)
            .Select(card => SpatialCardFootprint.Face(card).Grow(2)),
         .. Surfaces(main)
            .Where(control => InteractionControl.IsUsable(control) && control!.IsVisibleInTree())
            .Select(control => control!.GetGlobalRect().Grow(6))];

    private static IEnumerable<Control?> Surfaces(Main main)
    {
        foreach (string name in surfaces) yield return main.FindChild(name, true, false) as Control;
        if (main.FindChild("CausalSituation", true, false) is Control situation)
            foreach (Label label in situation.FindChildren("*", "Label", true, false).OfType<Label>())
                if (!string.IsNullOrWhiteSpace(label.Text)) yield return label;
        if (main.promptPanel.TopLevel) yield return main.promptPanel;
        if (main.cardInspector.Visible) yield return main.cardInspectorFrame;
    }
}

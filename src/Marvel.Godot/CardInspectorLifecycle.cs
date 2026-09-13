using Godot;

namespace Marvel.Godot;

/// <summary>Reflows an open inspector only while its source remains live.</summary>
internal static class CardInspectorLifecycle
{
    internal static void Reposition(Main main)
    {
        if (!main.cardInspector.Visible)
        {
            return;
        }

        Control? source = main.cardInspectorState.Source;
        if (source is null
            || !CardInspectorPlacement.IsLive(source)
            || main.cardInspectorContent.GetChildCount() == 0
            || main.cardInspectorContent.GetChild(0) is not Control detail)
        {
            main.HideCardInspector();
            return;
        }

        InspectorPlacement placement = CardInspectorPlacement.Position(
            main, detail, source, main.cardInspectorPinned);
        main.cardInspectorBackdrop.Visible = main.cardInspectorPinned && !placement.IsAttached;
    }
}

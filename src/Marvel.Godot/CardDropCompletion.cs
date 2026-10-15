namespace Marvel.Godot;

/// <summary>Routes a completed physical drop to its local staging or offered-action destination.</summary>
internal static class CardDropCompletion
{
    internal static bool TryDrop(CardPointerGesture gesture, bool current,
        PointerMulliganDraft mulligan, BoardPointerDropSurfaces drops, HandGestureLayout hand,
        Func<CardPointerGesture, bool>? directDrag)
    {
        if (!current) return false;
        if (gesture.Card.TargetId is { } id && mulligan.Drop(id,
            drops.ContainsMulligan(gesture.Position), hand.Contains(gesture.Position))) return true;
        if (gesture.IsHandCard && hand.Drop(gesture.Source, gesture.Position)) return true;
        hand.CancelPreview();
        return directDrag?.Invoke(gesture) == true;
    }
}

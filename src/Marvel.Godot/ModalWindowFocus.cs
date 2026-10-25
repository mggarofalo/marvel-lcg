using Godot;

namespace Marvel.Godot;

/// <summary>Transfers modal focus without retaining a hidden exclusive child.</summary>
internal static class ModalWindowFocus
{
    internal static void Bind(Window window)
    {
        bool released = false;
        window.FocusExited += () =>
        {
            // Godot focuses the parent during Hide before clearing its exclusive
            // child. Accessibility focus must not return to that hidden child.
            if (window.Visible || !window.Exclusive) return;
            released = true;
            window.Exclusive = false;
        };
        window.VisibilityChanged += () =>
        {
            if (!released) return;
            released = false;
            window.Exclusive = true;
        };
    }
}

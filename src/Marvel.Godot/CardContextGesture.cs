using Godot;
using System.Runtime.CompilerServices;

namespace Marvel.Godot;

/// <summary>Right-click and keyboard menu gestures are separate from inspection and dragging.</summary>
internal static class CardContextGesture
{
    private static readonly ConditionalWeakTable<Control, Binding> bindings = new();

    internal static void Bind(Control source, Action open) => BindNode(source, open);

    private static void BindNode(Node node, Action open)
    {
        if (node is not Control source) return;
        if (bindings.TryGetValue(source, out Binding? existing)) existing.Open = open;
        else Attach(source, new Binding(open));
        foreach (Node child in source.GetChildren()) BindNode(child, open);
    }

    private static void Attach(Control source, Binding binding)
    {
        bindings.Add(source, binding);
        source.ChildEnteredTree += child => BindNode(child, () => binding.Open());
        source.GuiInput += input =>
        {
            if (!IsMenu(input)) return;
            source.AcceptEvent();
            binding.Open();
        };
    }

    private sealed class Binding(Action open)
    {
        internal Action Open { get; set; } = open;
    }

    private static bool IsMenu(InputEvent input) => input switch
    {
        InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } => true,
        InputEventKey { Pressed: true, Echo: false, Keycode: Key.Menu } => true,
        InputEventKey { Pressed: true, Echo: false, Keycode: Key.F10, ShiftPressed: true } => true,
        _ => false,
    };
}

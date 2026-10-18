using Godot;

namespace Marvel.Godot;

/// <summary>Native list drag lifetime; proposals and acceptance are supplied by the editor.</summary>
internal sealed partial class DeckTransferList : ItemList
{
    internal Func<Variant>? BeginTransfer { get; set; }
    internal Func<Variant, bool>? AcceptTransfer { get; set; }
    internal Action<Variant>? CompleteTransfer { get; set; }
    internal Action? EndTransfer { get; set; }

    public override Variant _GetDragData(Vector2 position) => BeginTransfer?.Invoke() ?? default;
    public override bool _CanDropData(Vector2 position, Variant data) => AcceptTransfer?.Invoke(data) ?? false;
    public override void _DropData(Vector2 position, Variant data) => CompleteTransfer?.Invoke(data);
    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd) EndTransfer?.Invoke();
    }
}

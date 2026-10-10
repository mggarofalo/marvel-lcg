using Godot;
namespace Marvel.Godot;
/// <summary>Drains managed native-reference finalizers before the scene tree releases the .NET runtime.</summary>
internal static class ManagedShutdownLifetime
{
    private static Window? root;
    internal static void Attach(SceneTree tree)
    {
        if (root is not null) return;
        root = tree.Root;
        root.TreeExited += Release;
    }
    private static void Release()
    {
        root!.TreeExited -= Release;
        root = null;
        // Scene children have exited; finish their queued native-reference finalizers
        // while Godot's C# instance bindings still exist.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

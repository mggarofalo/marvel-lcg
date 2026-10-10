using Godot;

namespace Marvel.Godot;

/// <summary>Native smoke stressor for managed reference finalizers pending at process shutdown.</summary>
public sealed partial class ResourceFinalizerProbe : Node
{
    /// <summary>Amplifies finalizer scheduling pressure, then follows ordinary SceneTree shutdown.</summary>
    public void QuitWithPendingReferences()
    {
        CreatePendingReferences();
        GC.Collect();
        GD.Print("RESOURCE_FINALIZER_PROBE_READY");
        GetTree().Quit();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void CreatePendingReferences()
    {
        // Godot normally finalizes wrappers received from native input. This probe
        // amplifies that backlog without disposing borrowed application resources.
        for (int index = 0; index < 10_000; index++) _ = new InputEventMouseMotion();
        _ = new FinalizerDelay();
    }

    private sealed class FinalizerDelay
    {
        // Test-only scheduling pressure: native shutdown must wait for pending
        // reference finalizers instead of releasing their C# instance bindings.
        ~FinalizerDelay() => Thread.Sleep(100);
    }
}

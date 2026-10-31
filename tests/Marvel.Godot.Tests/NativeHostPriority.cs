using System.Diagnostics;

namespace Marvel.Godot.Tests;

/// <summary>Keeps the in-test authoritative host schedulable beside a rendered Windows client.</summary>
/// <remarks>
/// A rendered Godot project on Windows raises its own process to
/// <c>ABOVE_NORMAL_PRIORITY_CLASS</c> (Godot's <c>DisplayServerWindows</c> does this for
/// any project outside low-processor mode). Hosted Windows runners start job processes
/// below normal, and software rendering keeps every core busy, so a host left there is
/// starved: CI measured 6–44 s of host time for an <c>open</c> that takes 0.4 s beside a
/// headless client, exceeding the client's 30 s exchange deadline. The host therefore takes
/// the client's priority class for the journey. Matching rather than exceeding the client is
/// our choice; it restores fair scheduling without letting the test outrank the client.
/// </remarks>
internal sealed class NativeHostPriority : IDisposable
{
    private readonly ProcessPriorityClass? restore;

    internal NativeHostPriority(bool renderedClient)
    {
        if (!renderedClient || !OperatingSystem.IsWindows()) return;
        using Process current = Process.GetCurrentProcess();
        if (current.PriorityClass is ProcessPriorityClass.AboveNormal or ProcessPriorityClass.High
            or ProcessPriorityClass.RealTime) return;
        restore = current.PriorityClass;
        current.PriorityClass = ProcessPriorityClass.AboveNormal;
    }

    public void Dispose()
    {
        if (restore is not { } previous) return;
        using Process current = Process.GetCurrentProcess();
        current.PriorityClass = previous;
    }
}

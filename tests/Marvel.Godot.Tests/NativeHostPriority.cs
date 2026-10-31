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
/// xUnit can run rendered journeys from several classes at once, so the process class is
/// reference-counted: the first holder raises it and the last restores it.
/// </remarks>
internal sealed class NativeHostPriority : IDisposable
{
    private static readonly object Gate = new();
    private static int holders;
    private static ProcessPriorityClass? restore;
    private readonly bool held;
    private bool disposed;

    internal NativeHostPriority(bool renderedClient)
    {
        if (!renderedClient || !OperatingSystem.IsWindows()) return;
        lock (Gate)
        {
            if (holders++ == 0)
            {
                using Process current = Process.GetCurrentProcess();
                if (current.PriorityClass is not (ProcessPriorityClass.AboveNormal or ProcessPriorityClass.High
                    or ProcessPriorityClass.RealTime))
                {
                    restore = current.PriorityClass;
                    current.PriorityClass = ProcessPriorityClass.AboveNormal;
                }
            }
            held = true;
        }
    }

    public void Dispose()
    {
        if (!held || disposed) return;
        disposed = true;
        lock (Gate)
        {
            if (--holders > 0 || restore is not { } previous) return;
            restore = null;
            using Process current = Process.GetCurrentProcess();
            current.PriorityClass = previous;
        }
    }
}

using System.Diagnostics;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class NativeHostPriorityTests
{
    [Fact]
    public void RenderedWindowsJourneyRunsTheHostAtTheClientsClassAndRestoresIt()
    {
        ProcessPriorityClass before = Current();
        using (new NativeHostPriority(renderedClient: true))
        {
            bool raised = before is ProcessPriorityClass.AboveNormal or ProcessPriorityClass.High
                or ProcessPriorityClass.RealTime;
            ProcessPriorityClass expected = OperatingSystem.IsWindows() && !raised
                ? ProcessPriorityClass.AboveNormal : before;
            Assert.Equal(expected, Current());
        }
        Assert.Equal(before, Current());
    }

    [Fact]
    public void OverlappingRenderedJourneysKeepTheClassUntilTheLastEnds()
    {
        ProcessPriorityClass before = Current();
        var first = new NativeHostPriority(renderedClient: true);
        ProcessPriorityClass held = Current();
        using (new NativeHostPriority(renderedClient: true)) Assert.Equal(held, Current());
        Assert.Equal(held, Current());
        first.Dispose();
        first.Dispose();
        Assert.Equal(before, Current());
    }

    [Fact]
    public void HeadlessJourneyLeavesTheHostPriorityAlone()
    {
        ProcessPriorityClass before = Current();
        using (new NativeHostPriority(renderedClient: false)) Assert.Equal(before, Current());
        Assert.Equal(before, Current());
    }

    private static ProcessPriorityClass Current()
    {
        using Process current = Process.GetCurrentProcess();
        return current.PriorityClass;
    }
}

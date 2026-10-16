namespace Marvel.Rules.Play;

/// <summary>Collects immediate execution prerequisites without pulling deferred activations forward.</summary>
internal static class AbilityOwnedWork
{
    internal static HashSet<long> Segment(AbilityExecution invocation)
    {
        var frames = new HashSet<long>(invocation.SegmentFrames);
        frames.UnionWith(Children(invocation));
        return frames;
    }

    internal static HashSet<long> Children(AbilityExecution invocation)
    {
        var frames = new HashSet<long>();
        foreach (var child in invocation.Children) Include(child, frames);
        return frames;
    }

    internal static HashSet<long> All(AbilityExecution invocation)
    {
        var frames = new HashSet<long>();
        Include(invocation, frames);
        return frames;
    }

    private static void Include(AbilityExecution invocation, HashSet<long> frames)
    {
        if (invocation.DeferredActivation) return;
        frames.UnionWith(invocation.Frames);
        foreach (var child in invocation.Children) Include(child, frames);
    }
}

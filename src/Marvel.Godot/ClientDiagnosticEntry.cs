using Godot;

namespace Marvel.Godot;

/// <summary>Routes explicit diagnostic launch modes before normal client initialization.</summary>
internal static class ClientDiagnosticEntry
{
    internal static bool TryStart(Control owner) =>
        PackagedHostedSmoke.TryStart(owner) || CardVisualSample.TryStart(owner) || CardFaceSample.TryStart(owner) || CardLiveStateSample.TryStart(owner);
}

using Godot;

namespace Marvel.Godot;

/// <summary>Initializes application lifetime and routes explicit diagnostic launch modes.</summary>
internal static class ClientStartup
{
    /// <returns>Whether a diagnostic mode owns initialization of this scene.</returns>
    internal static bool Initialize(Control owner)
    {
        ManagedShutdownLifetime.Attach(owner.GetTree());
        return PackagedHostedSmoke.TryStart(owner) || CardVisualSample.TryStart(owner) || CardFaceSample.TryStart(owner) || CardLiveStateSample.TryStart(owner) || CardSourceSample.TryStart(owner) || CardInspectionSample.TryStart(owner) || SourceTableauSample.TryStart(owner);
    }
}

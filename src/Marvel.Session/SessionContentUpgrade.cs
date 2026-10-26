using Marvel.Rules.Play;

namespace Marvel.Session;

/// <summary>A release-owned source identity whose unchanged content has been checked by the host.</summary>
public sealed class SessionContentUpgrade(SessionCompatibility source, SessionCompatibility target,
    SessionReplayUpgrade replayUpgrade = SessionReplayUpgrade.Unchanged)
{
    /// <summary>Verifies the original complete trace before changing only its build provenance.</summary>
    public SessionSave Verify(SessionSave original, Func<SessionSetup, ReplayOpenedGame> open)
    {
        RequireSupportedRuntime(original.Compatibility);
        _ = SessionReplay.Verify(original, source with { Application = target.Application }, open);
        return original with { Compatibility = target };
    }

    /// <summary>Verifies every campaign transition, proof and retained attempt before changing provenance.</summary>
    public CampaignSave Verify(
        CampaignSave original, Marvel.Rules.Campaign.CampaignDefinition definition,
        Func<SessionSetup, ReplayOpenedGame> open,
        Func<Game, Marvel.Rules.Campaign.CampaignGameProgress> inspect)
    {
        RequireSupportedRuntime(original.Compatibility);
        _ = CampaignReplay.Verify(original, definition, source with { Application = target.Application }, open, inspect);
        return original with
        {
            Compatibility = target,
            Attempts = [.. original.Attempts.Select(attempt => attempt with { Compatibility = target })],
            Journal = [.. original.Journal.Select(entry => entry.Proof is { } proof
                ? entry with { Proof = proof with { Compatibility = target } } : entry)],
        };
    }
    private void RequireSupportedRuntime(SessionCompatibility actual)
    {
        string expectedReplay = replayUpgrade switch
        {
            SessionReplayUpgrade.Unchanged => target.ReplayContract,
            SessionReplayUpgrade.CoreRorsV19ToV20 => RequireNamedReplay("engine-replay-v19", "engine-replay-v20"),
            SessionReplayUpgrade.CoreRorsV19ToV21 => RequireNamedReplay("engine-replay-v19", "engine-replay-v21"),
            SessionReplayUpgrade.CoreRorsV20ToV21 => RequireNamedReplay("engine-replay-v20", "engine-replay-v21"),
            SessionReplayUpgrade.CoreRorsV19ToV22 => RequireNamedReplay("engine-replay-v19", "engine-replay-v22"),
            SessionReplayUpgrade.CoreRorsV20ToV22 => RequireNamedReplay("engine-replay-v20", "engine-replay-v22"),
            SessionReplayUpgrade.CoreRorsV21ToV22 => RequireNamedReplay("engine-replay-v21", "engine-replay-v22"),
            SessionReplayUpgrade.CoreRorsV19ToV23 => RequireNamedReplay("engine-replay-v19", "engine-replay-v23"),
            SessionReplayUpgrade.CoreRorsV20ToV23 => RequireNamedReplay("engine-replay-v20", "engine-replay-v23"),
            SessionReplayUpgrade.CoreRorsV21ToV23 => RequireNamedReplay("engine-replay-v21", "engine-replay-v23"),
            SessionReplayUpgrade.CoreRorsV19ToV24 => RequireNamedReplay("engine-replay-v19", "engine-replay-v24"),
            SessionReplayUpgrade.CoreRorsV20ToV24 => RequireNamedReplay("engine-replay-v20", "engine-replay-v24"),
            SessionReplayUpgrade.CoreRorsV21ToV24 => RequireNamedReplay("engine-replay-v21", "engine-replay-v24"),
            SessionReplayUpgrade.CoreRorsV19ToV25 => RequireNamedReplay("engine-replay-v19", "engine-replay-v25"),
            SessionReplayUpgrade.CoreRorsV20ToV25 => RequireNamedReplay("engine-replay-v20", "engine-replay-v25"),
            SessionReplayUpgrade.CoreRorsV21ToV25 => RequireNamedReplay("engine-replay-v21", "engine-replay-v25"),
            SessionReplayUpgrade.CoreRorsV19ToV26 => RequireNamedReplay("engine-replay-v19", "engine-replay-v26"),
            SessionReplayUpgrade.CoreRorsV20ToV26 => RequireNamedReplay("engine-replay-v20", "engine-replay-v26"),
            SessionReplayUpgrade.CoreRorsV21ToV26 => RequireNamedReplay("engine-replay-v21", "engine-replay-v26"),
            SessionReplayUpgrade.CoreRorsV19ToV27 => RequireNamedReplay("engine-replay-v19", "engine-replay-v27"),
            SessionReplayUpgrade.CoreRorsV20ToV27 => RequireNamedReplay("engine-replay-v20", "engine-replay-v27"),
            SessionReplayUpgrade.CoreRorsV21ToV27 => RequireNamedReplay("engine-replay-v21", "engine-replay-v27"),
            _ => throw new SessionCompatibilityException("replay_identity_mismatch", "unsupported named replay upgrade"),
        };
        // Application downgrade, RNG and digest checks remain exact. Source identity and
        // every original gameplay fact are checked by ordinary strict replay below this boundary.
        SessionReplay.RequireRuntimeCompatibility(target with { ReplayContract = expectedReplay }, actual);
    }
    private string RequireNamedReplay(string expectedSource, string expectedTarget)
    {
        if (source.ReplayContract != expectedSource || target.ReplayContract != expectedTarget)
            throw new SessionCompatibilityException("replay_identity_mismatch", "unsupported named replay upgrade");
        return expectedSource;
    }
}

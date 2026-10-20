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
        var expected = replayUpgrade switch
        {
            SessionReplayUpgrade.Unchanged => target,
            SessionReplayUpgrade.CoreRorsV19ToV20
                when source.ReplayContract == "engine-replay-v19" && target.ReplayContract == "engine-replay-v20"
                => target with { ReplayContract = source.ReplayContract },
            SessionReplayUpgrade.CoreRorsV19ToV21
                when source.ReplayContract == "engine-replay-v19" && target.ReplayContract == "engine-replay-v21"
                => target with { ReplayContract = source.ReplayContract },
            SessionReplayUpgrade.CoreRorsV20ToV21
                when source.ReplayContract == "engine-replay-v20" && target.ReplayContract == "engine-replay-v21"
                => target with { ReplayContract = source.ReplayContract },
            _ => throw new SessionCompatibilityException("replay_identity_mismatch", "unsupported named replay upgrade"),
        };
        // Application downgrade, RNG and digest checks remain exact. Source identity and
        // every original gameplay fact are checked by ordinary strict replay below this boundary.
        SessionReplay.RequireRuntimeCompatibility(expected, actual);
    }

}

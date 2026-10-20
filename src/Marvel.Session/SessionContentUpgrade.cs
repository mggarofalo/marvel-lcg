using Marvel.Rules.Play;

namespace Marvel.Session;

/// <summary>A release-owned source identity whose unchanged content has been checked by the host.</summary>
public sealed class SessionContentUpgrade(SessionCompatibility source, SessionCompatibility target)
{
    /// <summary>Verifies the original complete trace before changing only its build provenance.</summary>
    public SessionSave Verify(SessionSave original, Func<SessionSetup, ReplayOpenedGame> open)
    {
        SessionReplay.RequireRuntimeCompatibility(target, original.Compatibility);
        _ = SessionReplay.Verify(original, source with { Application = target.Application }, open);
        return original with { Compatibility = target };
    }

    /// <summary>Verifies every campaign transition, proof and retained attempt before changing provenance.</summary>
    public CampaignSave Verify(
        CampaignSave original, Marvel.Rules.Campaign.CampaignDefinition definition,
        Func<SessionSetup, ReplayOpenedGame> open,
        Func<Game, Marvel.Rules.Campaign.CampaignGameProgress> inspect)
    {
        SessionReplay.RequireRuntimeCompatibility(target, original.Compatibility);
        _ = CampaignReplay.Verify(original, definition, source with { Application = target.Application }, open, inspect);
        return original with
        {
            Compatibility = target,
            Attempts = [.. original.Attempts.Select(attempt => attempt with { Compatibility = target })],
            Journal = [.. original.Journal.Select(entry => entry.Proof is { } proof
                ? entry with { Proof = proof with { Compatibility = target } } : entry)],
        };
    }
}

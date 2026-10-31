using Marvel.Rules.Campaign;
using Marvel.Rules.Play;

namespace Marvel.Session;

/// <summary>Reconstructs campaign transitions and verifies every game-derived change against its trace.</summary>
public static class CampaignReplay
{
    /// <summary>Rejects altered aggregate state and unproved progress instead of trusting a saved campaign log.</summary>
    public static CampaignState Verify(
        CampaignSave save, CampaignDefinition definition, SessionCompatibility compatibility,
        Func<SessionSetup, ReplayOpenedGame> open, Func<Game, CampaignGameProgress> inspect)
    {
        _ = CampaignSaveJson.Write(save);
        SessionReplay.RequireRuntimeCompatibility(compatibility, save.Compatibility);
        if (save.Configuration.Product != definition.Product
            || CampaignSaveJson.Canonical(save.Compatibility with { Application = compatibility.Application })
                != CampaignSaveJson.Canonical(compatibility))
            throw new SessionCompatibilityException("campaign_compatibility", "campaign content or replay identity is unsupported");
        var state = CampaignRules.Create(definition, save.Configuration.Players,
            save.Configuration.Expert, save.Configuration.Seed);
        var proofs = new Dictionary<int, SessionSave>();
        foreach (var entry in save.Journal)
            state = Apply(state, definition, entry, proof => VerifyProof(proof, compatibility, open, inspect), proofs);
        if (CampaignSaveJson.Canonical(state) != CampaignSaveJson.Canonical(save.State))
            throw new ReplayDivergenceException("campaign state differs from its accepted transitions");
        foreach (var attempt in save.Attempts)
        {
            CampaignGameSetup setup = attempt.Setup.Campaign
                ?? throw new ReplayDivergenceException("campaign attempt lacks deterministic campaign setup");
            if (!proofs.TryGetValue(setup.Attempt, out var proof))
                throw new ReplayDivergenceException("campaign attempt has no admitted start");
            CampaignProof.RequireExtension(proof, attempt);
            _ = SessionReplay.Verify(attempt, compatibility, open);
        }
        if (proofs.Count != save.Attempts.Length)
            throw new ReplayDivergenceException("campaign is missing a retained attempt");
        return state;
    }

    private static CampaignState Apply(
        CampaignState state, CampaignDefinition definition, CampaignJournalEntry entry,
        Func<SessionSave, CampaignGameProgress> verify, Dictionary<int, SessionSave> proofs)
    {
        return entry.Kind switch
        {
            "participation" => CampaignParticipationRules.Select(state, entry.Seat, entry.Participating),
            "mode" => CampaignRules.SelectScenarioMode(state, entry.Expert),
            "deck" => CampaignRules.ReplaceDeck(state, entry.Seat,
                entry.Deck ?? throw new SessionSaveException("deck transition has no recipe")),
            "reward" => CampaignRewardRules.Choose(state, definition, entry.Seat, entry.Choice),
            "retry" => CampaignRules.Retry(state),
            "start" => Start(state, definition, entry, verify, proofs),
            "progress" => Progress(state, definition, entry, verify, proofs),
            _ => throw new SessionSaveException("campaign journal contains an unknown transition"),
        };
    }

    private static CampaignState Start(CampaignState state, CampaignDefinition definition, CampaignJournalEntry entry,
        Func<SessionSave, CampaignGameProgress> verify, Dictionary<int, SessionSave> proofs)
    {
        SessionSave proof = entry.Proof ?? throw new SessionSaveException("attempt start lacks proof");
        CampaignProof.RequireSetup(state, proof);
        var scenario = definition.Scenarios[(int)state.Stage];
        if (proof.Setup.Scenario != (state.ExpertScenario ? scenario.Expert : scenario.Standard))
            throw new ReplayDivergenceException("attempt scenario differs from campaign authority");
        _ = verify(proof);
        if (!proofs.TryAdd(state.Attempt, proof))
            throw new ReplayDivergenceException("campaign attempt started twice");
        return CampaignRules.Start(state);
    }

    private static CampaignState Progress(CampaignState state, CampaignDefinition definition,
        CampaignJournalEntry entry, Func<SessionSave, CampaignGameProgress> verify, Dictionary<int, SessionSave> proofs)
    {
        SessionSave proof = entry.Proof ?? throw new SessionSaveException("campaign progress lacks game proof");
        if (!proofs.TryGetValue(state.Attempt, out var previous))
            throw new ReplayDivergenceException("campaign progress precedes attempt start");
        CampaignProof.RequireExtension(previous, proof);
        CampaignGameProgress actual = verify(proof);
        if (CampaignSaveJson.Canonical(actual) != CampaignSaveJson.Canonical(entry.Progress))
            throw new ReplayDivergenceException("campaign effects differ from verified game facts");
        proofs[state.Attempt] = proof;
        state = CampaignProgressRules.Apply(state, actual);
        return actual.Outcome is { } outcome ? CampaignOutcomeRules.Finish(state, definition, outcome) : state;
    }

    private static CampaignGameProgress VerifyProof(SessionSave save, SessionCompatibility compatibility,
        Func<SessionSetup, ReplayOpenedGame> open, Func<Game, CampaignGameProgress> inspect) =>
        inspect(SessionReplay.Verify(save, compatibility, open));
}

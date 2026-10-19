using System.Collections.Immutable;
using Marvel.Rules.Campaign;
using Marvel.Session;
namespace Marvel.Server;

/// <summary>Builds complete proposed aggregates before any durable publication or live-state replacement.</summary>
internal static class CampaignTransactions
{
    internal static CampaignSave Append(CampaignSave save, CampaignState state, CampaignJournalEntry entry) =>
        save with { State = state, Revision = checked(save.Revision + 1), Journal = save.Journal.Add(entry) };

    internal static StoredCampaign Admit(StoredCampaign campaign, StoredSession session, ICampaignGameFactory factory)
    {
        CampaignSave save = campaign.Save;
        CampaignProof.RequireSetup(save.State, session.Save);
        var scenario = factory.CampaignDefinition!.Scenarios[(int)save.State.Stage];
        if (session.Save.Setup.Scenario != (save.State.ExpertScenario ? scenario.Expert : scenario.Standard))
            throw new SessionSaveException("campaign attempt scenario differs from prepared authority");
        save = Append(save, CampaignRules.Start(save.State), new("start", Proof: session.Save));
        save = save with { Attempts = save.Attempts.Add(session.Save) };
        campaign = campaign with { Save = save, Authorities = campaign.Authorities.Add(new(
            session.Save.Session.StorageId, [.. session.Authorities])) };
        return Update(campaign, session, factory);
    }

    internal static StoredCampaign Update(StoredCampaign campaign, StoredSession session, ICampaignGameFactory factory)
    {
        CampaignSave save = campaign.Save;
        int attempt = session.Save.Setup.Campaign!.Attempt;
        var proof = save.Journal.Last(entry => entry.Proof?.Setup.Campaign?.Attempt == attempt).Proof!;
        CampaignProof.RequireExtension(proof, session.Save);
        var verified = SessionReplay.Verify(session.Save, factory.Compatibility, setup => Open(factory, setup));
        CampaignGameProgress progress = factory.InspectCampaign(verified);
        if (save.State.Phase == CampaignPhase.Playing && save.State.Attempt == attempt)
            save = ApplyProgress(save, session.Save, progress, factory.CampaignDefinition!);
        save = save with { Attempts = [.. save.Attempts.Select(old => old.Session.StorageId == session.Save.Session.StorageId ? session.Save : old)] };
        return campaign with { Save = save, Authorities = [.. campaign.Authorities.Select(old =>
            old.StorageId == session.Save.Session.StorageId ? new CampaignAttemptAuthority(old.StorageId, [.. session.Authorities]) : old)] };
    }

    private static CampaignSave ApplyProgress(CampaignSave save, SessionSave proof, CampaignGameProgress progress,
        CampaignDefinition definition)
    {
        var previous = PreviousProgress(save);
        bool empty = progress.Removals.IsEmpty && progress.Obligations.IsEmpty && progress.Outcome is null;
        if (previous is null && empty || CampaignSaveJson.Canonical(previous) == CampaignSaveJson.Canonical(progress))
            return save with { Revision = checked(save.Revision + 1) };
        var state = CampaignProgressRules.Apply(save.State, progress);
        if (progress.Outcome is { } outcome) state = CampaignOutcomeRules.Finish(state, definition, outcome);
        return Append(save, state, new("progress", Proof: proof, Progress: progress));
    }

    private static CampaignGameProgress? PreviousProgress(CampaignSave save) =>
        save.Journal.LastOrDefault(entry => entry.Kind == "progress"
            && entry.Proof?.Setup.Campaign?.Attempt == save.State.Attempt)?.Progress;

    internal static ReplayOpenedGame Open(ICampaignGameFactory factory, SessionSetup setup)
    {
        var opened = factory.Create(new(setup.Scenario, setup.Heroes, setup.ModularSets, setup.Seed,
            setup.PlayerDecks, setup.Campaign));
        return new(opened.Game, opened.SetupEvents);
    }
}

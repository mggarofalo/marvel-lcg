using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Session;
namespace Marvel.Server;

/// <summary>Serializes campaign transitions and game commits through one durable aggregate boundary.</summary>
public sealed class CampaignHost : IEngineEndpoint
{
    private readonly object gate = new();
    private readonly ICampaignStore store;
    internal readonly ICampaignGameFactory Factory;
    internal readonly Dictionary<string, StoredCampaign> Campaigns = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, CampaignDeletion> Deleted = new(StringComparer.Ordinal);
    internal readonly EngineHost Games;
    internal StoredCampaign? Starting { get; private set; }
    internal EngineRequest? StartingRequest { get; private set; }
    private bool uncertain;

    /// <summary>Restores verified campaigns before allowing their attempts to be resumed.</summary>
    public CampaignHost(ICampaignGameFactory factory, ICampaignStore campaigns, ISessionStore sessions,
        Marvel.View.IVisibilityPolicy? visibility = null, Marvel.Observability.OperationalLog? log = null)
    {
        Factory = factory;
        store = campaigns;
        Restore();
        Games = new EngineHost(factory, store: new CampaignSessionStore(sessions, this), visibility: visibility, log: log);
    }

    private void Restore()
    {
        foreach (var deletion in store.Deletions()) Deleted.Add(deletion.StorageId, deletion);
        if (Factory.CampaignDefinition is not { } definition) return;
        foreach (var candidate in store.Load())
        {
            if (candidate.Campaign is not { } campaign) continue;
            try
            {
                _ = CampaignReplay.Verify(campaign.Save, definition, Factory.Compatibility,
                    setup => CampaignTransactions.Open(Factory, setup), Factory.InspectCampaign);
                Campaigns.Add(candidate.StorageId, campaign);
            }
            catch (Exception error) when (error is SessionSaveException or ReplayDivergenceException or SessionCompatibilityException or InvalidOperationException or ArgumentException)
            { /* A rejected archive remains intact and is never replaced with an older generation. */ }
        }
    }

    /// <inheritdoc />
    public EngineResponse Exchange(EngineRequest request)
    {
        lock (gate)
        {
            if (EngineHost.ValidateEnvelope(request) is { } failure) return failure;
            if (uncertain) return Failure(request, "campaign_recovery_required", "The save result is uncertain. Restart to verify durable state.");
            try
            {
                EngineResponse result = request.Operation == EngineProtocol.Campaign
                    ? CampaignCommands.Handle(this, request) : GameRequest(request);
                return uncertain ? Failure(request, "campaign_recovery_required", "Restart to verify the campaign save before continuing.") : result;
            }
            catch (CampaignCommitUncertainException)
            { return Failure(request, "campaign_recovery_required", "Restart to verify the campaign save before continuing."); }
            catch (IOException)
            { return Failure(request, "save_failed", "The campaign change was not committed."); }
            catch (Exception error) when (error is SessionSaveException or ReplayDivergenceException or SessionCompatibilityException or InvalidOperationException or ArgumentException or FormatException)
            { return Failure(request, "campaign_rejected", "The requested campaign transition is not available."); }
        }
    }

    private EngineResponse GameRequest(EngineRequest request)
    {
        if (DeletedGame(request))
            return Failure(request, "campaign_deleted", "This campaign and its saved games have been permanently deleted.");
        if (request.Operation == EngineProtocol.Decks && request.Deck?.Campaign is not null)
            return CampaignDeckRequests.Handle(this, request);
        if (request.Campaign is not null || request.Game?.Campaign is not null)
            return Failure(request, "invalid_request", "Campaign attempts must start from their campaign tracker.");
        var campaign = Campaigns.Values.FirstOrDefault(item => item.Save.Attempts.Any(attempt => attempt.Session.Label == request.GameId));
        if (campaign is not null) return CampaignGameRequest(request, campaign);
        return OrdinaryGameRequest(request);
    }

    private EngineResponse OrdinaryGameRequest(EngineRequest request)
    {
        var response = Games.Exchange(request);
        return response.Setup is { } setup && Factory.CampaignDefinition is { } definition
            ? response with { Setup = setup with { Campaigns = [definition.Product] } } : response;
    }

    private bool DeletedGame(EngineRequest request) =>
        Deleted.Values.Any(item => item.GameIds.Contains(request.GameId, StringComparer.Ordinal))
        || EntryReceipts.Valid(request.Entry) && Deleted.Values.Any(item =>
            item.GameEntryVerifiers.Contains(EntryReceipts.Verifier(request.Entry!), StringComparer.Ordinal));

    private EngineResponse CampaignGameRequest(EngineRequest request, StoredCampaign campaign)
    {
        if (campaign.Archived)
            return Failure(request, "campaign_archived", "Restore this campaign in Campaigns before returning to its game.");
        if (request.Operation != EngineProtocol.Attach && !Games.authority.TrySession(request, out _, out _))
            return Games.Exchange(request);
        if (!CampaignGameHistory.Allowed(campaign, request))
            return Failure(request, "campaign_history_protected", "That history crosses a committed campaign consequence.");
        // Leaving the table releases the client view; the campaign owns the durable attempt lifetime.
        if (request.Operation == EngineProtocol.Close)
        {
            var authorized = Games.Exchange(EngineRequest.SyncGame(request.RequestId, request.GameId, request.Capability ?? ""));
            return authorized.Error is null ? Success(request) : authorized;
        }
        var response = Games.Exchange(request);
        return CampaignGameHistory.Project(Campaigns[campaign.Save.Identity.StorageId], response);
    }

    internal string Publish(StoredCampaign campaign)
    {
        try
        {
            string generation = store.Commit(campaign);
            Campaigns[campaign.Save.Identity.StorageId] = campaign;
            return generation;
        }
        catch (CampaignCommitUncertainException) { uncertain = true; throw; }
    }

    internal void RequireRecovery(Exception failure)
    {
        uncertain = true;
        throw new CampaignCommitUncertainException(failure);
    }

    internal void Delete(StoredCampaign campaign, EngineRequest request)
    {
        var deletion = new CampaignDeletion(campaign.Save.Identity.StorageId, campaign.OwnerVerifier,
            request.RequestId, EntryReceipts.Fingerprint(request), campaign.Entry.Verifier,
            campaign.Save.Attempts.Select(attempt => attempt.Session.Label).ToArray(),
            campaign.Authorities.SelectMany(attempt => attempt.Authorities).Select(authority => authority.Entry?.Verifier)
                .OfType<string>().Distinct(StringComparer.Ordinal).ToArray());
        try { store.Delete(deletion); }
        catch (CampaignCommitUncertainException) { uncertain = true; throw; }
        Deleted[deletion.StorageId] = deletion;
        Campaigns.Remove(deletion.StorageId);
        Games.authority.RemoveGames(deletion.GameIds);
    }

    internal EngineResponse Start(EngineRequest request, StoredCampaign campaign)
    {
        _ = CampaignRules.Setup(campaign.Save.State);
        CampaignCommands.RequireDecks(Factory, campaign.Save.State);
        Starting = campaign;
        StartingRequest = request;
        try
        {
            var state = campaign.Save.State;
            var scenario = Factory.CampaignDefinition!.Scenarios[(int)state.Stage];
            var spec = new GameSpecification(state.ExpertScenario ? scenario.Expert : scenario.Standard,
                state.Players.Where(player => player.Participating).Select(player => player.Hero).ToArray(), null,
                CampaignSeed.ForAttempt(state.Seed, state.Stage, state.Attempt),
                state.Players.Where(player => player.Participating).Select(player => player.Deck).ToArray(), CampaignRules.Setup(state));
            var inner = EngineRequest.OpenGame(request.RequestId,
                campaign.Save.Identity.StorageId + ":" + state.Attempt, spec) with { Entry = request.Entry };
            var response = Games.Exchange(inner);
            return response with { Campaign = Describe(Campaigns[campaign.Save.Identity.StorageId]) };
        }
        finally { Starting = null; StartingRequest = null; }
    }

    internal CampaignDescriptor Describe(StoredCampaign campaign)
    {
        var save = campaign.Save;
        var active = save.State.Phase == CampaignPhase.Playing
            ? save.Attempts.LastOrDefault(item => item.Setup.Campaign!.Attempt == save.State.Attempt)?.Session.Label : null;
        return new(save.Identity.StorageId, save.Revision, save.State,
            save.State.Phase == CampaignPhase.Rewards ? CampaignRewardRules.Choice(save.State, Factory.CampaignDefinition!) : null, active)
        { Name = save.Identity.Label, Archived = campaign.Archived, PreparationProblems = PreparationProblems(save.State),
            RemovedCards = [.. Factory.CampaignDefinition!.Tech.Where(card => save.State.RemovedCardKeys.Contains(card.Key))] };
    }

    private IReadOnlyList<string> PreparationProblems(CampaignState state)
    {
        if (state.Phase != CampaignPhase.Preparing) return [];
        return [.. CampaignPreparationRules.Problems(state), .. state.Players.SelectMany((player, seat) =>
            player.Participating ? Factory.AssessCampaignDeck(player.Hero, player.Deck.Cards, state.BannedAllyFaces)
                .Diagnostics.Select(diagnostic => $"Player {seat + 1}: {diagnostic.Message}") : [])];
    }

    internal static EngineResponse Failure(EngineRequest request, string code, string message) =>
        new(EngineProtocol.Version, request.RequestId, request.GameId, null, null, [], Error: new(code, message, code == "campaign_recovery_required"
            ? EngineFailureDisposition.CommitUncertain : EngineFailureDisposition.Rejected));
    internal static EngineResponse Success(EngineRequest request) =>
        new(EngineProtocol.Version, request.RequestId, request.GameId, null, null, []);
}

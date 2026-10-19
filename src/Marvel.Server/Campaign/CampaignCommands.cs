using System.Collections.Immutable;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Session;
namespace Marvel.Server;

/// <summary>Authorizes campaign ownership and translates owner intentions into rules-owned transitions.</summary>
internal static class CampaignCommands
{
    internal static EngineResponse Handle(CampaignHost host, EngineRequest request)
    {
        if (request.Version != EngineProtocol.Version || request.Campaign is not { } query)
            return CampaignHost.Failure(request, "invalid_request", "A current campaign request is required.");
        if (host.Factory.CampaignDefinition is null)
            return CampaignHost.Failure(request, "campaign_unavailable", "This campaign is not admitted by the installed content.");
        if (query.Action == "create") return Create(host, request, query);
        if (!host.Campaigns.TryGetValue(request.GameId, out var campaign) || request.Capability is null
            || campaign.OwnerVerifier != SessionAuthorityRegistry.Verifier(request.Capability))
            return CampaignHost.Failure(request, "campaign_not_found", "The campaign owner capability is not valid.");
        return Authorized(host, request, query, campaign);
    }

    private static EngineResponse Authorized(CampaignHost host, EngineRequest request, CampaignQuery query, StoredCampaign campaign)
    {
        if (query.Action == "sync") return CampaignHost.Success(request) with { Campaign = host.Describe(campaign) };
        var receipt = campaign.Receipts.FirstOrDefault(item => item.RequestId == request.RequestId);
        if (receipt is not null) return Repeat(host, request, campaign, receipt);
        if (request.ExpectedRevision != campaign.Save.Revision)
            return CampaignHost.Failure(request, "revision_conflict", "Refresh the campaign before applying this choice.")
                with { Campaign = host.Describe(campaign) };
        if (query.Action == "start")
        {
            if (!EntryReceipts.Valid(request.Entry)) return CampaignHost.Failure(request, "invalid_request", "Starting requires a retained entry claim.");
            return host.Start(request, campaign);
        }
        var proposed = Change(host.Factory, campaign, query);
        proposed = AddReceipt(proposed, request);
        host.Publish(proposed);
        return CampaignHost.Success(request) with { Campaign = host.Describe(proposed) };
    }

    private static EngineResponse Create(CampaignHost host, EngineRequest request, CampaignQuery query)
    {
        if (!EntryReceipts.Valid(request.Entry))
            return CampaignHost.Failure(request, "invalid_request", "Creating requires a retained entry claim.");
        string verifier = EntryReceipts.Verifier(request.Entry!);
        var existing = host.Campaigns.Values.FirstOrDefault(item => item.Entry.Verifier == verifier);
        if (existing is not null)
            return existing.Entry.Fingerprint == EntryReceipts.Fingerprint(request)
                ? EntryReceipts.Recover(request, existing.Entry) with { RequestId = request.RequestId, Campaign = host.Describe(existing) }
                : CampaignHost.Failure(request, "entry_conflict", "This entry claim belongs to a different campaign request.");
        if (query.Product != host.Factory.CampaignDefinition!.Product || query.Heroes.IsDefaultOrEmpty
            || query.Decks.IsDefault || query.Heroes.Length != query.Decks.Length)
            return CampaignHost.Failure(request, "invalid_request", "A supported campaign and complete player recipes are required.");
        var roster = query.Heroes.Select((hero, seat) => new CampaignRosterEntry(hero, query.Decks[seat], host.Factory.CampaignBaseHitPoints(hero))).ToImmutableArray();
        var save = CampaignSave.Create(host.Factory.Compatibility,
            new(EngineHostLifecycle.NewStorageId(), request.GameId, "active"),
            new(query.Product, query.Expert, query.Seed, roster), host.Factory.CampaignDefinition);
        RequireDecks(host.Factory, save.State);
        string capability = new CryptographicCapabilityIssuer().Issue();
        var response = CampaignHost.Success(request) with { Capability = capability,
            Campaign = new(save.Identity.StorageId, 0, save.State, null, null) };
        var campaign = new StoredCampaign(save, SessionAuthorityRegistry.Verifier(capability),
            EntryReceipts.Protect(request, response), [], []);
        host.Publish(campaign);
        return response;
    }

    private static StoredCampaign Change(ICampaignGameFactory factory, StoredCampaign campaign, CampaignQuery query)
    {
        var save = campaign.Save;
        CampaignState state;
        CampaignJournalEntry entry;
        switch (query.Action)
        {
            case "participation": state = CampaignParticipationRules.Select(save.State, query.Seat, query.Participating);
                entry = new("participation", Seat: query.Seat) { Participating = query.Participating }; break;
            case "mode": state = CampaignRules.SelectScenarioMode(save.State, query.Expert); entry = new("mode", Expert: query.Expert); break;
            case "retry": state = CampaignRules.Retry(save.State); entry = new("retry"); break;
            case "reward": state = CampaignRewardRules.Choose(save.State, factory.CampaignDefinition!, query.Seat, query.Choice);
                entry = new("reward", Seat: query.Seat, Choice: query.Choice); break;
            case "deck":
                state = CampaignRules.ReplaceDeck(save.State, query.Seat, query.Deck ?? throw new ArgumentException("deck required"));
                RequireDeck(factory, state, query.Seat); entry = new("deck", Seat: query.Seat, Deck: query.Deck); break;
            default: throw new ArgumentException("unknown campaign command");
        }
        return campaign with { Save = CampaignTransactions.Append(save, state, entry) };
    }

    internal static void RequireDecks(ICampaignGameFactory factory, CampaignState state)
    {
        foreach (var player in state.Players.Where(player => player.Participating))
            if (!factory.AssessCampaignDeck(player.Hero, player.Deck.Cards, state.BannedAllyFaces).IsValid)
                throw new ArgumentException("campaign deck is not legal");
    }

    private static void RequireDeck(ICampaignGameFactory factory, CampaignState state, int seat)
    {
        var player = state.Players[seat];
        if (!factory.AssessCampaignDeck(player.Hero, player.Deck.Cards, state.BannedAllyFaces).IsValid)
            throw new ArgumentException("campaign deck is not legal");
    }

    internal static StoredCampaign AddReceipt(StoredCampaign campaign, EngineRequest request, string? gameId = null) =>
        campaign with { Receipts = campaign.Receipts.Add(new(request.RequestId,
            EntryReceipts.Fingerprint(request), campaign.Save.Revision, gameId)) };

    private static EngineResponse Repeat(CampaignHost host, EngineRequest request, StoredCampaign campaign, CampaignReceipt receipt)
    {
        if (receipt.Fingerprint != EntryReceipts.Fingerprint(request))
            return CampaignHost.Failure(request, "request_conflict", "This command id already names another campaign choice.");
        if (receipt.GameId is null) return CampaignHost.Success(request) with { Campaign = host.Describe(campaign) };
        var attempt = campaign.Save.Attempts.Single(item => item.Session.Label == receipt.GameId);
        var setup = attempt.Setup;
        var inner = EngineRequest.OpenGame(request.RequestId, receipt.GameId,
            new(setup.Scenario, setup.Heroes, setup.ModularSets, setup.Seed, setup.PlayerDecks, setup.Campaign)) with { Entry = request.Entry };
        return host.Games.Exchange(inner) with { Campaign = host.Describe(campaign) };
    }
}

using Marvel.Protocol;
namespace Marvel.Server;

/// <summary>Authorizes campaign construction exceptions without weakening ordinary deck assessment.</summary>
internal static class CampaignDeckRequests
{
    internal static EngineResponse Handle(CampaignHost host, EngineRequest request)
    {
        var draft = request.Deck!;
        var access = draft.Campaign!;
        if (!host.Campaigns.TryGetValue(access.CampaignId, out var campaign)
            || campaign.OwnerVerifier != SessionAuthorityRegistry.Verifier(access.Capability))
            return CampaignHost.Failure(request, "campaign_not_found", "The campaign deck authority is not valid.");
        if (draft.Cards is not { Count: <= 500 } || draft.Cards.Any(face => face is not { Length: <= 80 })
            || !campaign.Save.State.Players.Any(player => player.Hero == draft.Hero))
            return CampaignHost.Failure(request, "invalid_request", "The campaign draft exceeds its authorized roster or input bounds.");
        var assessment = host.Factory.AssessCampaignDeck(draft.Hero, draft.Cards, campaign.Save.State.BannedAllyFaces);
        return new(EngineProtocol.Version, request.RequestId, "", null, null, [])
        {
            Decks = new(null, new(assessment.Size, assessment.Diagnostics)
            {
                CanExtend = assessment.CanExtend,
                Quantities = draft.IncludeQuantities ? host.Factory.CampaignDeckQuantities(draft.Hero, draft.Cards,
                    campaign.Save.State.BannedAllyFaces) : new Dictionary<string, int[]>(),
            }),
        };
    }
}

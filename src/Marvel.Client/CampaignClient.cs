using Marvel.Protocol;
using static Marvel.Client.ClientResponseValidation;
namespace Marvel.Client;

/// <summary>Retains exact mutation requests across uncertain transport results and validates campaign entries.</summary>
internal static class CampaignClient
{
    internal static async ValueTask<ClientCampaignResult> ExchangeAsync(LocalGameClient client,
        EngineRequest request, CancellationToken cancellationToken)
    {
        if (request.Operation != EngineProtocol.Campaign || request.Campaign is null)
            throw new ArgumentException("a campaign intention is required", nameof(request));
        try
        {
            var response = await client.transport.ExchangeAsync(request, cancellationToken).ConfigureAwait(false);
            return Validate(client, request, response);
        }
        catch (Exception) { return Uncertain(request, "The response was lost. Retry this same campaign choice."); }
    }

    private static ClientCampaignResult Validate(LocalGameClient client, EngineRequest request, EngineResponse response)
    {
            if (response.Version != EngineProtocol.Version || response.RequestId != request.RequestId)
                return Uncertain(request, "The campaign service returned an unrecognized response.");
            if (response.Error is { } error)
                return new(response, null, Error(error.Code, error.Message),
                    error.Disposition == EngineFailureDisposition.CommitUncertain ? request : null);
            if (!Complete(response.Campaign, request)) return Uncertain(request, "The campaign service returned incomplete state.");
            return request.Campaign!.Action == "start" ? Enter(client, request, response) : Ordinary(request, response);
    }

    private static ClientCampaignResult Ordinary(EngineRequest request, EngineResponse response)
    {
        if (response.GameId != request.GameId || response.World is not null || response.Prompt is not null)
            return Uncertain(request, "The campaign response named another operation.");
        if (request.Campaign!.Action == "create" && !ValidIdentifier(response.Capability))
            return Uncertain(request, "The campaign owner authority was missing.");
        return new(response, null, null, null);

    }

    private static ClientCampaignResult Enter(LocalGameClient client, EngineRequest request, EngineResponse response)
    {
        if (!HasCompleteGameplayResponse(response, allowWaiting: true) || !ValidIdentifier(response.Capability)
            || !CompleteInvitations(response.Invitations)) return Uncertain(request, "The campaign game view was incomplete.");
        var session = new ClientSession(response.GameId, response.Capability!);
        client.RememberRevision(session, response);
        return new(response, new(session, Sanitize(response), response.Invitations ?? [], null), null, null);
    }

    private static bool Complete(CampaignDescriptor? campaign, EngineRequest request)
    {
        return campaign is { Revision: >= 0, State: not null } && ValidIdentifier(campaign.Id)
        && (request.Campaign!.Action == "create" || campaign.Id == request.GameId)
        && CompleteState(campaign.State);
    }

    private static bool CompleteState(Marvel.Rules.Campaign.CampaignState state)
    {
        return state.Product is { Length: > 0 and <= 80 } && !state.Players.IsDefaultOrEmpty && state.Players.Length <= 4
        && !state.BannedAllyFaces.IsDefault && !state.RemovedCardKeys.IsDefault
        && Enum.IsDefined(state.Phase) && Enum.IsDefined(state.Stage)
        && state.Players.All(CompletePlayer);
    }

    private static bool CompletePlayer(Marvel.Rules.Campaign.CampaignPlayerState player)
    {
        return player is { Hero.Length: > 0, Deck.Cards: not null } && !player.Cards.IsDefault
        && !player.AvailableObligations.IsDefault;
    }

    private static ClientCampaignResult Uncertain(EngineRequest request, string message) =>
        new(null, null, Error("campaign_uncertain", message), request);
}

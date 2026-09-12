using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Client;

/// <summary>Validates protocol responses before they enter recoverable client state.</summary>
internal static class ClientResponseValidation
{
    internal static bool HasRequestPrefix(string requestId, string prefix) =>
        string.Equals(requestId, prefix, StringComparison.Ordinal)
        || requestId.StartsWith(prefix + "-", StringComparison.Ordinal);

    internal static ClientResolutionResult UnavailableResolution(
        ClientMutationDisposition mutation) =>
        new(
            Response: null,
            Error(
                "session_unavailable",
                "This game session is unavailable. Return to the connection screen."),
            mutation,
            ClientSessionDisposition.Unavailable);

    internal static ClientSynchronizationResult UnavailableSynchronization() =>
        new(
            Response: null,
            Error(
                "session_unavailable",
                "This game session is unavailable. Return to the connection screen."),
            ClientSessionDisposition.Unavailable);

    internal static ClientSynchronizationResult SynchronizationFailed(
        string code,
        string message) =>
        new(Response: null, Error(code, message), ClientSessionDisposition.Active);

    internal static ClientStartupError Error(string code, string message) =>
        new(Bounded(code), Bounded(message));

    internal static ClientStartupError? IdentifierError(
        string? value,
        string code,
        string message) =>
        !ValidIdentifier(value)
            ? Error(code, message)
            : null;

    internal static bool ValidIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= EngineProtocol.MaximumIdentifierLength;

    internal static ClientStartupError? SessionError(ClientSession session) =>
        IdentifierError(
            session.GameId,
            "invalid_session",
            "The saved game session is incomplete.")
        ?? IdentifierError(
            session.Capability,
            "invalid_session",
            "The saved game session is incomplete.");

    internal static EngineResponse Sanitize(EngineResponse response) =>
        response with { Capability = null, Invitations = null };

    internal static bool TryResolveModularSets(
        SetupChoices available,
        GameSetupSelection selection,
        out IReadOnlyList<string>? modularSets)
    {
        if (selection.ModularKeys is null)
        {
            modularSets = null;
            return false;
        }

        switch (selection.Modular)
        {
            case ModularConfiguration.Recommended when selection.ModularKeys.Count == 0:
                modularSets = null;
                return true;
            case ModularConfiguration.None when selection.ModularKeys.Count == 0:
                modularSets = [];
                return true;
            case ModularConfiguration.Selected when selection.ModularKeys.Count > 0:
                var selected = selection.ModularKeys.ToHashSet(StringComparer.Ordinal);
                if (selected.Count != selection.ModularKeys.Count
                    || selected.Any(key =>
                        !available.ModularSets.Any(set => set.Key == key)))
                {
                    break;
                }

                // Catalog order is the client wire choice. It keeps the game
                // specification stable regardless of click order in a multi-select menu.
                modularSets = available.ModularSets
                    .Where(set => selected.Contains(set.Key))
                    .Select(set => set.Key)
                    .ToArray();
                return true;
        }

        modularSets = null;
        return false;
    }

    internal static bool ValidHeroSelection(
        SetupChoices available,
        IReadOnlyList<string>? heroKeys) =>
        heroKeys is { Count: 1 or 2 }
        && heroKeys.All(key =>
            !string.IsNullOrWhiteSpace(key)
            && available.Heroes.Any(hero => hero.Key == key))
        && heroKeys.Distinct(StringComparer.Ordinal).Count() == heroKeys.Count;

    internal static bool CompleteInvitations(IReadOnlyList<SeatInvitation>? invitations) =>
        invitations is null
        || invitations.All(invitation =>
            invitation is not null
            && invitation.Seat >= 0
            && ValidIdentifier(invitation.Invitation))
        && invitations.Select(invitation => invitation.Seat).Distinct().Count()
            == invitations.Count
        && invitations.Select(invitation => invitation.Invitation)
            .Distinct(StringComparer.Ordinal).Count() == invitations.Count;

    internal static ClientStartupError? EnvelopeError(
        EngineResponse response,
        string requestId,
        string gameId)
    {
        if (response.Version != EngineProtocol.Version)
        {
            return Error(
                "unsupported_version",
                "The game service uses an unsupported protocol version.");
        }

        return response.RequestId == requestId && response.GameId == gameId
            ? null
            : Error(
                "invalid_response",
                "The game service returned a response for a different request or game.");
    }

    internal static bool HasCompleteBoard(WorldDescriptor? world)
    {
        if (world?.Players is null || world.Areas is null || world.GameAreas is null)
            return false;
        return world.Players.All(CompletePlayer)
            && world.GameAreas.All(CompleteGameArea)
            && world.Areas.All(CompleteArea);
    }

    private static bool CompletePlayer(PlayerDescriptor player) =>
        player is not null && player.Name is not null;

    private static bool CompleteGameArea(GameAreaDescriptor area) =>
        area is not null && area.PlayAreas is not null;

    private static bool CompleteArea(AreaDescriptor area) =>
        area is not null && area.Zone is not null && area.Cards is not null
        && area.Removed is not null && area.Cards.Concat(area.Removed).All(CompleteCard);

    private static bool CompleteCard(CardDescriptor card) => card is not null
        && (card.Face is null || CompleteFace(card.Face));

    private static bool CompleteFace(CardFaceDescriptor face) =>
        face.Id is not null && face.Title is not null && face.Subtitle is not null
        && face.Fields is not null;

    internal static bool HasCompleteGameplayResponse(
        EngineResponse response,
        bool allowWaiting = false) =>
        response.Revision >= 0
        && HasCompleteEvents(response.Events)
        && HasCompleteBoard(response.World)
        && HasCompleteHistory(response.History)
        && Enum.IsDefined(response.World!.Outcome)
        && (response.World.Outcome == Outcome.Unfinished
            ? (allowWaiting && response.Prompt is null) || HasCompletePrompt(response.Prompt)
            : response.Prompt is null);

    internal static bool HasCompleteSynchronizationResponse(EngineResponse response) =>
        response.Events is { Count: 0 }
        && HasCompleteGameplayResponse(response, allowWaiting: true);

    internal static bool HasCompleteHistory(HistoryDescriptor? history)
    {
        if (!HasHistoryParts(history)) return false;
        if (history!.Entries.Any(entry => !CompleteEntry(entry, history.Cursor))) return false;
        if (history.ActionOpen && (history.Undo.Count > 0 || history.Redo.Count > 0)) return false;

        int[] entryCursors = [.. history.Entries.Select(entry => entry.Cursor)];
        return CompleteHistoryCursors(history, entryCursors);
    }

    private static bool HasHistoryParts(HistoryDescriptor? history) =>
        history is not null && history.Cursor >= 0 && history.Undo is not null
        && history.Redo is not null && history.Entries is not null;

    private static bool CompleteEntry(HistoryEntryDescriptor entry, int cursor) =>
        entry is not null && entry.Cursor >= 0 && entry.Cursor < cursor
        && !string.IsNullOrWhiteSpace(entry.Summary) && entry.Details is not null
        && !entry.Details.Any(string.IsNullOrWhiteSpace);

    private static bool CompleteHistoryCursors(HistoryDescriptor history, int[] cursors) =>
        cursors.SequenceEqual(cursors.Order())
        && cursors.Distinct().Count() == cursors.Length
        && history.Undo.All(target => ValidUndo(target, history.Cursor, cursors))
        && history.Redo.All(target => target > history.Cursor)
        && history.Undo.Distinct().Count() == history.Undo.Count
        && history.Redo.Distinct().Count() == history.Redo.Count;

    private static bool ValidUndo(int target, int cursor, int[] entries) =>
        target >= 0 && target < cursor && entries.Contains(target);

    internal static bool HasCompleteEvents(IReadOnlyList<GameEvent>? events) =>
        events is not null && events.All(CompleteEvent);

    private static bool CompleteEvent(GameEvent happened) =>
        happened is not null && happened.Trigger is not null && happened.Verb is not null
        && CompleteEventPayload(happened);

    private static bool CompleteEventPayload(GameEvent happened) => happened switch
            {
                CardsCreated created => Complete(created.Area)
                    && created.Cards is not null
                    && created.Cards.All(card => card.Card is not null),
                CardsMoved moved => Complete(moved.From)
                    && Complete(moved.To)
                    && moved.Cards is not null,
                AreaReordered reordered => Complete(reordered.Area)
                    && reordered.Order is not null,
                CardFormChanged changed => changed.From is not null
                    && changed.To is not null,
                CardsFlipped flipped => flipped.Cards is not null,
                CardAttached => true,
                CardDetached => true,
                ControlChanged => true,
                PlayAreaJoined => true,
                PlayAreaDetached => true,
                FieldSet set => set.Field is not null,
                _ => false,
            };

    internal static bool Complete(AreaRef area) =>
        area.Zone is not null && area.Id is not null;

    internal static bool Complete(EngineError error) =>
        error.Code is not null && error.Message is not null;

    internal static bool HasCompletePrompt(Prompt? prompt) =>
        prompt?.Trigger is not null && prompt.Label is not null
        // A cancellable turn prompt may have no legal actions left. Passing is
        // still a complete decision, so the empty option list is not a broken
        // response and must remain synchronizable after the last action.
        && prompt.Affordances is not null
        && (prompt.Cancellable || prompt.Affordances.Count > 0)
        && prompt.Affordances.All(CompleteAffordance);

    private static bool CompleteAffordance(Affordance option) =>
        option is not null && option.Verb is not null && option.Label is not null
        && CompleteTargets(option.Targets) && CompleteCosts(option.Costs);

    private static bool CompleteTargets(TargetRequest? targets) => targets is null
        || targets.Legal is not null
        && (targets.Groups is null || targets.Groups.All(group => group is not null))
        && (targets.MustIncludeTraits is null
            || targets.MustIncludeTraits.All(trait => trait is not null));

    private static bool CompleteCosts(IReadOnlyList<CostOption>? costs) =>
        costs is null || costs.All(CompleteCost);

    private static bool CompleteCost(CostOption cost) =>
        cost is not null && cost.Cost is not null && cost.OrCost is not null
        && CompleteRules(cost) && CompletePaymentInputs(cost)
        && (cost.Components is null || cost.Components.All(CompleteComponent));

    private static bool CompleteRules(CostOption cost) =>
        (cost.Rule is null || cost.Rule.All(rule => rule is not null))
        && (cost.OrRule is null || cost.OrRule.All(rule => rule is not null));

    private static bool CompletePaymentInputs(CostOption cost) =>
        (cost.Sources is null || cost.Sources.All(source => source.Generates is not null))
        && (cost.Variables is null || cost.Variables.All(variable => variable.Name is not null));

    private static bool CompleteComponent(ResourceCost component) =>
        component is not null && component.Cost is not null
        && (component.Rule is null || component.Rule.All(rule => rule is not null));

    internal static string Bounded(string value) =>
        value.Length <= LocalGameClient.MaximumDisplayedErrorLength
            ? value
            : value[..LocalGameClient.MaximumDisplayedErrorLength];
}

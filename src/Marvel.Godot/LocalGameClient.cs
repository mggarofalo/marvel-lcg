using System.Globalization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The client-owned label for the one local table.</summary>
public static class LocalGameSession
{
    /// <summary>The opaque game id sent through either transport.</summary>
    public const string GameId = "local-core-game";
}

/// <summary>A bounded product-level failure suitable for display.</summary>
public sealed record ClientStartupError(string Code, string Message);

/// <summary>A composed client, or why its engine connection could not be configured.</summary>
public sealed record LocalClientConnection(
    LocalGameClient? Client,
    ClientStartupError? Error)
{
    /// <summary>Whether the configured engine transport is available.</summary>
    public bool Succeeded => Client is not null && Error is null;
}

/// <summary>The result of discovering the authored setup surface.</summary>
public sealed record ClientSetupResult(
    SetupChoices? Choices,
    ClientStartupError? Error)
{
    /// <summary>Whether complete choices were returned.</summary>
    public bool Succeeded => Choices is not null && Error is null;
}

/// <summary>The result of trying to open one selected game.</summary>
public sealed record ClientStartupResult(
    EngineResponse? Response,
    ClientStartupError? Error)
{
    /// <summary>Whether a complete initial game view was returned.</summary>
    public bool Succeeded => Response is not null && Error is null;
}

/// <summary>A resolved decision, optionally paired with a recovered current view.</summary>
public sealed record ClientResolutionResult(
    EngineResponse? Response,
    ClientStartupError? Error)
{
    /// <summary>Whether the submitted decision was accepted.</summary>
    public bool Succeeded => Response is not null && Error is null;

    /// <summary>Whether an authoritative view is available for rendering.</summary>
    public bool HasAuthoritativeView => Response is not null;
}

/// <summary>How the player wants to fill the scenario's modular-set slot.</summary>
public enum ModularConfiguration
{
    /// <summary>Use the scenario's authored recommendation.</summary>
    Recommended,

    /// <summary>Deliberately include no modular set.</summary>
    None,

    /// <summary>Use one explicitly selected authored modular set.</summary>
    Selected,
}

/// <summary>Raw values selected by the setup screen.</summary>
public sealed record GameSetupSelection(
    string HeroKey,
    string ScenarioKey,
    ModularConfiguration Modular,
    string? ModularKey,
    string Seed);

/// <summary>Uses the engine protocol for both local and remote game setup.</summary>
public sealed class LocalGameClient
{
    private const int MaximumDisplayedErrorLength = 240;
    private const string OpenRequestId = "local-open";
    private const string RecoverRequestId = "local-recover";
    private const string ResolveRequestId = "local-resolve";
    private const string SetupRequestId = "local-setup";
    private readonly IEngineTransport transport;

    /// <summary>Creates an app client over an embedded or remote transport.</summary>
    public LocalGameClient(IEngineTransport transport) =>
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));

    /// <summary>Composes the local host while keeping dataset access at its boundary.</summary>
    public static LocalClientConnection ConnectLocal(string dataRoot)
    {
        try
        {
            var host = new EngineHost(DatasetGameFactory.Load(dataRoot));
            return new LocalClientConnection(
                new LocalGameClient(new InProcessTransport(host)), Error: null);
        }
        catch (Exception)
        {
            return new LocalClientConnection(
                Client: null,
                Error(
                    "content_unavailable",
                    "The local game could not load its committed Core Set content."));
        }
    }

    /// <summary>Reads the exact product choices accepted by the host.</summary>
    public async ValueTask<ClientSetupResult> ReadSetupAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.ReadSetup(SetupRequestId), cancellationToken)
                .ConfigureAwait(false);
            ClientStartupError? envelope = EnvelopeError(
                response, SetupRequestId, string.Empty);
            if (envelope is not null)
            {
                return new ClientSetupResult(Choices: null, envelope);
            }

            if (response.Error is not null)
            {
                if (!Complete(response.Error))
                {
                    return SetupFailed(
                        "invalid_response",
                        "The game service returned an incomplete error.");
                }

                return SetupFailed(response.Error.Code, response.Error.Message);
            }

            SetupChoices? choices = response.Setup;
            if (choices?.Heroes is not { Count: > 0 }
                || choices.Scenarios is not { Count: > 0 }
                || choices.ModularSets is not { Count: > 0 }
                || choices.Scenarios
                    .SelectMany(scenario => scenario.RecommendedModularSets)
                    .Any(recommended => !choices.ModularSets.Any(
                        modular => modular.Key == recommended))
                || !HasCompleteEvents(response.Events))
            {
                return SetupFailed(
                    "invalid_response",
                    "The game service did not return complete setup choices.");
            }

            return new ClientSetupResult(choices, Error: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return SetupFailed(
                "transport_unavailable",
                "The game service could not be reached. Try loading setup again.");
        }
    }

    /// <summary>Validates a screen selection and sends exactly one open request.</summary>
    public ValueTask<ClientStartupResult> OpenAsync(
        SetupChoices available,
        GameSetupSelection selection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(available);
        ArgumentNullException.ThrowIfNull(selection);

        if (!available.Heroes.Any(hero => hero.Key == selection.HeroKey)
            || !available.Scenarios.Any(scenario => scenario.Key == selection.ScenarioKey))
        {
            return ValueTask.FromResult(Failed(
                "invalid_selection",
                "Choose a hero and scenario offered by this game service."));
        }

        IReadOnlyList<string>? modularSets;
        switch (selection.Modular)
        {
            case ModularConfiguration.Recommended:
                modularSets = null;
                break;
            case ModularConfiguration.None:
                modularSets = [];
                break;
            case ModularConfiguration.Selected
                when selection.ModularKey is not null
                     && available.ModularSets.Any(set => set.Key == selection.ModularKey):
                modularSets = [selection.ModularKey];
                break;
            default:
                return ValueTask.FromResult(Failed(
                    "invalid_selection",
                    "Choose a modular-set option offered by this game service."));
        }

        if (!uint.TryParse(
                selection.Seed,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint seed))
        {
            return ValueTask.FromResult(Failed(
                "invalid_seed",
                "Enter a whole-number seed from 0 through 4294967295."));
        }

        return OpenAsync(
            new GameSpecification(
                selection.ScenarioKey,
                [selection.HeroKey],
                modularSets,
                seed),
            cancellationToken);
    }

    /// <summary>Sends the canonical open request through the configured transport.</summary>
    public async ValueTask<ClientStartupResult> OpenAsync(
        GameSpecification specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.OpenGame(
                    OpenRequestId, LocalGameSession.GameId, specification),
                cancellationToken).ConfigureAwait(false);

            ClientStartupError? envelope = EnvelopeError(
                response, OpenRequestId, LocalGameSession.GameId);
            if (envelope is not null)
            {
                return new ClientStartupResult(Response: null, envelope);
            }

            if (response.Error is not null)
            {
                if (!Complete(response.Error))
                {
                    return Failed(
                        "invalid_response",
                        "The game service returned an incomplete error.");
                }

                return Failed(response.Error.Code, response.Error.Message);
            }

            if (!HasCompleteGameplayResponse(response)
                || response.Prompt is null
                || string.IsNullOrWhiteSpace(response.Capability))
            {
                return Failed(
                    "invalid_response",
                    "The engine did not return a complete initial game view.");
            }

            return new ClientStartupResult(response, Error: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failed(
                "transport_unavailable",
                "The game service could not be reached. Try starting the game again.");
        }
    }

    /// <summary>
    /// Submits one answer and, after a rejection or uncertain response, reads
    /// the current view without ever repeating the mutation.
    /// </summary>
    public async ValueTask<ClientResolutionResult> ResolveAsync(
        string capability,
        EngineDecision decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);
        ArgumentNullException.ThrowIfNull(decision);
        cancellationToken.ThrowIfCancellationRequested();
        ClientStartupError? failure = null;
        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.ResolveGame(
                    ResolveRequestId, LocalGameSession.GameId, capability, decision),
                cancellationToken).ConfigureAwait(false);
            ClientStartupError? envelope = EnvelopeError(
                response, ResolveRequestId, LocalGameSession.GameId);
            if (envelope is not null)
            {
                failure = envelope;
            }
            else if (response.Error is null)
            {
                if (HasCompleteGameplayResponse(response))
                {
                    return new ClientResolutionResult(response, Error: null);
                }

                failure = Error(
                    "invalid_response",
                    "The game service did not return a complete current table.");
            }
            else
            {
                failure = Complete(response.Error)
                    ? Error(response.Error.Code, response.Error.Message)
                    : Error(
                        "invalid_response",
                        "The game service returned an incomplete error.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            failure = Error(
                "transport_unavailable",
                "The decision response was lost. The client will read the current table without repeating it.");
        }

        return await RecoverCurrentViewAsync(capability, failure)
            .ConfigureAwait(false);
    }

    private static ClientSetupResult SetupFailed(string code, string message) =>
        new(Choices: null, Error(code, message));

    private static ClientStartupResult Failed(string code, string message) =>
        new(Response: null, Error(code, message));

    private async ValueTask<ClientResolutionResult> RecoverCurrentViewAsync(
        string capability,
        ClientStartupError failure)
    {
        try
        {
            EngineResponse synchronized = await transport.ExchangeAsync(
                EngineRequest.SyncGame(
                    RecoverRequestId, LocalGameSession.GameId, capability),
                CancellationToken.None).ConfigureAwait(false);
            return EnvelopeError(
                    synchronized, RecoverRequestId, LocalGameSession.GameId) is null
                && synchronized.Error is null
                && HasCompleteGameplayResponse(synchronized)
                ? new ClientResolutionResult(synchronized, failure)
                : new ClientResolutionResult(Response: null, failure);
        }
        catch (Exception)
        {
            return new ClientResolutionResult(Response: null, failure);
        }
    }

    private static ClientStartupError Error(string code, string message) =>
        new(Bounded(code), Bounded(message));

    private static ClientStartupError? EnvelopeError(
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

    private static bool HasCompleteBoard(WorldDescriptor? world) =>
        world?.Players is not null
        && world.Areas is not null
        && world.GameAreas is not null
        && world.Players.All(player => player is not null && player.Name is not null)
        && world.GameAreas.All(area => area is not null && area.PlayAreas is not null)
        && world.Areas.All(area =>
            area is not null
            && area.Zone is not null
            && area.Cards is not null
            && area.Removed is not null
            && area.Cards.Concat(area.Removed).All(card =>
                card is not null
                && (card.Face is null
                    || card.Face.Id is not null
                    && card.Face.Title is not null
                    && card.Face.Subtitle is not null
                    && card.Face.Fields is not null)));

    private static bool HasCompleteGameplayResponse(EngineResponse response) =>
        HasCompleteEvents(response.Events)
        && HasCompleteBoard(response.World)
        && Enum.IsDefined(response.World!.Outcome)
        && (response.World.Outcome == Outcome.Unfinished
            ? HasCompletePrompt(response.Prompt)
            : response.Prompt is null);

    private static bool HasCompleteEvents(IReadOnlyList<GameEvent>? events) =>
        events is not null && events.All(happened =>
            happened is not null
            && happened.Trigger is not null
            && happened.Verb is not null
            && happened switch
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
            });

    private static bool Complete(AreaRef area) =>
        area.Zone is not null && area.Id is not null;

    private static bool Complete(EngineError error) =>
        error.Code is not null && error.Message is not null;

    private static bool HasCompletePrompt(Prompt? prompt) =>
        prompt?.Trigger is not null
        && prompt.Label is not null
        && prompt.Affordances is { Count: > 0 }
        && prompt.Affordances.All(option =>
            option is not null
            && option.Verb is not null
            && option.Label is not null
            && (option.Targets is null
                || option.Targets.Legal is not null
                && (option.Targets.Groups is null
                    || option.Targets.Groups.All(group => group is not null))
                && (option.Targets.MustIncludeTraits is null
                    || option.Targets.MustIncludeTraits.All(trait => trait is not null)))
            && (option.Costs is null || option.Costs.All(cost =>
                cost is not null
                && cost.Cost is not null
                && cost.OrCost is not null
                && (cost.Rule is null || cost.Rule.All(rule => rule is not null))
                && (cost.OrRule is null || cost.OrRule.All(rule => rule is not null))
                && (cost.Sources is null
                    || cost.Sources.All(source => source.Generates is not null))
                && (cost.Variables is null
                    || cost.Variables.All(variable => variable.Name is not null))
                && (cost.Components is null || cost.Components.All(component =>
                    component is not null
                    && component.Cost is not null
                    && (component.Rule is null
                        || component.Rule.All(rule => rule is not null)))))));

    private static string Bounded(string value) =>
        value.Length <= MaximumDisplayedErrorLength
            ? value
            : value[..MaximumDisplayedErrorLength];
}

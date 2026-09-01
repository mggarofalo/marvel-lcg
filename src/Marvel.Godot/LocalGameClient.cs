using System.Globalization;
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

/// <summary>A locally composed client, or why committed content could not be loaded.</summary>
public sealed record LocalClientConnection(
    LocalGameClient? Client,
    ClientStartupError? Error)
{
    /// <summary>Whether the local engine transport is available.</summary>
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
                EngineRequest.ReadSetup("local-setup"), cancellationToken)
                .ConfigureAwait(false);
            if (response.Error is not null)
            {
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
                || response.Events is null)
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
        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.OpenGame(
                    "local-open", LocalGameSession.GameId, specification),
                cancellationToken).ConfigureAwait(false);

            if (response.Error is not null)
            {
                return Failed(response.Error.Code, response.Error.Message);
            }

            if (!HasCompleteBoard(response.World)
                || response.Prompt is null || response.Events is null
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

    private static ClientSetupResult SetupFailed(string code, string message) =>
        new(Choices: null, Error(code, message));

    private static ClientStartupResult Failed(string code, string message) =>
        new(Response: null, Error(code, message));

    private static ClientStartupError Error(string code, string message) =>
        new(Bounded(code), Bounded(message));

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

    private static string Bounded(string value) =>
        value.Length <= MaximumDisplayedErrorLength
            ? value
            : value[..MaximumDisplayedErrorLength];
}

using Marvel.Server;

namespace Marvel.Godot;

/// <summary>The stable Core Set game used while building the local-play client.</summary>
public static class DevelopmentGame
{
    /// <summary>A small, deterministic game that every development launch opens.</summary>
    public static GameSpecification Specification { get; } =
        new("rhino", ["spider_man"], ModularSets: null, Seed: 7);

    /// <summary>The client-chosen label for the one local table.</summary>
    public const string GameId = "local-core-game";
}

/// <summary>A bounded product-level startup failure suitable for display.</summary>
public sealed record ClientStartupError(string Code, string Message);

/// <summary>The result of trying to open the development game.</summary>
public sealed record ClientStartupResult(
    EngineResponse? Response,
    ClientStartupError? Error)
{
    /// <summary>Whether a complete initial game view was returned.</summary>
    public bool Succeeded => Response is not null && Error is null;
}

/// <summary>Opens the local game through the same protocol used by a remote client.</summary>
public sealed class LocalGameClient
{
    private const int MaximumDisplayedErrorLength = 240;
    private readonly IEngineTransport transport;

    /// <summary>Creates an app client over an embedded or remote transport.</summary>
    public LocalGameClient(IEngineTransport transport) =>
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));

    /// <summary>
    /// Loads committed content at the host boundary and opens the development game
    /// through <see cref="InProcessTransport"/>.
    /// </summary>
    public static async ValueTask<ClientStartupResult> OpenLocalAsync(
        string dataRoot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var host = new EngineHost(DatasetGameFactory.Load(dataRoot));
            return await new LocalGameClient(new InProcessTransport(host))
                .OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failed(
                "content_unavailable",
                "The local game could not be opened. Check that the committed datasets are available.");
        }
    }

    /// <summary>Sends the canonical open request through the configured transport.</summary>
    public async ValueTask<ClientStartupResult> OpenAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.OpenGame(
                    "local-open",
                    DevelopmentGame.GameId,
                    DevelopmentGame.Specification),
                cancellationToken).ConfigureAwait(false);

            if (response.Error is not null)
            {
                return Failed(response.Error.Code, response.Error.Message);
            }

            if (response.World is null || response.Prompt is null
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
                "The game service could not be reached. Try opening the local game again.");
        }
    }

    private static ClientStartupResult Failed(string code, string message) =>
        new(
            Response: null,
            new ClientStartupError(Bounded(code), Bounded(message)));

    private static string Bounded(string value) =>
        value.Length <= MaximumDisplayedErrorLength
            ? value
            : value[..MaximumDisplayedErrorLength];
}

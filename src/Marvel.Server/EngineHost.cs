using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Server;

/// <summary>The endpoint behind every transport.</summary>
public interface IEngineEndpoint
{
    /// <summary>Applies one protocol request synchronously.</summary>
    EngineResponse Exchange(EngineRequest request);
}

/// <summary>The only interface a client uses, embedded or hosted.</summary>
public interface IEngineTransport
{
    /// <summary>Sends one engine command and receives its result.</summary>
    ValueTask<EngineResponse> ExchangeAsync(
        EngineRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Creates a fresh game without deciding where its content bytes came from.</summary>
public interface IGameFactory
{
    /// <summary>Deals and opens one game.</summary>
    OpenedGame Create(GameSpecification specification);
}

/// <summary>A game and the card-text events produced during its setup.</summary>
public sealed record OpenedGame(Game Game, IReadOnlyList<GameEvent> SetupEvents);

/// <summary>A single-threaded collection of deterministic engine sessions.</summary>
/// <remarks>
/// The host is deliberately synchronous and has no lock. Socket I/O finishes
/// before a request reaches this type, and the standalone server handles one
/// request at a time. Threading or async must never become a path into game
/// state; see <c>AGENTS.md</c>, “Determinism is load-bearing”.
/// </remarks>
public sealed class EngineHost(IGameFactory factory) : IEngineEndpoint
{
    private readonly IGameFactory factory =
        factory ?? throw new ArgumentNullException(nameof(factory));
    private readonly Dictionary<string, Game> games = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public EngineResponse Exchange(EngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return Handle(request);
        }
        catch (Exception failure)
        {
            // A socket cannot throw a domain exception into its caller. The
            // same conversion happens here, before either transport, so local
            // play and hosted play have the same observable failure path. A
            // stack trace is server state and never crosses the boundary.
            return Failed(request, "engine_error", failure.Message);
        }
    }

    private EngineResponse Handle(EngineRequest request)
    {
        if (request.Version != EngineProtocol.Version)
        {
            return Failed(
                request, "unsupported_version",
                $"protocol {request.Version} is not supported; expected {EngineProtocol.Version}");
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Failed(request, "invalid_request", "request_id is required");
        }

        if (string.IsNullOrWhiteSpace(request.GameId))
        {
            return Failed(request, "invalid_request", "game_id is required");
        }

        return request.Operation switch
        {
            EngineProtocol.Open => Open(request),
            EngineProtocol.Resolve => Resolve(request),
            EngineProtocol.Close => Close(request),
            _ => Failed(
                request, "invalid_request",
                $"operation '{request.Operation}' is not supported"),
        };
    }

    private EngineResponse Open(EngineRequest request)
    {
        if (request.Game is null || request.Decision is not null)
        {
            return Failed(
                request, "invalid_request",
                "open requires game and does not accept decision");
        }

        if (string.IsNullOrWhiteSpace(request.Game.Scenario))
        {
            return Failed(request, "invalid_request", "a game requires a scenario");
        }

        if (request.Game.Heroes is not { Count: > 0 }
            || request.Game.Heroes.Any(string.IsNullOrWhiteSpace))
        {
            return Failed(request, "invalid_request", "a game requires at least one hero");
        }

        if (games.ContainsKey(request.GameId))
        {
            return Failed(request, "game_exists", $"game '{request.GameId}' is already open");
        }

        var opened = factory.Create(request.Game);
        games.Add(request.GameId, opened.Game);
        return Succeeded(request, opened.Game.Pending, opened.SetupEvents);
    }

    private EngineResponse Resolve(EngineRequest request)
    {
        if (request.Decision is null || request.Game is not null)
        {
            return Failed(
                request, "invalid_request",
                "resolve requires decision and does not accept game");
        }

        if (!games.TryGetValue(request.GameId, out var game))
        {
            return Failed(request, "game_not_found", $"game '{request.GameId}' is not open");
        }

        if (request.Decision.Targets is null)
        {
            return Failed(request, "invalid_request", "decision.targets is required");
        }

        var resolved = game.Resolve(request.Decision.ToDomain());
        return Succeeded(request, resolved.Prompt, resolved.Events);
    }

    private EngineResponse Close(EngineRequest request)
    {
        if (request.Game is not null || request.Decision is not null)
        {
            return Failed(
                request, "invalid_request",
                "close does not accept game or decision");
        }

        return games.Remove(request.GameId)
            ? Succeeded(request, prompt: null, events: [])
            : Failed(request, "game_not_found", $"game '{request.GameId}' is not open");
    }

    private static EngineResponse Succeeded(
        EngineRequest request,
        Prompt? prompt,
        IReadOnlyList<GameEvent> events) =>
        new(EngineProtocol.Version, request.RequestId, request.GameId, prompt, events);

    private static EngineResponse Failed(
        EngineRequest request, string code, string message) =>
        new(
            EngineProtocol.Version,
            request.RequestId ?? string.Empty,
            request.GameId ?? string.Empty,
            Prompt: null,
            Events: [],
            Error: new EngineError(code, message));
}

/// <summary>Loads the repository's canonical datasets and deals games from them.</summary>
public sealed class DatasetGameFactory : IGameFactory
{
    private readonly SetupCatalog setup;
    private readonly CardCatalog cards;
    private readonly AbilityBook abilities;

    private DatasetGameFactory(SetupCatalog setup, CardCatalog cards, AbilityBook abilities)
    {
        this.setup = setup;
        this.cards = cards;
        this.abilities = abilities;
    }

    /// <summary>Loads the three datasets beneath <paramref name="dataRoot"/>.</summary>
    public static DatasetGameFactory Load(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        string root = Path.GetFullPath(dataRoot);
        return new DatasetGameFactory(
            SetupCatalog.Parse(Read(root, "setup", "setup.json")),
            CardCatalog.Parse(Read(root, "cards", "cards.json")),
            AbilityCatalog.Parse(Read(root, "abilities", "abilities.json")));
    }

    /// <inheritdoc />
    public OpenedGame Create(GameSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        var runner = new AbilityRunner(abilities);
        var setupEvents = new List<GameEvent>();
        var campaign = setup.Campaign(specification.Scenario);
        var world = WorldSetup.Deal(
            cards,
            Blueprints.From(
                Dealer.DealOrder(
                    setup,
                    specification.Scenario,
                    specification.Heroes,
                    specification.ModularSets),
                cards),
            [.. specification.Heroes.Select(hero => setup.Hero(hero).Name)],
            specification.Seed,
            runner,
            setupEvents,
            campaign.Expert);
        return new OpenedGame(Game.Begin(world, cards, runner), setupEvents);
    }

    private static string Read(string root, string dataset, string file) =>
        File.ReadAllText(Path.Combine(root, "datasets", dataset, file));

}

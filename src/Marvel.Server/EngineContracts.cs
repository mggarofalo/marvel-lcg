using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;

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

/// <summary>A game factory whose dataset identities make durable replay meaningful.</summary>
public interface IDurableGameFactory : IGameFactory
{
    /// <summary>The replay contracts and dataset hashes used by this factory.</summary>
    SessionCompatibility Compatibility { get; }
}

/// <summary>Exposes the authored choices a client may use to open a game.</summary>
public interface ISetupDiscovery
{
    /// <summary>Returns the complete supported setup surface.</summary>
    SetupChoices DiscoverSetup();
}

/// <summary>Issues transport capabilities that never enter deterministic game state.</summary>
public interface ISessionCapabilityIssuer
{
    /// <summary>Returns a new opaque capability.</summary>
    string Issue();
}

/// <summary>A game and the card-text events produced during its setup.</summary>
public sealed record OpenedGame(Game Game, IReadOnlyList<GameEvent> SetupEvents);

using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>The only interface a client uses, embedded or hosted.</summary>
public interface IEngineTransport
{
    /// <summary>Sends one engine command and receives its result.</summary>
    ValueTask<EngineResponse> ExchangeAsync(
        EngineRequest request,
        CancellationToken cancellationToken = default);
}

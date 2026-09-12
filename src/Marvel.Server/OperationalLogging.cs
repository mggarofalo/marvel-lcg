using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Marvel.Server;

/// <summary>Stable identifiers for machine-readable operational events.</summary>
public static class OperationalEventIds
{
    /// <summary>One engine protocol request reached a final disposition.</summary>
    public const string RequestCompleted = "server.request.completed";

    /// <summary>One durable session was verified and published during startup.</summary>
    public const string SessionRestored = "session.restore.completed";

    /// <summary>Startup rejected a durable session before publication.</summary>
    public const string SessionRestoreFailed = "session.restore.failed";

    /// <summary>The standalone socket listener began accepting connections.</summary>
    public const string ServerListening = "server.listener.started";

    /// <summary>The standalone listener completed an intentional shutdown.</summary>
    public const string ServerStopped = "server.listener.stopped";

    /// <summary>The process could not start its server composition.</summary>
    public const string ServerStartFailed = "server.start.failed";

    /// <summary>One client transport exchange reached a final disposition.</summary>
    public const string TransportCompleted = "transport.exchange.completed";

    /// <summary>A client synchronized after an uncertain mutation outcome.</summary>
    public const string ReconnectCompleted = "client.reconnect.completed";

    /// <summary>One replay stage reached a final operational disposition.</summary>
    public const string ReplayCompleted = "session.replay.completed";

    /// <summary>One persistence stage reached a final operational disposition.</summary>
    public const string PersistenceCompleted = "session.persistence.completed";

    /// <summary>A configured durable diagnostic destination could not be used.</summary>
    public const string DiagnosticsUnavailable = "diagnostics.sink.unavailable";
}

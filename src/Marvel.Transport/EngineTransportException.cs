using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Marvel.Transport;

/// <summary>An exchange failed before or after host dispatch or request transmission could begin.</summary>
public sealed class EngineTransportException : IOException
{
    /// <summary>Creates a transport failure without exposing its diagnostic as the message.</summary>
    public EngineTransportException(bool requestMayHaveCommitted, Exception innerException)
        : base("the engine transport exchange failed", innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        RequestMayHaveCommitted = requestMayHaveCommitted;
    }

    /// <summary>
    /// Whether host dispatch or request transmission began before the failure occurred.
    /// </summary>
    public bool RequestMayHaveCommitted { get; }
}

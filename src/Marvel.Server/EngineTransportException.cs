using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Marvel.Server;

/// <summary>A socket exchange failed before or after request transmission could begin.</summary>
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
    /// Whether request transmission began before the failure occurred.
    /// </summary>
    public bool RequestMayHaveCommitted { get; }
}

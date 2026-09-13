using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Marvel.Server;

/// <summary>A TCP transport for the same request/response contract used in-process.</summary>
/// <remarks>
/// Each exchange is one connection and one length-prefixed UTF-8 JSON frame in
/// each direction. That lets the standalone process serve clients in sequence
/// without a connection monopolising its single game-state thread. Framing and
/// the 4 MiB limit are our protocol choices, not game rules.
/// </remarks>
public sealed class SocketTransport(
    string host,
    int port,
    OperationalLog? operationalLog = null) : IEngineTransport
{
    private readonly Action? onRequestWriteStarting;
    private readonly Action? onRequestCommitted;
    private readonly OperationalLog log = operationalLog ?? OperationalLog.None;
    private readonly string host =
        !string.IsNullOrWhiteSpace(host)
            ? host
            : throw new ArgumentException("a socket host is required", nameof(host));
    private readonly int port = port is > 0 and <= ushort.MaxValue
        ? port
        : throw new ArgumentOutOfRangeException(nameof(port));

    internal SocketTransport(
        string host, int port, Action onRequestCommitted)
        : this(host, port, operationalLog: null) =>
        this.onRequestCommitted = onRequestCommitted
            ?? throw new ArgumentNullException(nameof(onRequestCommitted));

    internal SocketTransport(
        string host,
        int port,
        Action onRequestWriteStarting,
        Action onRequestCommitted)
        : this(host, port, operationalLog: null)
    {
        this.onRequestWriteStarting = onRequestWriteStarting
            ?? throw new ArgumentNullException(nameof(onRequestWriteStarting));
        this.onRequestCommitted = onRequestCommitted
            ?? throw new ArgumentNullException(nameof(onRequestCommitted));
    }

    /// <inheritdoc />
    public async ValueTask<EngineResponse> ExchangeAsync(
        EngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        bool requestMayHaveCommitted = false;
        var elapsed = Stopwatch.StartNew();

        try
        {
            return await PerformAsync(request, elapsed,
                () => requestMayHaveCommitted = true, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            !requestMayHaveCommitted && cancellationToken.IsCancellationRequested)
        {
            Observe(request, elapsed, "cancelled", "transport_cancelled");
            throw;
        }
        catch (OperationCanceledException failure)
        {
            // Cancellation after transmission begins cannot prove that the
            // server did not apply the request. Preserve that uncertainty so
            // mutation clients synchronize instead of retrying the decision.
            Observe(request, elapsed, "uncertain", "transport_failed");
            throw new EngineTransportException(requestMayHaveCommitted, failure);
        }
        catch (Exception failure) when (failure is IOException
                                             or SocketException
                                             or InvalidDataException
                                             or JsonException
                                             or NotSupportedException)
        {
            Observe(
                request,
                elapsed,
                requestMayHaveCommitted ? "uncertain" : "rejected",
                "transport_failed");
            throw new EngineTransportException(requestMayHaveCommitted, failure);
        }
    }

    private async ValueTask<EngineResponse> PerformAsync(
        EngineRequest request, Stopwatch elapsed, Action markCommitted,
        CancellationToken cancellationToken)
    {
        byte[] requestFrame = EngineJson.Write(request);
        SocketFrame.ValidatePayloadLength(requestFrame.Length);
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        using NetworkStream stream = client.GetStream();
        markCommitted();
        onRequestWriteStarting?.Invoke();
        await SocketFrame.WriteAsync(stream, requestFrame, cancellationToken).ConfigureAwait(false);
        onRequestCommitted?.Invoke();
        byte[] response = await SocketFrame.ReadAsync(stream, CancellationToken.None)
            .ConfigureAwait(false)
            ?? throw new EndOfStreamException("the engine host closed without a response");
        return ParseResponse(request, elapsed, response);
    }

    private EngineResponse ParseResponse(
        EngineRequest request, Stopwatch elapsed, byte[] response)
    {
        int version = EngineJson.ReadResponseVersion(response);
        if (version != EngineProtocol.Version)
        {
            var mismatch = new EngineResponse(version, request.RequestId, request.GameId,
                Capability: null, Prompt: null, Events: []);
            Observe(request, elapsed, "rejected", "unsupported_version");
            return mismatch;
        }
        EngineResponse parsed = EngineJson.ReadResponse(response);
        string disposition = Disposition(parsed);
        long? revision = parsed.Error is null
            && request.Operation is not (EngineProtocol.Setup or EngineProtocol.Close)
            ? parsed.Revision : null;
        Observe(request, elapsed, disposition, parsed.Error?.Code, revision);
        return parsed;
    }

    private static string Disposition(EngineResponse response) => response.Error is null
        ? "accepted"
        : response.Error.Code.StartsWith("stale_", StringComparison.Ordinal) ? "stale" : "rejected";

    private void Observe(
        EngineRequest request,
        Stopwatch elapsed,
        string disposition,
        string? errorCode,
        long? revision = null)
    {
        elapsed.Stop();
        log.Write(
            OperationalEventIds.TransportCompleted,
            disposition,
            elapsed.ElapsedMilliseconds,
            request.RequestId,
            request.GameId,
            request.Operation,
            revision,
            errorCode: errorCode,
            expectedRevision: request.ExpectedRevision);
    }
}

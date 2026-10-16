using Marvel.Server;
using Marvel.Observability;
using Marvel.Transport;
using Marvel.Protocol;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Marvel.View;

namespace Marvel.Server.Host;

internal static class Program
{
    private const int DefaultPort = 41923;
    private static readonly TimeSpan OperationalShutdownBudget = TimeSpan.FromSeconds(3);

    public static int Main(string[] args)
    {
        if (args.SequenceEqual(["--version"], StringComparer.Ordinal))
        {
            Console.Out.WriteLine(EngineBuildIdentity.Display);
            return 0;
        }
        if (args.SequenceEqual(["--health-check"], StringComparer.Ordinal))
        {
            return HealthCheck(IPAddress.Loopback.ToString(), DefaultPort);
        }
        if (args.Contains("--export-incident", StringComparer.Ordinal))
        {
            return ExportIncident(args, Console.Out, Console.Error);
        }

        OperationalLog log = CreateLog(Console.Error, telemetryEndpoint: null);
        using var stopping = new CancellationTokenSource();
        ConsoleCancelEventHandler stop = (_, signal) =>
        {
            signal.Cancel = true;
            stopping.Cancel();
        };
        bool subscribed = false;
        try
        {
            ServerOptions options = ServerOptions.Parse(args);
            log = CreateLog(
                Console.Error, options.TelemetryEndpoint, options.DiagnosticsRoot);
            SocketEngineServer server = Prepare(options, log);
            Console.CancelKeyPress += stop;
            subscribed = true;
            using PosixSignalRegistration? terminate = OperatingSystem.IsWindows()
                ? null
                : PosixSignalRegistration.Create(PosixSignal.SIGTERM, signal =>
                {
                    signal.Cancel = true;
                    stopping.Cancel();
                });
            return Serve(server, log, onListening: null, stopping.Token);
        }
        catch (Exception)
        {
            return Failed(log);
        }
        finally
        {
            if (subscribed)
            {
                Console.CancelKeyPress -= stop;
            }
        }
    }

    internal static int HealthCheck(string host, int port)
    {
        try
        {
            var transport = new SocketTransport(host, port);
            EngineResponse response = transport.ExchangeAsync(
                    EngineRequest.ReadSetup("health"),
                    CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            return response.Error is null
                && response.Setup?.Runtime.Protocol == EngineProtocol.Version
                && string.Equals(
                    response.Setup.Runtime.ProductVersion,
                    EngineBuildIdentity.ProductVersion,
                    StringComparison.Ordinal)
                    ? 0
                    : 1;
        }
        catch (Exception)
        {
            return 1;
        }
    }

    internal static int ExportIncident(
        string[] args,
        TextWriter output,
        TextWriter error)
    {
        try
        {
            IncidentExportOptions options = IncidentExportOptions.Parse(args);
            if (options.Output != "-")
            {
                throw new ArgumentException("incident export writes only to standard output");
            }
            output.Write(IncidentExporter.Serialize(IncidentExporter.Build(
                options.DataRoot,
                options.SaveRoot,
                options.DiagnosticsRoot)));
            return 0;
        }
        catch (Exception)
        {
            error.WriteLine("incident export failed without changing server state");
            return 2;
        }
    }

    internal static int Run(
        string[] args,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(error);
        OperationalLog log = CreateLog(error, telemetryEndpoint: null);
        try
        {
            ServerOptions options = ServerOptions.Parse(args);
            log = CreateLog(error, options.TelemetryEndpoint, options.DiagnosticsRoot);
            return Serve(Prepare(options, log), log, onListening: null, cancellationToken);
        }
        catch (Exception)
        {
            return Failed(log);
        }
    }

    internal static int Run(
        ServerOptions options,
        TextWriter error,
        Action<IPEndPoint> onListening,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(onListening);
        ArgumentNullException.ThrowIfNull(error);
        OperationalLog log = CreateLog(
            error, options.TelemetryEndpoint, options.DiagnosticsRoot);
        try
        {
            return Serve(Prepare(options, log), log, onListening, cancellationToken);
        }
        catch (Exception)
        {
            return Failed(log);
        }
    }

    private static OperationalLog CreateLog(
        TextWriter error,
        Uri? telemetryEndpoint,
        string? diagnosticsRoot = null)
    {
        IOperationalSink sink = new JsonTextOperationalSink(error);
        bool diagnosticsUnavailable = false;
        if (diagnosticsRoot is not null)
        {
            try
            {
                sink = new CompositeOperationalSink(
                    sink,
                    new RotatingJsonFileOperationalSink(diagnosticsRoot));
            }
            catch (Exception)
            {
                diagnosticsUnavailable = true;
            }
        }

        if (telemetryEndpoint is not null)
        {
            sink = new CompositeOperationalSink(
                sink,
                new OperationalTelemetrySink(
                    new HttpTelemetryExporter(telemetryEndpoint)));
        }

        var log = new OperationalLog(sink, "Marvel.Server", runtime: EngineBuildIdentity.Display);
        if (diagnosticsUnavailable)
        {
            log.Write(
                OperationalEventIds.DiagnosticsUnavailable,
                "rejected",
                operation: "start",
                errorCode: "diagnostics_unavailable");
        }

        return log;
    }

    private static SocketEngineServer Prepare(
        ServerOptions options, OperationalLog? log = null)
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(options.DataRoot),
            visibility: options.Visibility,
            store: new FileSessionStore(options.SaveRoot),
            log: log);
        return new SocketEngineServer(host, options.Address, options.Port);
    }

    private static int Serve(
        SocketEngineServer server,
        OperationalLog log,
        Action<IPEndPoint>? onListening,
        CancellationToken cancellationToken)
    {
        server.Run(endpoint =>
        {
            log.Write(
                OperationalEventIds.ServerListening,
                "accepted",
                operation: "listen");
            onListening?.Invoke(endpoint);
        }, cancellationToken);
        log.Write(
            OperationalEventIds.ServerStopped,
            "accepted",
            operation: "listen");
        log.Flush(OperationalShutdownBudget);
        return 0;
    }

    private static int Failed(OperationalLog log)
    {
        log.Write(
            OperationalEventIds.ServerStartFailed,
            "rejected",
            operation: "start",
            errorCode: "server_start_failed");
        log.Flush(OperationalShutdownBudget);
        return 2;
    }

}

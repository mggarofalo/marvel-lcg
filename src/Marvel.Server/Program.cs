using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Marvel.View;

namespace Marvel.Server;

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

        var log = new OperationalLog(sink, "Marvel.Server");
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

    internal sealed record ServerOptions(
        IPAddress Address,
        int Port,
        string DataRoot,
        IVisibilityPolicy Visibility,
        string SaveRoot,
        Uri? TelemetryEndpoint = null,
        string? DiagnosticsRoot = null)
    {
        public static ServerOptions Parse(string[] args)
        {
            IPAddress address = IPAddress.Loopback;
            int port = DefaultPort;
            string dataRoot = Environment.CurrentDirectory;
            string saveRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MarvelLCG",
                "sessions");
            string visibility = "cooperative";
            int? seat = null;
            Uri? telemetryEndpoint = null;
            string? diagnosticsRoot = null;

            for (int index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--listen":
                        string printed = Value(args, ref index, "--listen");
                        address = TryAddress(printed, out IPAddress? parsed)
                            ? parsed
                            : throw new ArgumentException(
                                $"--listen requires an IP address, got '{printed}'");
                        break;
                    case "--port":
                        string number = Value(args, ref index, "--port");
                        port = int.TryParse(
                            number, NumberStyles.None, CultureInfo.InvariantCulture,
                            out int parsedPort)
                            && parsedPort is > 0 and <= ushort.MaxValue
                                ? parsedPort
                                : throw new ArgumentException(
                                    "--port requires an integer from 1 to 65535");
                        break;
                    case "--data-root":
                        dataRoot = Value(args, ref index, "--data-root");
                        break;
                    case "--save-root":
                        saveRoot = Value(args, ref index, "--save-root");
                        break;
                    case "--visibility":
                        visibility = Value(args, ref index, "--visibility");
                        break;
                    case "--seat":
                        string seatNumber = Value(args, ref index, "--seat");
                        seat = int.TryParse(
                            seatNumber, NumberStyles.None, CultureInfo.InvariantCulture,
                            out int parsedSeat)
                            && parsedSeat >= 0
                            ? parsedSeat
                            : throw new ArgumentException(
                                "--seat requires a non-negative integer");
                        break;
                    case "--telemetry-endpoint":
                        string endpoint = Value(args, ref index, "--telemetry-endpoint");
                        telemetryEndpoint = TryTelemetryEndpoint(endpoint, out Uri? parsedEndpoint)
                            ? parsedEndpoint
                            : throw new ArgumentException(
                                "--telemetry-endpoint requires HTTPS or loopback HTTP");
                        break;
                    case "--diagnostics-root":
                        diagnosticsRoot = Value(args, ref index, "--diagnostics-root");
                        break;
                    default:
                        throw new ArgumentException($"unknown option '{args[index]}'");
                }
            }

            IVisibilityPolicy policy = visibility switch
            {
                "cooperative" when seat is null => new PermissiveVisibilityPolicy(),
                "restricted" when seat is int authorized =>
                    new RestrictedVisibilityPolicy(authorized),
                "restricted" => throw new ArgumentException(
                    "--visibility restricted requires --seat"),
                "cooperative" => throw new ArgumentException(
                    "--seat is only valid with --visibility restricted"),
                _ => throw new ArgumentException(
                    "--visibility must be cooperative or restricted"),
            };
            return new ServerOptions(
                address, port, dataRoot, policy, saveRoot, telemetryEndpoint,
                diagnosticsRoot);
        }

        private static string Value(
            string[] args, ref int index, string option) =>
            ++index < args.Length
                ? args[index]
                : throw new ArgumentException($"{option} requires a value");

        private static bool TryAddress(string printed, out IPAddress address)
        {
            address = null!;
            if (!IPAddress.TryParse(printed, out IPAddress? parsed))
            {
                return false;
            }

            if (parsed.AddressFamily == AddressFamily.InterNetworkV6)
            {
                address = parsed;
                return printed.Contains(':');
            }

            string[] octets = printed.Split('.');
            if (parsed.AddressFamily != AddressFamily.InterNetwork
                || octets.Length != 4
                || octets.Any(octet =>
                    !byte.TryParse(
                        octet,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out byte value)
                    || octet != value.ToString(CultureInfo.InvariantCulture)))
            {
                return false;
            }

            address = parsed;
            return true;
        }

        private static bool TryTelemetryEndpoint(string printed, out Uri? endpoint)
        {
            endpoint = null;
            if (!Uri.TryCreate(printed, UriKind.Absolute, out Uri? parsed)
                || !HttpTelemetryExporter.IsAllowedEndpoint(parsed))
            {
                return false;
            }

            endpoint = parsed;
            return true;
        }
    }

    internal sealed record IncidentExportOptions(
        string Output,
        string DataRoot,
        string SaveRoot,
        string DiagnosticsRoot)
    {
        public static IncidentExportOptions Parse(string[] args)
        {
            string? output = null;
            string dataRoot = Environment.CurrentDirectory;
            string? saveRoot = null;
            string? diagnosticsRoot = null;
            for (int index = 0; index < args.Length; index++)
            {
                string value = index + 1 < args.Length
                    ? args[index + 1]
                    : string.Empty;
                switch (args[index])
                {
                    case "--export-incident":
                        output = Required(value, args[index]);
                        index++;
                        break;
                    case "--data-root":
                        dataRoot = Required(value, args[index]);
                        index++;
                        break;
                    case "--save-root":
                        saveRoot = Required(value, args[index]);
                        index++;
                        break;
                    case "--diagnostics-root":
                        diagnosticsRoot = Required(value, args[index]);
                        index++;
                        break;
                    default:
                        throw new ArgumentException("unsupported incident export option");
                }
            }

            return new IncidentExportOptions(
                output ?? throw new ArgumentException("--export-incident is required"),
                dataRoot,
                saveRoot ?? throw new ArgumentException("--save-root is required"),
                diagnosticsRoot
                    ?? throw new ArgumentException("--diagnostics-root is required"));
        }

        private static string Required(string value, string option) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException($"{option} requires a value")
                : value;
    }
}

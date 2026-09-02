using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Marvel.View;

namespace Marvel.Server;

internal static class Program
{
    private const int DefaultPort = 41923;

    public static int Main(string[] args)
    {
        using var stopping = new CancellationTokenSource();
        ConsoleCancelEventHandler stop = (_, signal) =>
        {
            signal.Cancel = true;
            stopping.Cancel();
        };
        bool subscribed = false;
        try
        {
            SocketEngineServer server = Prepare(ServerOptions.Parse(args));
            Console.CancelKeyPress += stop;
            subscribed = true;
            using PosixSignalRegistration? terminate = OperatingSystem.IsWindows()
                ? null
                : PosixSignalRegistration.Create(PosixSignal.SIGTERM, signal =>
                {
                    signal.Cancel = true;
                    stopping.Cancel();
                });
            return Serve(server, Console.Error, onListening: null, stopping.Token);
        }
        catch (Exception failure)
        {
            return Failed(Console.Error, failure);
        }
        finally
        {
            if (subscribed)
            {
                Console.CancelKeyPress -= stop;
            }
        }
    }

    internal static int Run(
        string[] args,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        return Run(() => Prepare(ServerOptions.Parse(args)), error,
            onListening: null, cancellationToken);
    }

    internal static int Run(
        ServerOptions options,
        TextWriter error,
        Action<IPEndPoint> onListening,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(onListening);
        return Run(() => Prepare(options), error, onListening, cancellationToken);
    }

    private static int Run(
        Func<SocketEngineServer> prepare,
        TextWriter error,
        Action<IPEndPoint>? onListening,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            return Serve(prepare(), error, onListening, cancellationToken);
        }
        catch (Exception failure)
        {
            return Failed(error, failure);
        }
    }

    private static SocketEngineServer Prepare(ServerOptions options)
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(options.DataRoot),
            visibility: options.Visibility,
            store: new FileSessionStore(options.SaveRoot));
        return new SocketEngineServer(host, options.Address, options.Port);
    }

    private static int Serve(
        SocketEngineServer server,
        TextWriter error,
        Action<IPEndPoint>? onListening,
        CancellationToken cancellationToken)
    {
        server.Run(endpoint =>
        {
            error.WriteLine(
                $"Marvel.Server protocol {EngineProtocol.Version} listening on "
                + endpoint);
            onListening?.Invoke(endpoint);
        }, cancellationToken);
        return 0;
    }

    private static int Failed(TextWriter error, Exception failure)
    {
        error.WriteLine(failure.Message);
        return 2;
    }

    internal sealed record ServerOptions(
        IPAddress Address,
        int Port,
        string DataRoot,
        IVisibilityPolicy Visibility,
        string SaveRoot)
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
            return new ServerOptions(address, port, dataRoot, policy, saveRoot);
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
    }
}

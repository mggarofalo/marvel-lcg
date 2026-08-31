using System.Globalization;
using System.Net;
using Marvel.View;

namespace Marvel.Server;

internal static class Program
{
    private const int DefaultPort = 41923;

    public static int Main(string[] args)
    {
        try
        {
            var options = ServerOptions.Parse(args);
            var host = new EngineHost(
                DatasetGameFactory.Load(options.DataRoot),
                visibility: options.Visibility);
            var server = new SocketEngineServer(host, options.Address, options.Port);
            using var stopping = new CancellationTokenSource();
            Console.CancelKeyPress += (_, signal) =>
            {
                signal.Cancel = true;
                stopping.Cancel();
            };

            Console.Error.WriteLine(
                $"Marvel.Server protocol {EngineProtocol.Version} listening on "
                + $"{options.Address}:{options.Port}");
            server.Run(stopping.Token);
            return 0;
        }
        catch (Exception failure)
        {
            Console.Error.WriteLine(failure.Message);
            return 2;
        }
    }

    internal sealed record ServerOptions(
        IPAddress Address,
        int Port,
        string DataRoot,
        IVisibilityPolicy Visibility)
    {
        public static ServerOptions Parse(string[] args)
        {
            IPAddress address = IPAddress.Loopback;
            int port = DefaultPort;
            string dataRoot = Environment.CurrentDirectory;
            string visibility = "cooperative";
            int? seat = null;

            for (int index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--listen":
                        string printed = Value(args, ref index, "--listen");
                        address = IPAddress.TryParse(printed, out var parsed)
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
            return new ServerOptions(address, port, dataRoot, policy);
        }

        private static string Value(
            string[] args, ref int index, string option) =>
            ++index < args.Length
                ? args[index]
                : throw new ArgumentException($"{option} requires a value");
    }
}

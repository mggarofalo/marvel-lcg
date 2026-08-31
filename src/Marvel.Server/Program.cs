using System.Globalization;
using System.Net;

namespace Marvel.Server;

internal static class Program
{
    private const int DefaultPort = 41923;

    public static int Main(string[] args)
    {
        try
        {
            var options = ServerOptions.Parse(args);
            var host = new EngineHost(DatasetGameFactory.Load(options.DataRoot));
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

    private sealed record ServerOptions(IPAddress Address, int Port, string DataRoot)
    {
        public static ServerOptions Parse(string[] args)
        {
            IPAddress address = IPAddress.Loopback;
            int port = DefaultPort;
            string dataRoot = Environment.CurrentDirectory;

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
                    default:
                        throw new ArgumentException($"unknown option '{args[index]}'");
                }
            }

            return new ServerOptions(address, port, dataRoot);
        }

        private static string Value(
            string[] args, ref int index, string option) =>
            ++index < args.Length
                ? args[index]
                : throw new ArgumentException($"{option} requires a value");
    }
}

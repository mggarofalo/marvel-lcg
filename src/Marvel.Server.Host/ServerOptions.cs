using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Marvel.Observability;
using Marvel.View;

namespace Marvel.Server.Host;

/// <summary>Validates standalone host configuration before any session is loaded.</summary>
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
        var state = new ServerOptionState();
        for (int index = 0; index < args.Length; index++)
        {
            state.Apply(args[index], args, ref index);
        }
        return state.Options();
    }

    private sealed class ServerOptionState
    {
        private IPAddress address = IPAddress.Loopback;
        private int port = 41923;
        private string dataRoot = Environment.CurrentDirectory;
        private string saveRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MarvelLCG", "sessions");
        private string visibility = "cooperative";
        private int? seat;
        private Uri? telemetryEndpoint;
        private string? diagnosticsRoot;

        internal void Apply(string option, string[] args, ref int index)
        {
            switch (option)
            {
                case "--listen": address = ParseAddress(Value(args, ref index, option)); break;
                case "--port": port = ParsePort(Value(args, ref index, option)); break;
                case "--data-root": dataRoot = Value(args, ref index, option); break;
                case "--save-root": saveRoot = Value(args, ref index, option); break;
                case "--visibility": visibility = Value(args, ref index, option); break;
                case "--seat": seat = ParseSeat(Value(args, ref index, option)); break;
                case "--telemetry-endpoint":
                    telemetryEndpoint = ParseTelemetry(Value(args, ref index, option)); break;
                case "--diagnostics-root": diagnosticsRoot = Value(args, ref index, option); break;
                default: throw new ArgumentException($"unknown option '{option}'");
            }
        }

        internal ServerOptions Options() => new(
            address, port, dataRoot, Visibility(), saveRoot, telemetryEndpoint,
            diagnosticsRoot);

        private IVisibilityPolicy Visibility()
        {
            if (visibility == "cooperative" && seat is null)
                return new PermissiveVisibilityPolicy();
            if (visibility == "restricted" && seat is int authorized)
                return new RestrictedVisibilityPolicy(authorized);
            if (visibility == "restricted")
                throw new ArgumentException("--visibility restricted requires --seat");
            if (visibility == "cooperative")
                throw new ArgumentException("--seat is only valid with --visibility restricted");
            throw new ArgumentException("--visibility must be cooperative or restricted");
        }

        private static IPAddress ParseAddress(string printed) =>
            TryAddress(printed, out IPAddress? parsed) ? parsed
            : throw new ArgumentException(
                $"--listen requires an IP address, got '{printed}'");

        private static int ParsePort(string printed) =>
            int.TryParse(printed, NumberStyles.None, CultureInfo.InvariantCulture,
                out int parsed) && parsed is > 0 and <= ushort.MaxValue ? parsed
            : throw new ArgumentException("--port requires an integer from 1 to 65535");

        private static int ParseSeat(string printed) =>
            int.TryParse(printed, NumberStyles.None, CultureInfo.InvariantCulture,
                out int parsed) && parsed >= 0 ? parsed
            : throw new ArgumentException("--seat requires a non-negative integer");

        private static Uri ParseTelemetry(string printed) =>
            TryTelemetryEndpoint(printed, out Uri? parsed) ? parsed!
            : throw new ArgumentException(
                "--telemetry-endpoint requires HTTPS or loopback HTTP");
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

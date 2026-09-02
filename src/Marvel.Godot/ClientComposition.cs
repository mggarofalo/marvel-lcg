using Marvel.Server;

namespace Marvel.Godot;

/// <summary>Selects the desktop client's engine transport from bounded local configuration.</summary>
public static class ClientComposition
{
    private const int MaximumEndpointLength = 512;
    private const int MaximumHostLength = 253;
    private static readonly OperationalLog Log = CreateLog();

    /// <summary>Connects to the embedded engine by default or an explicitly configured socket.</summary>
    public static LocalClientConnection Connect(string dataRoot, string? configuredEndpoint)
    {
        if (string.IsNullOrEmpty(configuredEndpoint))
        {
            return LocalGameClient.ConnectLocal(dataRoot);
        }

        if (!TryReadEndpoint(configuredEndpoint, out string host, out int port))
        {
            return new LocalClientConnection(
                Client: null,
                new ClientStartupError(
                    "invalid_endpoint",
                    "MARVEL_ENGINE_ENDPOINT must be an explicit tcp://host:port endpoint."));
        }

        return new LocalClientConnection(
            new LocalGameClient(new SocketTransport(host, port, Log), Log),
            Error: null);
    }

    internal static OperationalLog ProcessLog => Log;

    internal static void Flush(TimeSpan timeout) => Log.Flush(timeout);

    private static OperationalLog CreateLog()
    {
        IOperationalSink sink = new JsonTextOperationalSink(Console.Error);
        string? configured = Environment.GetEnvironmentVariable(
            "MARVEL_TELEMETRY_ENDPOINT");
        if (Uri.TryCreate(configured, UriKind.Absolute, out Uri? endpoint)
            && HttpTelemetryExporter.IsAllowedEndpoint(endpoint))
        {
            sink = new CompositeOperationalSink(
                sink,
                new OperationalTelemetrySink(new HttpTelemetryExporter(endpoint)));
        }

        return new OperationalLog(sink, "Marvel.Godot");
    }

    private static bool TryReadEndpoint(
        string configuredEndpoint,
        out string host,
        out int port)
    {
        host = string.Empty;
        port = 0;
        if (configuredEndpoint.Length > MaximumEndpointLength
            || configuredEndpoint.Any(character =>
                char.IsWhiteSpace(character) || char.IsControl(character))
            || !Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out Uri? endpoint)
            || !string.Equals(endpoint.Scheme, "tcp", StringComparison.OrdinalIgnoreCase)
            || endpoint.UserInfo.Length != 0
            || endpoint.Host.Length == 0
            || endpoint.IdnHost.Length > MaximumHostLength
            || endpoint.Port is <= 0 or > ushort.MaxValue
            || endpoint.AbsolutePath is not ("" or "/")
            || endpoint.Query.Length != 0
            || endpoint.Fragment.Length != 0
            || endpoint.HostNameType == UriHostNameType.Unknown)
        {
            return false;
        }

        host = endpoint.IdnHost;
        port = endpoint.Port;
        return true;
    }
}

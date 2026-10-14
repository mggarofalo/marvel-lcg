using Marvel.Server;

namespace Marvel.Client;

/// <summary>Composes content and the ordered embedded host transport.</summary>
internal static class LocalClientConnectionFactory
{
    private static readonly object gate = new();
    private static readonly HashSet<QueuedInProcessTransport> hosts = [];
    private static bool stopping;

    internal static LocalClientConnection Connect(string dataRoot)
    {
        OperationalLog log = ClientComposition.ProcessLog;
        try
        {
            var host = new EngineHost(DatasetGameFactory.Load(dataRoot), log: log);
            lock (gate)
            {
                if (stopping) throw new InvalidOperationException("The desktop host lifetime is closing.");
                var transport = new QueuedInProcessTransport(host);
                hosts.Add(transport);
                _ = ReleaseAfterShutdownAsync(transport);
                return new LocalClientConnection(new LocalGameClient(transport, log), Error: null);
            }
        }
        catch (Exception)
        {
            log.Write(OperationalEventIds.ServerStartFailed, "rejected",
                operation: "embedded_start", errorCode: "content_unavailable");
            return new LocalClientConnection(Client: null, new ClientStartupError("content_unavailable",
                "The local game could not load its committed Core Set content."));
        }
    }

    internal static Task ShutdownAsync()
    {
        lock (gate)
        {
            stopping = true;
            return Task.WhenAll(hosts.Select(host => host.DisposeAsync().AsTask()));
        }
    }

    private static async Task ReleaseAfterShutdownAsync(QueuedInProcessTransport transport)
    {
        await transport.ShutdownCompletion.ConfigureAwait(false);
        lock (gate) hosts.Remove(transport);
    }
}

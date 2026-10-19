using Marvel.Client;
using Marvel.Observability;
using Marvel.Transport;
using Marvel.Server;

namespace Marvel.Desktop;

/// <summary>Composes content and the ordered embedded host transport.</summary>
internal static class LocalClientConnectionFactory
{
    private static readonly object gate = new();
    private static readonly HashSet<QueuedInProcessTransport> hosts = [];
    private static bool stopping;
    private static readonly DesktopPersistentHosts persistent = new();

    internal static LocalClientConnection Connect(string dataRoot)
    {
        OperationalLog log = ClientComposition.ProcessLog;
        try
        {
            lock (gate)
            {
                if (stopping) throw new InvalidOperationException("The desktop host lifetime is closing.");
                var transport = new QueuedInProcessTransport(persistent.Get(dataRoot, log));
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

    internal static async Task ShutdownAsync()
    {
        Task completion;
        lock (gate)
        {
            stopping = true;
            completion = Task.WhenAll(hosts.Select(host => host.DisposeAsync().AsTask()));
        }
        await completion.ConfigureAwait(false);
        lock (gate) persistent.Dispose();
    }

    private static async Task ReleaseAfterShutdownAsync(QueuedInProcessTransport transport)
    {
        await transport.ShutdownCompletion.ConfigureAwait(false);
        lock (gate) hosts.Remove(transport);
    }
}

using Marvel.Observability;
using Marvel.Protocol;
using Marvel.Server;
namespace Marvel.Desktop;

/// <summary>Shares one durable embedded authority across connections for the lifetime of the desktop process.</summary>
internal sealed class DesktopPersistentHosts : IDisposable
{
    private readonly Dictionary<string, IEngineEndpoint> endpoints = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IDisposable> lifetimes = [];

    internal IEngineEndpoint Get(string dataRoot, OperationalLog log)
    {
        string key = Path.GetFullPath(dataRoot);
        if (endpoints.TryGetValue(key, out var existing)) return existing;
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarvelLCG", "Saves");
        IDurableGameFactory factory = DatasetGameFactory.Load(dataRoot);
        var sessions = new FileSessionStore(Path.Combine(root, "Sessions"));
        IEngineEndpoint endpoint;
        if (factory is ICampaignGameFactory campaign)
        {
            var store = new FileCampaignStore(Path.Combine(root, "Campaigns"));
            lifetimes.Add(store);
            endpoint = new CampaignHost(campaign, store, sessions, log: log);
        }
        else endpoint = new EngineHost(factory, store: sessions, log: log);
        endpoints.Add(key, endpoint);
        return endpoint;
    }

    public void Dispose()
    {
        foreach (var lifetime in lifetimes) lifetime.Dispose();
        lifetimes.Clear();
        endpoints.Clear();
    }
}

using Marvel.Observability;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Transport;
namespace Marvel.Server.Host;

/// <summary>Owns durable server stores until every admitted socket exchange has drained.</summary>
internal sealed class HostedEndpoint : IDisposable
{
    private readonly FileCampaignStore? campaigns;
    internal SocketEngineServer Server { get; }
    internal HostedEndpoint(ServerOptions options, OperationalLog? log)
    {
        IDurableGameFactory factory = DatasetGameFactory.Load(options.DataRoot);
        var sessions = new FileSessionStore(options.SaveRoot);
        IEngineEndpoint endpoint;
        if (factory is ICampaignGameFactory campaign)
        {
            campaigns = new FileCampaignStore(Path.Combine(options.SaveRoot, ".campaigns"));
            endpoint = new CampaignHost(campaign, campaigns, sessions, options.Visibility, log);
        }
        else endpoint = new EngineHost(factory, visibility: options.Visibility, store: sessions, log: log);
        Server = new(endpoint, options.Address, options.Port);
    }
    public void Dispose() => campaigns?.Dispose();
}

using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Exposes the authored choices a client may use to open a game.</summary>
public interface ISetupDiscovery
{
    /// <summary>Returns the complete supported setup surface.</summary>
    SetupChoices DiscoverSetup();
}

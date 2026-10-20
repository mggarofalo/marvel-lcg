using Marvel.Session;

namespace Marvel.Server;

/// <summary>Offers only release-supported, content-checked predecessor replay migrations.</summary>
public interface IContentUpgradeFactory : IDurableGameFactory
{
    /// <summary>Returns no path for unknown or incompatible content identities.</summary>
    SessionContentUpgrade? ContentUpgrade(SessionCompatibility source);
}

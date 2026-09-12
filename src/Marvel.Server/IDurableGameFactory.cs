using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A game factory whose dataset identities make durable replay meaningful.</summary>
public interface IDurableGameFactory : IGameFactory
{
    /// <summary>The replay contracts and dataset hashes used by this factory.</summary>
    SessionCompatibility Compatibility { get; }
}

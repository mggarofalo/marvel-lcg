using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Issues transport capabilities that never enter deterministic game state.</summary>
public interface ISessionCapabilityIssuer
{
    /// <summary>Returns a new opaque capability.</summary>
    string Issue();
}

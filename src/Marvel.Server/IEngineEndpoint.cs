using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>The endpoint behind every transport.</summary>
public interface IEngineEndpoint
{
    /// <summary>Applies one protocol request synchronously.</summary>
    EngineResponse Exchange(EngineRequest request);
}

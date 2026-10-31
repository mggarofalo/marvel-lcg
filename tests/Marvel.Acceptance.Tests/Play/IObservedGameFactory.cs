using Marvel.Rules.Play;
using Marvel.Server;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>A host's game factory whose most recently created game a test can observe.</summary>
internal interface IObservedGameFactory : IDurableGameFactory
{
    /// <summary>The most recently created game.</summary>
    Game Game { get; }
}

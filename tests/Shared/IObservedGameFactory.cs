using Marvel.Rules.Play;
using Marvel.Server;

namespace Marvel.Tests;

/// <summary>A host's game factory whose most recently created game a test can observe.</summary>
internal interface IObservedGameFactory : IDurableGameFactory
{
    /// <summary>The most recently created game.</summary>
    Game Game { get; }
}

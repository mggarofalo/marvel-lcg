using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Observes a production deal without preparing or changing its state.</summary>
internal sealed class HulkRestartFactory : IObservedGameFactory
{
    private readonly DatasetGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
    public Game Game { get; private set; } = null!;
    public SessionCompatibility Compatibility => factory.Compatibility;

    public OpenedGame Create(GameSpecification specification)
    {
        var opened = factory.Create(specification);
        Game = opened.Game;
        return opened;
    }
}

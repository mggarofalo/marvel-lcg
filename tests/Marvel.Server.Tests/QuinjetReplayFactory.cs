using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;

namespace Marvel.Server.Tests;

internal sealed class QuinjetReplayFactory(DatasetGameFactory inner) : IDurableGameFactory
{
    public SessionCompatibility Compatibility => inner.Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Quinjet { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        Game = inner.Create(specification).Game;
        // A deterministic prepared scene tests the actual row without admitting the incomplete pack.
        var world = Game.State;
        Quinjet = world.CreateCard("03019", world.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(1), cardOwner: 1)).ObjectId;
        for (int answer = 0; answer < 30; answer++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing setup prompt");
            if (prompt.PublicKind == PublicDecisionKind.PlayerAction) return new(Game, []);
            Game.Resolve(prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("The deterministic prelude did not reach a player action");
    }
}

using Marvel.Cards.Run;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;

namespace Marvel.Server.Tests;

internal sealed class ManOutOfTimeReplayFactory(DatasetGameFactory inner) : IDurableGameFactory
{
    public SessionCompatibility Compatibility => inner.Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Obligation { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        Game = inner.Create(specification).Game;
        // A deterministic prepared scene exercises this row without admitting the incomplete pack.
        var world = Game.State;
        var source = world.CreateCard("03026", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        Obligation = source.ObjectId;
        ((AbilityRunner)world.ActionAbilities).ExecuteReveal(world, source, 0);
        for (int answer = 0; answer < 30; answer++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing setup prompt");
            if (prompt.Label.Contains("03026", StringComparison.Ordinal)) break;
            Game.Resolve(prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        if (!Game.Pending!.Label.Contains("03026", StringComparison.Ordinal))
            throw new InvalidOperationException("No obligation choice");
        var seat = world.Seats[0];
        foreach (var card in seat.Hand.Cards.ToArray()) World.MoveToTop(card, seat.Deck);
        for (int count = 0; count < 5; count++) world.CreateCard("01088", seat.Hand);
        return new(Game, []);
    }
}

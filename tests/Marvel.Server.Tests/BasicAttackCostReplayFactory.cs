using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;

namespace Marvel.Server.Tests;

internal sealed class BasicAttackCostReplayFactory(DatasetGameFactory inner, bool stunned) : IDurableGameFactory
{
    public SessionCompatibility Compatibility => inner.Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Ally { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        OpenedGame opened = inner.Create(specification);
        Game = opened.Game;
        var world = Game.State;
        // A synthetic lasting cost on a Core ally isolates persistence from pack admission.
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Ally = ally.ObjectId;
        world.Effects.Register(new(EffectSource.LastingEffect, BasicAttackCosts.DiscardField, 1,
            Card: ally.ObjectId, Affects: ally.ObjectId));
        if (stunned) Statuses.Give(world, ally, Statuses.Stunned);
        for (int answer = 0; answer < 30; answer++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing setup prompt");
            if (prompt.PublicKind == PublicDecisionKind.PlayerAction) return new(Game, []);
            Game.Resolve(prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("The deterministic prelude did not reach a player action");
    }
}

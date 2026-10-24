using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;

namespace Marvel.Server.Tests;

internal sealed class ExpertDefenseReplayFactory(DatasetGameFactory inner) : IDurableGameFactory
{
    public SessionCompatibility Compatibility => inner.Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Expert { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        Game = inner.Create(specification).Game;
        // A deterministic prepared Core scene exercises research content without product admission.
        var world = Game.State;
        var seat = world.Seats[0];
        seat.IdentityCard.TurnTo("01001a");
        Expert = world.CreateCard("03033", seat.Hand).ObjectId;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: villain.ObjectId, Seat: 0));
        for (int count = 0; count < 30; count++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing prepared defense prompt");
            if (prompt.Trigger == HeroDefenseTiming.Defends) return new(Game, []);
            Game.Resolve(prompt.Asking == Question.Defender
                ? Decision.Take(seat.IdentityCard.ObjectId)
                : prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("Prepared attack did not reach hero-defense interrupts");
    }
}

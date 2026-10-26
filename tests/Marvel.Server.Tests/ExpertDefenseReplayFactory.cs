using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;

namespace Marvel.Server.Tests;

internal sealed class ExpertDefenseReplayFactory(DatasetGameFactory inner, bool charged = true) : IDurableGameFactory
{
    public SessionCompatibility Compatibility => inner.Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Expert { get; private set; }
    internal int Backflip { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        Game = inner.Create(specification).Game;
        // A deterministic prepared Core attack isolates committed defense and its dependent choice.
        var world = Game.State;
        var seat = world.Seats[0];
        seat.IdentityCard.TurnTo("01001a");
        Expert = world.CreateCard("03033", seat.Hand).ObjectId;
        var backflip = world.Cards.First(card => card.FaceId == "01003");
        World.MoveToTop(backflip, seat.Hand);
        Backflip = backflip.ObjectId;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (charged)
        {
            // Rhino's two legal Core copies supply Charge's +3 ATK and a two-icon boost.
            // Against Spider-Man's DEF 3, damage is 4, or 1 after Expert Defense's +3 DEF.
            var charges = world.Cards.Where(card => card.FaceId == "01099").ToArray();
            Reveal.Resolve(world, world.Facts, charges[0], 0, []);
            World.MoveToTop(charges[1], world.AreaOf(DeckType.EncounterDeck));
        }
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

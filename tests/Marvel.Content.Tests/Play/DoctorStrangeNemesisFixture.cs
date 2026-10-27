using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical Doctor Strange nemesis programs on nonpublic research boards.</summary>
internal static class DoctorStrangeNemesisFixture
{
    internal static AbilityRunner Runner() => AuthoredCards.Runner();

    internal static World Board()
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("09001a,09001b", seat.Hero);
        world.Abilities = Runner();
        return world;
    }

    internal static Card Counterspell(World world)
    {
        var card = world.CreateCard("09030", world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard), 1, 1, Subject: card.ObjectId, Seat: 1));
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        return card;
    }
}



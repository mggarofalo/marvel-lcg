using System.Text.Json.Nodes;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical Mordo and Thoughtcasting with only Counterspell replaced by its explicit candidate.</summary>
internal static class DoctorStrangeNemesisFixture
{
    internal static JsonArray Rows() => JsonNode.Parse(File.ReadAllText(RepositoryPaths.Repository(
        "tests", "Marvel.Content.Tests", "Play", "Fixtures", "doctor-strange-final-nemesis.json")))!.AsArray();

    internal static AbilityRunner Runner() => DoctorStrangeAbilityFixture.WithReplacements(Rows());

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



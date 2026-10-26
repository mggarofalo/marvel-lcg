using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic prepared states use the actual identity, ally and Preparation rows.
// These focused search and payment checks do not claim a full-pack journey.
internal static class BlackWidowIdentitySearchFixture
{
    internal static AbilityRunner Runner() => AuthoredCards.Runner();

    internal static World Board(int players = 2, int widow = 1)
    {
        var world = ThorActualFixture.Board("06001a", players);
        var seat = world.Seats[widow];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("08001a,08001b", seat.Hero);
        seat.IdentityCard.TurnTo("08001b");
        world.Abilities = Runner();
        return world;
    }

    internal static Card PlayCard(World world, string face, int player)
    {
        // The isolated dependency keeps its printed one-copy limit; each play uses a fresh location.
        if (face == "08017")
            foreach (var existing in world.Cards.Where(card => card.FaceId == face
                && DeckTypes.IsInPlay(card.Area.Type) && CardControl.ControllerOf(world, card) == player).ToArray())
                Discard.Card(world, existing, "test dependency reset", []);
        var card = world.CreateCard(face, world.Seats[player].Hand);
        int price = (int)CardPayment.CostOf(world, world.Facts, world.Seats[player], card).Amount;
        int[] resources = Enumerable.Range(0, price)
            .Select(_ => world.CreateCard("01093", world.Seats[player].Hand).ObjectId).ToArray();
        CardPlay.Play(world, world.Facts, world.Abilities!, world.Seats[player], card, resources, []);
        return card;
    }

    internal static Prompt Prompt(World world) => Assert.IsType<Prompt>(
        Sequence.Work(world, world.Facts, world.Abilities!, []));

    internal static void Answer(World world, Prompt prompt, int id) =>
        Sequence.Answer(world, world.Facts, world.Abilities!, prompt, Decision.Take(id), []);

    internal static void Finish(World world) => Sequence.Finish(world, world.Facts, world.Abilities!, []);
}



using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

// Nonpublic characteristic composition probe; no setup/admission claim.
internal static class WidowEncounterFixture
{
    internal static AbilityRunner Runner() => AuthoredCards.Runner();

    internal static World Board(bool hero = true)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[1].IdentityCard = world.CreateCard("08001a,08001b", world.Seats[1].Hero);
        world.Seats[1].IdentityCard.TurnTo(hero ? "08001a" : "08001b");
        world.Abilities = Runner();
        return world;
    }

    internal static Card Upgrade(World world, string face = "08010", int seat = 1) =>
        world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(seat), cardOwner: seat));

}

using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

internal static class CaptainAmericaFixture
{
    // Prepared card-execution scene, not pack setup or starter-deck acceptance.
    // Both printed identity faces are captured so round accounting survives form changes.
    internal static (Game Game, World World) PlayingCaptain(Action<World> prepare, bool hero = true, bool twoPlayers = false) =>
        ActionAbilitySupportFixtures6.Playing(world =>
        {
            var seat = world.Seats[0];
            World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
            seat.IdentityCard = world.CreateCard("03001a,03001b", seat.Hero);
            if (!hero) seat.IdentityCard.TurnTo("03001b");
            prepare(world);
        }, heroes: twoPlayers ? ["spider_man", "captain_marvel"] : null, abilities: AuthoredCards.Runner());
}

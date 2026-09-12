using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityTypedFixedCountsFixtures
{
    internal static IReadOnlyList<string> FixedCountFaces { get; } =
        Array.AsReadOnly(["01002", "01003", "01004", "01005"]);
    internal static (World World, Card Source) FixedCountBoard(AbilityRunner runner, int players = 1)
    {
        var world = new World(CardCatalogData, players, seed: 5489)
        {
            Abilities = runner
        };
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        if (players == 2)
        {
            var second = world.CreateSeat("p1");
            second.IdentityCard = world.CreateCard("01010b", second.Hero);
        }

        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard(AuthoredCards.AuntMay, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        return (world, source);
    }
}

using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HydraPatrolTests : CoreCardTestBase
{
    [Fact]
    public void DefeatSearchesBothEncounterAreasAndEngagesOneMinionWithEachPlayer()
    {
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var first = world.CreateCard("04145", world.AreaOf(DeckType.EncounterDeck));
        var second = world.CreateCard("04145", world.AreaOf(DeckType.EncounterDiscardPile));
        var scheme = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        Defeat.Scheme(world, Cards, scheme, "test", [], by:1, recordOn:new Occurrence(1, []));
        for (int choices = 0; ; choices++)
        {
            var prompt = Sequence.Work(world, Cards, runner, []);
            if (prompt is null) break;
            Assert.True(choices < 6);
            var option = prompt.Affordances[0];
            var answer = option.Verb == "Order" ? new Decision(option.Id, option.Targets!.Legal.ToArray()) : Decision.Take(option.Id);
            Sequence.Answer(world, Cards, runner, prompt, answer, []);
        }
        Assert.Equal(DeckType.EngagedEnemiesArea, first.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, second.Area.Type);
        Assert.NotEqual(first.Area.PlayArea.Player, second.Area.PlayArea.Player);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
    }
}

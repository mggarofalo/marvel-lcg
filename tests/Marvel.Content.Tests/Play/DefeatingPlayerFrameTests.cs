using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DefeatingPlayerFrameTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void DefeatBindsOnlyTheCreditedPlayersChoiceAndContinuation(int player)
    {
        // Prison Camps: "The player who defeated this scheme searches their deck
        // and discard pile for an ally". The frame binds both choices and "you".
        var world = Hero("01001a", 2);
        var runner = Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("04141", world.AreaOf(DeckType.SideSchemesArea));
        var first = world.CreateCard("01088", world.Seats[0].Hand);
        var second = world.CreateCard("01088", world.Seats[1].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[1].Deck);
        world.CreateCard("01090", world.Seats[0].Deck);
        world.CreateCard("01090", world.Seats[1].Deck);
        Defeat.Scheme(world, Cards, scheme, "test", [], by: player,
            recordOn: new Occurrence(1, []));

        var choice = Sequence.Work(world, Cards, runner, [])!;
        Assert.Equal(player, choice.Player);
        Assert.NotEqual(Question.Order, choice.Asking);
        var option = Assert.Single(choice.Affordances);
        Assert.Equal(player == 0 ? first.ObjectId : second.ObjectId, option.AnchorId);
        Sequence.Answer(world, Cards, runner, choice,
            Decision.Take(option.Id), []);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(DeckType.DiscardPile, (player == 0 ? first : second).Area.Type);
        Assert.Single(world.Seats[player].Hand.Cards);
        Assert.Equal(player == 0 ? second : first,
            Assert.Single(world.Seats[1 - player].Hand.Cards));
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
    }

    [Fact]
    public void MissingDefeatCreditRaisesInsteadOfChoosingTheFirstPlayer()
    {
        var world = Hero("01001a", 2);
        var runner = Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("04141", world.AreaOf(DeckType.SideSchemesArea));
        Defeat.Scheme(world, Cards, scheme, "test", [], recordOn: new Occurrence(1, []));
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Finish(world, Cards, runner, []));
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"04141","abilities":[{
          "trigger":{"timing":"WhenDefeated","event":"WhenCardDefeated","subject":"this"},
          "effect":{"eachPlayer":{"player":"defeater","effect":{"seq":[
            {"chooseCard":{"from":{"cardsIn":{"area":"yourHand"}},"effect":{"discard":"chosen"}}},
            {"draw":{"player":"you","count":1}}
          ]}}}
        }]}]}
        """));
}




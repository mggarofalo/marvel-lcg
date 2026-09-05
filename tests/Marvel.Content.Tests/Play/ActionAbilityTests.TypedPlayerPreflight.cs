using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData("drawToHandSize", false)]
    [InlineData("drawToHandSize", true)]
    [InlineData("drawToPrintedHandSize", false)]
    [InlineData("drawToPrintedHandSize", true)]
    public void HandSizeOptionChecksItsNamedPlayerRatherThanTheResolver(string operation, bool full)
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$"""
            {"choose":{"options":[{"{{{operation}}}":"firstPlayer"},
              {"heal":{"card":{"query":"villain"},"amount":1}}]}}
            """, cost: """{"exhaust":"this"}""");
        var (world, source) = FixedCountBoard(runner, players: 2);
        world.FirstPlayer = 1;
        var recipient = world.Seats[1];
        int size = (int)world.Facts.PrintedValue(recipient.IdentityCard.FaceId, "HS", world.Players);
        int held = full ? size : size - 1;
        for (int index = 0; index < held; index++) world.CreateCard("01002", recipient.Hand);
        var top = world.CreateCard("01003", recipient.Deck);
        world.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);

        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;

        int[] expected = full ? [1] : [0, 1];
        Assert.Equal(expected, prompt.Affordances.Select(option => option.Id));
        Assert.Equal(0, prompt.Player);
        if (!full)
        {
            runner.Chose(world, source, 0, choice.Index, Decision.Take(0), choice.Tier);
            Assert.Equal(size, recipient.Hand.Cards.Count);
            Assert.Contains(top, recipient.Hand.Cards);
            Assert.Empty(recipient.Deck.Cards);
        }
        else
        {
            Assert.Throws<RulesNotImplementedException>(() => runner.Chose(
                world, source, 0, choice.Index, Decision.Take(0), choice.Tier));
            Assert.Equal(size, recipient.Hand.Cards.Count);
            Assert.Equal([top], recipient.Deck.Cards);
        }
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("firstPlayer")]
    [InlineData("each")]
    public void RandomDiscardOptionUsesItsCompiledPlayerSelection(string players)
    {
        var (runner, fields) = MutableNumericRunner("discardAtRandom",
            $$"""{"player":"{{players}}","count":1}""", choice: true);
        var (world, source) = FixedCountBoard(runner, players: 2);
        world.FirstPlayer = 1;
        var card = world.CreateCard("01002", world.Seats[1].Hand);
        var remaining = world.CreateCard("01003", world.Seats[1].Deck);
        // Neither the resolving player's empty hand nor a later map edit can
        // hide the option that will discard the named player's card.
        fields["player"] = new AbilityValue.Word("you");

        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;
        Assert.Equal(0, Assert.Single(prompt.Affordances).Id);
        runner.Chose(world, source, 0, choice.Index, Decision.Take(0), choice.Tier);

        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.Equal([card], world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1).Cards);
        Assert.Equal([remaining], world.Seats[1].Deck.Cards);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(1, world.Random.Generator.WordsConsumed);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APostCompilationFieldCannotRedirectAChoiceToAnotherPlayer(bool cards)
    {
        string operation = cards ? "chooseCard" : "choose";
        string arguments = cards
            ? """{"from":{"query":"villain"},"effect":{"heal":{"card":"chosen","amount":1}}}"""
            : """
                {"options":[{"heal":{"card":{"query":"villain"},"amount":1}},
                 {"draw":{"player":"you","count":1}}]}
                """;
        var (runner, fields) = MutableEffectRunner(operation, arguments, false);
        var (world, source) = FixedCountBoard(runner, players: 2);
        world.FirstPlayer = 1;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(1);
        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        fields["chooser"] = new AbilityValue.Word("firstPlayer");

        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;

        // Engine contract: admitted choices belong to their resolving player.
        Assert.Equal(0, prompt.Player);
        var offered = Assert.Single(prompt.Affordances);
        runner.Chose(world, source, 0, choice.Index, Decision.Take(offered.Id), choice.Tier);
        Assert.Equal(0, villain.Damage);
        Assert.False(source.Ready);
    }
}

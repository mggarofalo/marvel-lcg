using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IndependentGamesKeepDelayedActivationsSeparate(bool shareRunner, bool reverseCompletion)
    {
        // Activation and card ids are game-local. Completing an id in one
        // board must neither run nor consume another board's delayed text.
        var program = GameOwnershipProgram("""
            {"afterActivation":{"effect":{
              "placeCounters":{"card":"this","counter":"test","count":1}}}}
            """);
        var firstRunner = new AbilityRunner(program);
        var secondRunner = shareRunner ? firstRunner : new AbilityRunner(program);
        static (World World, Card Source, AbilityRunner Runner) Board(AbilityRunner runner)
        {
            var (world, source) = FixedCountBoard(runner);
            return (world, source, runner);
        }
        var first = Board(firstRunner);
        var second = Board(secondRunner);
        foreach (var (world, source, runner) in new[] { first, second })
        {
            world.Activation = new EnemyActivation(
                world.TheCardIn(DeckType.VillainArea)!.ObjectId, 0, Attacking: true, Id: 41);
            var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
            runner.Act(world, action, [], []);
            Assert.DoesNotContain("c_test", source.Tokens.Keys);
        }

        var boards = reverseCompletion ? new[] { second, first } : [first, second];
        string untouched = boards[1].World.Digest().Canonical();
        boards[0].Runner.ActivationCompleted(boards[0].World, boards[0].World.Activation!);

        Assert.Equal(1, boards[0].Source.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(untouched, boards[1].World.Digest().Canonical());
        Assert.DoesNotContain("c_test", boards[1].Source.Tokens.Keys);

        boards[1].Runner.ActivationCompleted(boards[1].World, boards[1].World.Activation!);
        Assert.Equal(1, boards[1].Source.Tokens.GetValueOrDefault("c_test"));
        foreach (var (world, source, runner) in boards)
        {
            runner.ActivationCompleted(world, world.Activation!);
            Assert.Equal(1, source.Tokens.GetValueOrDefault("c_test"));
            Assert.False(world.Agenda.IsBusy);
            Assert.Equal(0, world.Random.Generator.WordsConsumed);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndependentGamesResumeTheirOwnChoiceResults(bool shareRunner)
    {
        // Each continuation carries its own earlier results. Interleaving
        // another game must not replace what "healed this way" reads.
        var program = GameOwnershipProgram("""
            {"seq":[
              {"heal":{"card":"you","amount":3}},
              {"choose":{"options":[{"exhaust":"this"},{"ready":"this"}]}},
              {"if":{"test":{"atLeast":{"value":{"result":"healed"},"count":2}},
                "then":{"placeCounters":{"card":"this","counter":"test","count":2}},
                "else":{"placeCounters":{"card":"this","counter":"test","count":1}}}}
            ]}
            """);
        var firstRunner = new AbilityRunner(program);
        var secondRunner = shareRunner ? firstRunner : new AbilityRunner(program);
        static (Game Game, World World, Card Source) Board(AbilityRunner runner, int damage)
        {
            Card? source = null;
            var (game, world) = Playing(board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(damage);
            }, abilities: runner);
            return (game, world, source!);
        }
        var first = Board(firstRunner, 1);
        var second = Board(secondRunner, 2);
        foreach (var (game, world, source) in new[] { first, second })
        {
            var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source.ObjectId);
            game.Resolve(Decision.Take(action.Id));
            Assert.Equal(Question.Option, game.Pending!.Asking);
            Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        }

        string pendingFirst = first.World.Digest().Canonical();
        second.Game.Resolve(Decision.Take(0));
        Assert.Equal(2, second.Source.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(pendingFirst, first.World.Digest().Canonical());
        Assert.Equal(Question.Option, first.Game.Pending!.Asking);

        first.Game.Resolve(Decision.Take(0));
        Assert.Equal(1, first.Source.Tokens.GetValueOrDefault("c_test"));
        foreach (var (game, world, source) in new[] { first, second })
        {
            Assert.False(source.Ready);
            Assert.Equal(Question.TurnOption, game.Pending!.Asking);
            Assert.False(world.Agenda.IsBusy);
        }
    }

    private static AbilityProgram GameOwnershipProgram(string effect) =>
        AbilityLowering.Book(AbilityCatalog.Parse($$$$$$"""
            {"cards":[{"card":"{{{{{{AuthoredCards.AuntMay}}}}}}","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{{{{{{effect}}}}}}
            }]}]}
            """));
}

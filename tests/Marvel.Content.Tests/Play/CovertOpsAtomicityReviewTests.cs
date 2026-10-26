using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowEventCompositionFixture;

namespace Marvel.Content.Tests.Play;

public sealed class CovertOpsAtomicityReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsufficientPaymentLeavesBothBranchStatesUntouchedAndAllowsLegalRetry(bool schemeAvailable)
    {
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", (schemeAvailable ? 6 : 0) - main.Tokens.GetValueOrDefault("k_threat"));
        var source = world.CreateCard("08003", world.Seats[1].Hand);
        int[] payment = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01093", world.Seats[1].Hand).ObjectId).ToArray();
        var runner = (AbilityRunner)world.Abilities!;
        var action = new PendingAbility(source.ObjectId, AbilityType.Action, 1);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, payment[..2], [])));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Agenda.Current);
        runner.ExecuteAction(world, new PlayerAction(action, payment, []));
        if (schemeAvailable)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            string paid = world.Digest().Canonical();
            string question = JsonSerializer.Serialize(prompt);
            var occurrence = world.Agenda.Occurrence;
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, prompt,
                Decision.Take(world.TheCardIn(DeckType.VillainArea)!.ObjectId), []));
            Assert.Equal(paid, world.Digest().Canonical());
            Assert.Same(occurrence, world.Agenda.Occurrence);
            Assert.Equal(question, JsonSerializer.Serialize(Sequence.Work(world, world.Facts, runner, [])));
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(main.ObjectId), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(schemeAvailable ? 2 : 0, main.Tokens["k_threat"]);
        Assert.True(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}

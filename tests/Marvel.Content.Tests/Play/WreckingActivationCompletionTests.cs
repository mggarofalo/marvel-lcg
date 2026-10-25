using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingActivationCompletionTests
{
    [Fact]
    public void PreventingAllAttackDamageDoesNotCancelCrowbarTossContinuation()
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07012", 0);
        for (int index = 0; index < groups.Length - 1; index++) groups[index].Scheme.PlaceTokens("k_threat", 5);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        Finish(world, source, AuthoredCards.Runner());
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Same(groups[3], world.EncounterTopology!.Active);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    public void SeparateActivationReceiptsCombineWithoutRetainingEarlierSuccess(bool firstCancelled, bool secondCancelled)
    {
        // Synthetic conjunction: rr:then requires the preceding effect to "fully resolve".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07012", 0);
        if (firstCancelled) Statuses.Give(world, groups[0].Villain, Statuses.Stunned);
        if (secondCancelled) Statuses.Give(world, groups[2].Villain, Statuses.Confused);
        var runner = WithEffect("""
        {"then":{"effect":{"seq":[
          {"enemyAttacks":{"enemies":{"titled":"Wrecker"},"against":"you"}},
          {"enemySchemes":{"enemies":{"titled":"Piledriver"},"against":"you"}}]},
          "then":{"giveStatus":{"card":"you","status":"tough"}}}}
        """);
        Finish(world, source, runner);
        Assert.Equal(!firstCancelled && !secondCancelled,
            Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(firstCancelled ? 0 : 4, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(secondCancelled ? 0 : 2, groups[2].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ACollectionRequiresEveryRequestedActivationToBeMade(int cancelled)
    {
        var (world, _, source) = WreckingEncounterCardFixture.Create("07012", 0);
        Card[] minions = [.. Enumerable.Range(0, 2).Select(_ => world.CreateCard(AuthoredCards.Shocker,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))))];
        foreach (var minion in minions.Take(cancelled)) Statuses.Give(world, minion, Statuses.Confused);
        var runner = WithEffect("""
        {"then":{"effect":{"enemySchemes":{"enemies":{"query":"minionsEngagedWithYou"}}},
          "then":{"giveStatus":{"card":"you","status":"tough"}}}}
        """);
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(Question.Order, prompt.Asking);
        var order = Assert.Single(prompt.Affordances);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(order.Id, [.. minions.Select(card => card.ObjectId)], []), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(cancelled == 0, Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(2 - cancelled, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    private static AbilityRunner WithEffect(string effect)
    {
        var document = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var row = document["cards"]!.AsArray().Single(card => card!["card"]!.GetValue<string>() == "07012")!;
        row["abilities"]![0]!["effect"] = JsonNode.Parse(effect);
        return new AbilityRunner(AbilityCatalog.Parse(document.ToJsonString()));
    }

    private static void Finish(World world, Card source, AbilityRunner runner)
    {
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return;
            Assert.True(step < 15);
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
    }
}

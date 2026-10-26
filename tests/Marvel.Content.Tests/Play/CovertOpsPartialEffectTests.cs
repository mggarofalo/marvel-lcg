using Marvel.Cards.Run;
using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowEventCompositionFixture;

namespace Marvel.Content.Tests.Play;

public sealed class CovertOpsPartialEffectTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.3")]
    [Rule("rr:labeled-ability.4")]
    public void IndependentSentenceCreatesOneThwartWithNoSchemeEvenUnderPatrol(bool patrol)
    {
        // "that ability is considered to be a thwart made by that player's identity."
        // Ruling 0fc2a7cc9197e127 explicitly includes a patrol-blocked main scheme.
        var world = Board();
        world.Abilities = Runner(observeThwart: true);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", (patrol ? 6 : 0) - main.Tokens.GetValueOrDefault("k_threat"));
        if (patrol) world.CreateCard("16119", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var source = PlayEvent(world, "08003");
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities!, []));
        Assert.Equal(1, prompt.Player);
        var occurrence = world.Agenda.Occurrence!;
        Assert.Equal(world.Seats[1].IdentityCard.ObjectId, occurrence.Actor);
        Assert.Equal(-1, occurrence.Target);
        Assert.Equal(-1, occurrence.Subject);
        Assert.Null(occurrence.TargetFacts);
        Assert.True(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.Equal(patrol ? 6 : 0, main.Tokens["k_threat"]);
        Sequence.Answer(world, world.Facts, world.Abilities!, prompt,
            Decision.Take(Assert.Single(prompt.Affordances).Id), []);
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void RepeatedOffersLeaveRandomnessStateAndFutureResolutionUnchanged()
    {
        var probed = Resolve(extraQueries: true);
        var control = Resolve(extraQueries: false);
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, probed.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("attack")]
    [InlineData("automatic")]
    [InlineData("draw")]
    public void UnsupportedSchemeLessPowerShapesStillRaiseAtLowering(string shape)
    {
        string body = shape == "draw" ? "{\"draw\":{\"player\":\"you\",\"count\":1}}"
            : "{\"giveStatus\":{\"card\":{\"query\":\"villain\"},\"status\":\"confused\"}}";
        string verb = shape == "attack" ? "attack" : "thwart";
        string automatic = shape == "automatic" ? ",\"automaticTarget\":true" : "";
        string rows = "{\"cards\":[{\"card\":\"08003\",\"abilities\":[{"
            + "\"trigger\":{\"event\":\"WhenActionTriggered\",\"timing\":\"Action\",\"subject\":\"this\"},"
            + "\"effect\":{\"" + verb + "\":{\"effect\":" + body + automatic + "}}}]}]}";
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(rows)));
    }

    [Fact]
    public void DifferentFallbackDoesNotAcquireCorrelatedBranchAdmission()
    {
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", -main.Tokens.GetValueOrDefault("k_threat"));
        string rows = Rows;
        int branch = rows.IndexOf("\"else\"", StringComparison.Ordinal);
        rows = rows[..branch] + rows[branch..].Replace("confused", "stunned", StringComparison.Ordinal);
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(rows));
        Assert.Throws<RulesNotImplementedException>(() => PlayEvent(world, "08003"));
        Assert.Equal(4, world.Seats[1].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned));
    }

    private static World Resolve(bool extraQueries)
    {
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", -main.Tokens.GetValueOrDefault("k_threat"));
        var source = world.CreateCard("08003", world.Seats[1].Hand);
        int[] payments = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01093", world.Seats[1].Hand).ObjectId).ToArray();
        string digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var runner = (AbilityRunner)world.Abilities!;
        if (extraQueries)
            for (int query = 0; query < 3; query++)
                Assert.Contains(runner.Actions(world, 1), action => action.Card == source.ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Agenda.Current);
        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 1), payments, []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        return world;
    }
}

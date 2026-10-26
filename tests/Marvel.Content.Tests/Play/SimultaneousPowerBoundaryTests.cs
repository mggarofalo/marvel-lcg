using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SimultaneousPowerBoundaryTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:and")]
    public void SyntheticThwartRetainsRemovalAndPrivateInspection(bool inspectFirst)
    {
        // "Two or more effects within an ability resolve simultaneously."
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"thwart":{"target":{"query":"mainScheme"},"effect":{"seq":[{"and":[
              {"removeThreat":{"scheme":"chosen","amount":3}},
              {"inspectEncounterTop":{"count":1,"result":"boostIconsLookedAt"}}
            ]},{"removeThreat":{"scheme":"chosen","amount":{"result":"boostIconsLookedAt"}}}]}}}
            """, includeAuthored: true);
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var source = world.CreateCard(AuthoredCards.AuntMay,
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        var top = world.AreaOf(DeckType.EncounterDeck).Cards[^1];
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var order = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Sequence.Answer(world, world.Facts, runner, order,
            Decision.Take(order.Affordances[0].Id, inspectFirst ? [1, 0] : [0, 1], []), events);
        var inspection = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal("Acknowledge", Assert.Single(inspection.Affordances).Verb);
        Assert.False(top.FaceUp);
        Sequence.Answer(world, world.Facts, runner, inspection,
            Decision.Take(inspection.Affordances[0].Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(1, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.EncounterDeck, top.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ThirdSimultaneousMutationStillRaisesBeforeCost()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"and":[
              {"dealAttackDamage":{"cards":"chosen","amount":1}},
              {"discardTop":{"from":"yourDeck","count":1}},
              {"draw":{"player":"you","count":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var source = world.CreateCard(AuthoredCards.AuntMay,
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        var before = System.Text.Json.JsonSerializer.Serialize(world.Digest());
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("suspends inside", error.Message);
        Assert.True(source.Ready);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(world.Digest()));
    }
    [Theory]
    [InlineData("{\"draw\":{\"player\":\"you\",\"count\":1}}")]
    [InlineData("{\"dealAttackDamage\":{\"cards\":\"chosen\",\"amount\":1}}")]
    public void UnsupportedDiscardContinuationRaisesBeforeCost(string continuation)
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"and":[{"dealAttackDamage":{"cards":"chosen","amount":1}},
                {"discardTop":{"from":"yourDeck","count":1}}]},
              {"if":{"test":{"exists":{"cardsIn":{"area":"encounterDeck"}}},
                "then":CONTINUATION}}
            ]}}}
            """.Replace("CONTINUATION", continuation, StringComparison.Ordinal),
            cost: """{"exhaust":"this"}""", includeAuthored: true);
        var world = ThorActualFixture.Board("06001a");
        var source = world.CreateCard(AuthoredCards.AuntMay,
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("suspends inside", error.Message);
        Assert.True(source.Ready);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }
}

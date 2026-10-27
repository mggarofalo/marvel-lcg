using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Clea defeat and explicitly synthetic alternative-program boundaries.</summary>
public sealed class DoctorStrangeSupportPrerequisiteTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CleaOwnerCompositionUsesPhysicalOwnerDeckAndSurvivesDefeat(bool accept, bool foreignControl)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var source = world.CreateCard("09013", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(foreignControl ? 1 : 0), cardOwner: 0));
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, world.Facts, new(source, source, 2, "Clea probe", "Damage"), events);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.True(prompt.Cancellable);
        Sequence.Answer(world, world.Facts, runner, prompt,
            accept ? Decision.Take(Assert.Single(prompt.Affordances).Id) : Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        // Control: the accepted shuffle survives ordinary defeat cleanup.
        Assert.Equal(accept ? DeckType.PlayerDeck : DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(0, source.Area.PlayArea.Player);
        Assert.Equal(1, events.OfType<CardsMoved>().Count(move =>
            move.Cards.Any(card => card.Card == source.ObjectId)));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StatusRemovalCompositionRemovesEveryMatchingPhysicalCard(bool twoStatuses)
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = Runner("""
            {"card":"09019","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"removeStatuses":{"cards":"you","statuses":["stunned","confused","tough"]}}}]}
            """);
        world.Abilities = runner;
        var source = world.CreateCard("09019", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Stunned);
        if (twoStatuses) Statuses.Give(world, hero, Statuses.Tough);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        // Boundary: two statuses produce two removals without the printed choice of one.
        Assert.Equal(twoStatuses ? 2 : 1, events.Events.OfType<CardsMoved>().Count(item => item.Verb == "Remove_Status"));
    }

    [Fact]
    public void UnknownStatusChoiceSpellingIsRejected()
    {
        var error = Assert.ThrowsAny<Exception>(() => Runner("""
            {"card":"09019","startingCounters":{"type":"medical","count":3,"uses":true},"abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"seq":[{"exhaust":"this"},{"removeCounters":{"card":"this","counter":"medical","count":1}}]},
              "effect":{"chooseCard":{"from":{"query":"heroes"},"effect":{"and":[
                {"heal":{"card":"chosen","amount":1}},
                {"chooseStatusOn":{"card":"chosen","count":1,"effect":{"discard":"chosen"}}}
              ]}}}}]}
            """));
        Assert.Contains("chooseStatusOn", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("09013", "{\"shuffleInto\":{\"cards\":\"this\",\"deck\":\"unknownOwnerDeck\"}}", "unknownOwnerDeck")]
    [InlineData("09039", "{\"reduceUpgradeCostOn\":{\"card\":\"this\",\"amount\":1}}", "reduceUpgradeCostOn")]
    public void UnknownDestinationAndUpgradeOperationSpellingsAreRejected(string face, string effect, string word)
    {
        var root = JsonNode.Parse("""{"card":"FACE","abilities":[{"trigger":{"timing":"Constant","subject":"this"},"effect":EFFECT}]}"""
            .Replace("FACE", face, StringComparison.Ordinal).Replace("EFFECT", effect, StringComparison.Ordinal))!;
        var error = Assert.ThrowsAny<Exception>(() => Runner(root.ToJsonString()));
        Assert.Contains(word, error.Message, StringComparison.Ordinal);
        Assert.NotNull(Runner("""{"card":"09039","abilities":[]}"""));
    }

    private static AbilityRunner Runner(string row)
    {
        // Deliberately alternate effects replace the production row only in this synthetic fixture.
        return DoctorStrangeAbilityFixture.WithReplacements(new JsonArray(JsonNode.Parse(row)));
    }
}

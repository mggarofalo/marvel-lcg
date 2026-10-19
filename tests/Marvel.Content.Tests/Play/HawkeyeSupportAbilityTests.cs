using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HawkeyeSupportAbilityTests : CoreCardTestBase
{
    [Theory]
    [InlineData("04001a", 3, 0)]
    [InlineData("04001b", 0, 3)]
    public void SniperShotResolvesOnlyTheRevealingPlayersCurrentForm(string identity, long damage, long threat)
    {
        var world = Hero(identity);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var shot = world.CreateCard("04030", world.AreaOf(DeckType.RevealingArea));
        var events = runner.ExecuteReveal(world, shot, 0).Events.ToList();
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(threat, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void ExpertMarksmanPaysForAnArrowAndExhaustsOnlyWhenItsResourceIsSpent()
    {
        var world = Hero("04001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var marksman = world.CreateCard("04010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("04002", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var arrow = world.CreateCard("04008", world.Seats[0].Hand);
        var nonArrow = world.CreateCard("01005", world.Seats[0].Hand);
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        main.PlaceTokens("k_threat", 5);
        Assert.Contains(CardPayment.Generators(world, Cards, world.Seats[0], runner, arrow), item => item.Effect == marksman.ObjectId);
        Assert.DoesNotContain(CardPayment.Generators(world, Cards, world.Seats[0], runner, nonArrow), item => item.Effect == marksman.ObjectId);
        Assert.True(marksman.Ready);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == arrow.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [marksman.ObjectId], [])).Events.ToList();
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(main.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.False(marksman.Ready);
        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.Same(world.Seats[0].Hand, nonArrow.Area);
    }

    [Theory]
    [InlineData("01088", 2)]
    [InlineData("01055", 1)]
    public void HawkeyeAllyDiscardsPrintedIconsForNonAttackDamage(string face, long expected)
    {
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ally = world.CreateCard("04011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var payment = world.CreateCard(face, world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Deck);
        string before = world.Digest().Canonical();
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == ally.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Throws<RulesNotImplementedException>(() =>
            runner.ExecuteAction(world, new PlayerAction(action, [], [ally.ObjectId])));
        Assert.Equal(before, world.Digest().Canonical());
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [payment.ObjectId])).Events.ToList();
        var target = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, target, Decision.Take(villain.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(expected, villain.Damage);
        Assert.False(ally.Ready);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.DoesNotContain(events, item => item is FieldSet field && field.Verb == BasicPowers.AttackVerb);
    }

    [Fact]
    public void ClintFindsHisBowFromDiscardAndPaysOnlyOncePerPhase()
    {
        var world = Hero("04001b");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var bow = world.CreateCard("04002", discard);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == world.Seats[0].IdentityCard.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(bow.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Same(world.Seats[0].Hand, bow.Area);
        Assert.Same(discard, payment.Area);
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == world.Seats[0].IdentityCard.ObjectId);
    }

    [Fact]
    public void GoliathGrowsOnceAndLeavesAtPhaseEnd()
    {
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ally = world.CreateCard("04013", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        for (int index = 0; index < 10; index++) world.CreateCard("01088", world.Seats[0].Deck);
        long printed = Modified(world, ally, "attack");
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == ally.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        Assert.Equal(printed + 4, Modified(world, ally, "attack"));
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == ally.ObjectId);
        DelayedEffects.Occur(world, "WhenPlayerPhaseEnds", -1, events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }

    [Fact]
    public void SkyCycleGrantsAerialAndReadiesItsAttachedAvenger()
    {
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ally = world.CreateCard("04014", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var cycle = world.CreateCard("04015", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, 0));
        Reveal.EnterPlay(world, Cards, cycle, [], abilities: runner);
        ally.Exhaust();
        Assert.Contains("AERIAL", Traits.Of(world, ally, Cards));
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == cycle.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.True(ally.Ready);
        Assert.False(cycle.Ready);
        Discard.Card(world, cycle, "test", []);
        Assert.DoesNotContain("AERIAL", Traits.Of(world, ally, Cards));
    }

    [Fact]
    public void TeamTrainingAddsHealthOnlyToItsControllersAllies()
    {
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ours = world.CreateCard("04014", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var theirs = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        long ourHealth = Modified(world, ours, "health");
        long theirHealth = Modified(world, theirs, "health");
        var training = world.CreateCard("04016", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Reveal.EnterPlay(world, Cards, training, [], abilities: runner);
        Assert.Equal(ourHealth + 1, Modified(world, ours, "health"));
        Assert.Equal(theirHealth, Modified(world, theirs, "health"));
        Discard.Card(world, training, "test", []);
        Assert.Equal(ourHealth, Modified(world, ours, "health"));
    }
}

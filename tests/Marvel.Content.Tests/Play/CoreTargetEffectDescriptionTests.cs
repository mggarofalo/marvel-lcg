using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// These are engine presentation choices, not additional game rules.
public sealed class CoreTargetEffectDescriptionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(0, "14/14 → 6/14 HP")]
    [InlineData(2, "14/14 → 4/14 HP")]
    public void WebKickNamesItsAttackAndSeparatesPrintedDamageFromTheLivePreview(
        int modifier, string preview)
    {
        var world = Board();
        var kick = world.CreateCard("01005", world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        // A contract fixture applies a modifier to a reachable Core event.
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "eventDamage", Amount: modifier,
            Card: kick.ObjectId, Affects: kick.ObjectId));
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == kick.ObjectId);
        Affordance offered = runner.Describe(world, action);

        Assert.True(offered.DeferredTargetSelection);
        Assert.Null(offered.Targets);
        Assert.Contains("Choose an enemy to attack", offered.Description);
        Assert.Contains("Printed damage: 8", offered.Description);
        Assert.Contains("modifiers, prevention and later effects can change the result", offered.Description);

        var events = runner.Act(world, action, [genius.ObjectId, energy.ObjectId], []).ToList();
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));

        Assert.Equal("Swinging Web Kick: choose an enemy to attack", prompt.DisplayQuestion);
        Assert.Equal(offered.Description, prompt.Description);
        Affordance target = Assert.Single(prompt.Affordances);
        Assert.Contains(preview, target.Description);
        Assert.Equal("Attack Rhino", target.CommitLabel);
        Assert.False(target.DeferredTargetSelection);
        Assert.Equal(DeckType.DiscardPile, genius.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.False(prompt.Cancellable);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(enemy.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(8 + modifier, enemy.Damage);
    }

    [Fact]
    public void TacTeamNamesOrdinaryDamageWithoutCallingItAnAttack()
    {
        var world = Board();
        var team = world.CreateCard("01056",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.EnterPlay(world, Cards, team, [], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == team.ObjectId);

        runner.Act(world, action, [], []);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));

        Assert.Equal("Tac Team: choose an enemy to deal damage to", prompt.DisplayQuestion);
        Assert.Contains("Printed damage: 2", prompt.Description);
        Assert.DoesNotContain("attack", prompt.Description);
        Assert.Contains("14/14 → 12/14 HP", Assert.Single(prompt.Affordances).Description);
        Assert.Equal("Deal damage to Rhino", Assert.Single(prompt.Affordances).CommitLabel);
    }

    [Fact]
    public void SurveillanceTeamNamesItsPrintedRemovalAndLiveSchemeResult()
    {
        var world = Board();
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 3);
        var team = world.CreateCard("01064",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.EnterPlay(world, Cards, team, [], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == team.ObjectId);

        runner.Act(world, action, [], []);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));

        Assert.Equal("Surveillance Team: choose a scheme to remove threat from", prompt.DisplayQuestion);
        Assert.Contains("Printed threat removal: 1", prompt.Description);
        Assert.Contains("3/7 → 2/7 threat", Assert.Single(prompt.Affordances).Description);
        Assert.Equal("Remove threat from The Break-In!", Assert.Single(prompt.Affordances).CommitLabel);
    }

    [Fact]
    public void ForJusticeNamesThreatRemovalWithoutInventingAConstantForItsPaidResourceBranch()
    {
        var world = Board();
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 5);
        var justice = world.CreateCard("01060", world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == justice.ObjectId);

        runner.Act(world, action, [genius.ObjectId], []);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));

        Assert.Equal("For Justice!: choose a scheme to remove threat from", prompt.DisplayQuestion);
        Assert.Equal("Choose a scheme to remove threat from", prompt.Description);
        Assert.Equal("Remove threat from The Break-In!", Assert.Single(prompt.Affordances).Description);
        Assert.Equal("Remove threat from The Break-In!", Assert.Single(prompt.Affordances).CommitLabel);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(scheme.ObjectId), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(1, scheme.Tokens["k_threat"]);
    }

    [Fact]
    public void UntargetedCoreEventDoesNotPromiseADeferredTargetChoice()
    {
        var world = Board();
        var stomp = world.CreateCard("01022", world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == stomp.ObjectId);

        Affordance offered = runner.Describe(world, action);

        Assert.True(offered.PlaysCard);
        Assert.False(offered.DeferredTargetSelection);
        runner.Act(world, action, [genius.ObjectId], []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, stomp.Area.Type);
    }

    [Fact]
    public void ASingleStepSequenceRetainsTheSameSeparateChoiceBoundary()
    {
        // Synthetic structural fixture: wrapping one effect cannot change when
        // its choice is answered, or the explanation of that choice.
        var runner = Runner("01005", "Action", """
            { "seq": [{ "chooseCard": {
                "from": { "query": "enemies" },
                "effect": { "dealDamage": { "cards": "chosen", "amount": 2 } }
            } }] }
            """);
        var world = Board();
        world.Abilities = runner;
        var source = world.CreateCard("01005", world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == source.ObjectId);

        Affordance offered = runner.Describe(world, action);
        runner.Act(world, action, [genius.ObjectId, energy.ObjectId], []);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));

        Assert.True(offered.DeferredTargetSelection);
        Assert.Equal(offered.Description, prompt.Description);
        Assert.Contains("Printed damage: 2", prompt.Description);
        Assert.Contains("14/14 → 12/14 HP", Assert.Single(prompt.Affordances).Description);
        Assert.Equal("Deal damage to Rhino", Assert.Single(prompt.Affordances).CommitLabel);
    }

    private static World Board()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01087", seat.Deck);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        return world;
    }
}

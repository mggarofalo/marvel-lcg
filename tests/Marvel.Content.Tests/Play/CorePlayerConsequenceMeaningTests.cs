using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CorePlayerConsequenceMeaningTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void DaggersNamesThePlayerAndCurrentSpecialDamageBeforeSelection(bool final, int amount)
    {
        // Printed Core01046 chooses a player, damages the villain and each enemy
        // engaged with that player; its final Special deals2 instead of1.
        var world = Board();
        Card daggers = world.CreateCard("01046", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Card first = world.CreateCard("01129", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Card second = world.CreateCard("01129", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ResolveSpecial(world, daggers, 0, finalStep: final);
        var events = new List<GameEvent>();
        Prompt prompt = Sequence.Work(world, Cards, runner, events)!;
        Affordance choice = Assert.Single(prompt.Affordances,
            offer => offer.AnchorId == world.Seats[1].IdentityCard.ObjectId);

        Assert.Contains("choose a player", prompt.DisplayQuestion);
        Assert.Contains("villain and their engaged enemies", prompt.Description);
        Assert.Contains("Spider-Man", choice.CommitLabel);
        Assert.Contains($"Deal {amount} damage to the villain and {amount} to each enemy engaged with Spider-Man", choice.Description);
        Assert.Contains("prevention", choice.Description);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(choice.Id), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(amount, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, first.Damage);
        Assert.Equal(amount, second.Damage);
    }

    [Fact]
    public void NoradNamesItsTwoThreatConsequenceAndCurrentThreshold()
    {
        // Printed Core01138b offers2threat on itself or a facedown Drone.
        var world = Board();
        Card scheme = world.CreateCard("01138b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = new Occurrence(1, [Steps.VillainPhaseStepOneEnds]);
        var response = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);
        var events = new List<GameEvent>();
        Prompt prompt = Sequence.Work(world, Cards, runner, events)!;
        Affordance order = Assert.Single(prompt.Affordances);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(order.Id, order.Targets!.Legal, []), events);
        prompt = Sequence.Work(world, Cards, runner, events)!;
        Affordance threat = Assert.Single(prompt.Affordances, offer => offer.Label == "placeThreat");

        Assert.Equal("Place 2 threat on Assault on NORAD", threat.DisplayLabel);
        Assert.Equal(threat.DisplayLabel, threat.CommitLabel);
        Assert.Contains("2/20", threat.Description);
        Assert.Contains("prevention", threat.Description);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(threat.Id), events);
        Prompt next = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(1, next.Player);
        Assert.Equal(4, scheme.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void PlayerDamageCopyDoesNotEvaluateConcealedMatchingCardCounts(int upgrades)
    {
        var world = Board();
        for (int index = 0; index < upgrades; index++) world.CreateCard("01047", world.Seats[0].Deck);
        Card source = world.CreateCard("01046", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [{ "card": "01046", "abilities": [{
              "trigger": { "event": "ResolveSpecial", "timing": "Special", "subject": "this" },
              "effect": { "chooseCard": { "from": { "query": "identities" }, "effect": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount":
                  { "count": { "cardsIn": { "area": "yourDeck", "kind": "Upgrade" } } } } },
                { "dealDamage": { "cards": { "query": "enemiesEngagedWithChosenPlayer" }, "amount": 1 } }
              ] } } }
            }]}]}
            """));
        world.Abilities = runner;
        runner.ResolveSpecial(world, source, 0, finalStep: false);
        Prompt prompt = Sequence.Work(world, Cards, runner, [])!;
        Affordance choice = Assert.Single(prompt.Affordances,
            offer => offer.AnchorId == world.Seats[1].IdentityCard.ObjectId);
        Assert.Equal("Spider-Man", choice.DisplayLabel);
        Assert.Equal("Select Spider-Man → Spider-Man", choice.Description);
    }

    [Fact]
    public void NickFuryOffersThreeConciseQuantifiedOptionsBeforeChoosingTheirTargets()
    {
        var world = Board();
        Card scheme = world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        for (int index = 0; index < 3; index++) world.CreateCard("01044", world.Seats[0].Deck);
        Card nick = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = new Occurrence(1, [Steps.CardEntersPlay], Subject: nick.ObjectId, Player: 0);
        var response = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);
        Prompt prompt = Sequence.Work(world, Cards, runner, [])!;
        Assert.Equal(["Remove 2 threat", "Draw 3 cards", "Deal 4 damage"],
            prompt.Affordances.Select(offer => offer.DisplayLabel));
        Assert.Contains("choose a scheme", prompt.Affordances[0].Description!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("choose an enemy", prompt.Affordances[2].Description!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, scheme.Tokens["k_threat"]);
    }

    private static World Board()
    {
        var world = new World(Cards, players: 2);
        var first = world.CreateSeat("Black Panther");
        first.IdentityCard = world.CreateCard("01040a", first.Hero);
        var second = world.CreateSeat("Spider-Man");
        second.IdentityCard = world.CreateCard("01001a", second.Hero);
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        return world;
    }
}

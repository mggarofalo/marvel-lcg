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

/// <summary>Test-local printed Red Dagger text; this does not admit the pack.</summary>
public sealed class DistinctArrowPaymentTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("YY", false)]
    [InlineData("YB", true)]
    [InlineData("GG", true)]
    [Rule("rr:wild-resource.1")]
    [Rule("rr:wild-resource.1.1")]
    public void ConsequentialDefeatRequiresDifferentTypesFromResourcesRatherThanCards(string generated, bool payable)
    {
        // Wild resources "can be used as any other resource type"; multiple wild
        // resources "may be declared as different resource types".
        var (world, ally, enemy, generator) = Board(generated);
        BasicPowerResolution.Consequential(world, Cards, ally, true, "Attack", []);
        var prompt = Sequence.Work(world, Cards, world.Abilities, []);
        if (!payable)
        {
            Assert.Null(prompt);
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
            Assert.Equal(0, enemy.Damage);
            return;
        }
        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
        var offer = Assert.Single(prompt.Affordances);
        var price = Assert.Single(offer.Costs!);
        Assert.True(Assert.Single(price.ResourceCosts).DistinctTypes);
        Assert.Contains("different types", offer.CostDescription!, StringComparison.Ordinal);
        var allocation = ResourcePayment.Allocate(price, [generator.ObjectId])!;
        Assert.NotNull(allocation);
        Sequence.Answer(world, Cards, world.Abilities, prompt,
            new Decision(offer.Id, [enemy.ObjectId], [generator.ObjectId], Allocations: allocation), []);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, []));
        Assert.Equal(2, enemy.Damage);
        Assert.Same(world.Seats[0].Hand, ally.Area);
        Assert.Equal(0, ally.Damage);
    }

    [Fact]
    public void AnInvalidWildDeclarationCannotPayOrMoveAnyCard()
    {
        var (world, ally, enemy, generator) = Board("GG");
        BasicPowerResolution.Consequential(world, Cards, ally, true, "Attack", []);
        var prompt = Sequence.Work(world, Cards, world.Abilities, [])!;
        var offer = Assert.Single(prompt.Affordances);
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, world.Abilities, prompt,
            new Decision(offer.Id, [enemy.ObjectId], [generator.ObjectId],
                Allocations: [new(generator.ObjectId, 0, "YY")]), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, enemy.Damage);
    }

    private static (World World, Card Ally, Card Enemy, Card Generator) Board(string generated)
    {
        var world = new World(Cards, players: 2);
        foreach (string hero in new[] { "01001a", "01010a" })
        {
            var seat = world.CreateSeat(hero);
            seat.IdentityCard = world.CreateCard(hero, seat.Hero);
            for (int index = 0; index < 5; index++) world.CreateCard("01088", seat.Deck);
        }
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse($$$"""
            {"cards":[
              {"card":"05002","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"Interrupt","subject":"this"},
                "cost":{"spendDistinct":2},
                "effect":{"seq":[
                  {"dealDamage":{"cards":{"query":"enemies"},"amount":2}},
                  {"returnToHand":"this"}
                ]}
              }]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "effect":{"generate":"{{{generated}}}"}
              }]}
            ]}
            """));
        // Make the Call can put an ally owned by seat 0 under seat 1's control.
        // Rulings 10293662ffefd5b2 and 2368dda1610740b3 assign the payment to
        // its controller and the returned card to its owner's hand.
        var ally = world.CreateCard("05002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        ally.TakeDamage(2);
        var enemy = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var generator = world.CreateCard("01030", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        return (world, ally, enemy, generator);
    }
}



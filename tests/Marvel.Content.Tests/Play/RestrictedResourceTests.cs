using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RestrictedResourceTests
{
    [Theory]
    [InlineData("04005", true)]
    [InlineData("04008", true)]
    [InlineData("01005", false)]
    [InlineData("04003", false)]
    [Rule("rr:resource-ability.1")]
    public void ExpertMarksmanOffersOnlyForArrowEvents(string targetFace, bool allowed)
    {
        // 04010: "generate a wild resource for an Arrow event."
        var fixture = RestrictedResourceFixture.Create("arrowEvent", hero: false);
        var world = fixture.World;
        var target = world.CreateCard(targetFace, world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        long rng = world.Random.Generator.WordsConsumed;
        var agenda = world.Agenda.Outstanding.ToArray();
        for (int repeat = 0; repeat < 2; repeat++)
            Assert.Equal(allowed, CardPayment.Generators(world, world.Facts, world.Seats[0], fixture.Runner, target)
                .Any(source => source.Effect == fixture.Generator.ObjectId));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.True(fixture.Generator.Ready);
    }

    [Theory]
    [InlineData("01057", true)]
    [InlineData("01063", true)]
    [InlineData("04035", true)]
    [InlineData("04036", true)]
    [InlineData("04037", true)]
    [InlineData("04034", false)]
    [InlineData("01091", false)]
    [Rule("rr:resource-ability.1")]
    public void FinesseIncludesAspectSignaturesButExcludesHeroAndBasicCards(string targetFace, bool allowed)
    {
        // 04033: "generate a wild resource for an aspect card."
        var fixture = RestrictedResourceFixture.Create("aspectCard");
        var world = fixture.World;
        var target = world.CreateCard(targetFace, world.Seats[0].Hand);
        Assert.Equal(allowed, CardPayment.Generators(world, world.Facts, world.Seats[0], fixture.Runner, target)
            .Any(source => source.Effect == fixture.Generator.ObjectId));
    }

    [Theory]
    [InlineData("spend")]
    [InlineData("spendPrinted")]
    [Rule("rr:resource-ability.1")]
    public void FinessePaysAspectAbilityCostAndExhaustsExactlyOnce(string cost)
    {
        // ruling:6800619dc41a043c: Finesse pays a cost within an aspect card's ability.
        // Synthetic aspect action costs one wild and draws one card.
        var fixture = RestrictedResourceFixture.Create("aspectCard", cost: cost);
        var world = fixture.World;
        var ability = Assert.Single(fixture.Runner.Actions(world, 0));
        var price = Assert.Single(fixture.Runner.Describe(world, ability).CostOptions);
        Assert.Equal(fixture.Generator.ObjectId, Assert.Single(price.Sources!).Effect);
        int before = world.Seats[0].Deck.Cards.Count;
        fixture.Runner.ExecuteAction(world, new PlayerAction(ability, [fixture.Generator.ObjectId], []));
        Assert.False(fixture.Generator.Ready);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(before - 1, world.Seats[0].Deck.Cards.Count);
        Assert.Empty(fixture.Runner.ResourceAbilities(world, 0));
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void IneligibleForgedPaymentRejectsBeforeExhaustionOrOtherStateChange()
    {
        // Costs must be payable before paying any costs. Basic ability is not an aspect-card cost.
        var fixture = RestrictedResourceFixture.Create("aspectCard", "01091");
        var world = fixture.World;
        Assert.Empty(fixture.Runner.Actions(world, 0));
        string before = world.Digest().Canonical();
        long rng = world.Random.Generator.WordsConsumed;
        Assert.Throws<RulesNotImplementedException>(() => AbilityResourcePayment.Spend(world, fixture.Runner,
            new(fixture.Action, 0, [fixture.Generator.ObjectId], 1, "G"), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
        Assert.True(fixture.Generator.Ready);
        Assert.Empty(world.Seats[0].Hand.Cards);
    }

    [Fact]
    [Rule("rr:ability.13")]
    public void FinesseCannotGenerateInAlterEgoOrForAnotherPlayersOrdinaryPayment()
    {
        // A timing trigger containing Hero can only be used in that form.
        var alterEgo = RestrictedResourceFixture.Create("aspectCard", hero: false);
        Assert.Empty(alterEgo.Runner.ResourceAbilities(alterEgo.World, 0));
        var fixture = RestrictedResourceFixture.Create("aspectCard", players: 2);
        var target = fixture.World.CreateCard("01063", fixture.World.Seats[1].Hand);
        Assert.DoesNotContain(CardPayment.Generators(fixture.World, fixture.World.Facts,
            fixture.World.Seats[1], fixture.Runner, target), source => source.Effect == fixture.Generator.ObjectId);
        fixture.Generator.Exhaust();
        Assert.Empty(fixture.Runner.ResourceAbilities(fixture.World, 0));
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    public void MissingCostContextCannotSpendRestrictedGenerators()
    {
        var fixture = RestrictedResourceFixture.Create("arrowEvent");
        Assert.DoesNotContain(CardPayment.Generators(fixture.World, fixture.World.Facts,
            fixture.World.Seats[0], fixture.Runner), source => source.Effect == fixture.Generator.ObjectId);
    }
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void ExpertMarksmanPaysAnArrowEventAndItsEffectResolvesAfterExhaustion()
    {
        // Printed 04006 is an Arrow event costing one. Its synthetic draw effect
        // isolates payment from the separately authored event's damage behavior.
        var fixture = RestrictedResourceFixture.Create("arrowEvent", "04006", cost: "");
        var ability = Assert.Single(fixture.Runner.Actions(fixture.World, 0));
        fixture.Runner.ExecuteAction(fixture.World, new PlayerAction(ability, [fixture.Generator.ObjectId], []));
        Assert.False(fixture.Generator.Ready);
        Assert.Equal(DeckType.DiscardPile, fixture.Action.Area.Type);
        Assert.Single(fixture.World.Seats[0].Hand.Cards);
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    public void FinessePaysPrintedAspectCardCostAndPutsTheCardIntoPlay()
    {
        var fixture = RestrictedResourceFixture.Create("aspectCard");
        var world = fixture.World;
        World.MoveToTop(fixture.Action, world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, fixture.Runner, world.Seats[0], fixture.Action,
            [fixture.Generator.ObjectId], []);
        Assert.False(fixture.Generator.Ready);
        Assert.Equal(DeckType.SupportsArea, fixture.Action.Area.Type);
        Assert.DoesNotContain(fixture.Action, world.Seats[0].Hand.Cards);
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    public void AspectAbilityContextQualifiesPowerOfAggressionHandResources()
    {
        // rr:cost.3.1: paying for an ability on a card counts as paying for that card.
        var fixture = RestrictedResourceFixture.Create("aspectCard", "01057");
        var world = fixture.World;
        Card power = world.CreateCard("01055", world.Seats[0].Hand);
        var playing = CardPayment.Generators(world, world.Facts, world.Seats[0], fixture.Runner, fixture.Action);
        var ability = CardPayment.Generators(world, world.Facts, world.Seats[0], fixture.Runner,
            abilitySource: fixture.Action);
        Assert.Equal("GG", playing.Single(source => source.Effect == power.ObjectId).PossibleResources);
        Assert.Equal("GG", ability.Single(source => source.Effect == power.ObjectId).PossibleResources);
        Assert.Contains(ability, source => source.Effect == fixture.Generator.ObjectId);
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    public void ArrowTraitDoesNotMakeASupportAnArrowEvent()
    {
        var fixture = RestrictedResourceFixture.Create("arrowEvent");
        fixture.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "trait:ARROW", 1,
            Card: fixture.Action.ObjectId, Affects: fixture.Action.ObjectId));
        Assert.DoesNotContain(CardPayment.Generators(fixture.World, fixture.World.Facts,
            fixture.World.Seats[0], fixture.Runner, fixture.Action), source => source.Effect == fixture.Generator.ObjectId);
    }

}

using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PreparationResourceIndependentTests
{
    [Theory]
    [InlineData(false, "spend")]
    [InlineData(true, "spend")]
    [InlineData(false, "spendPrinted")]
    [InlineData(true, "spendPrinted")]
    [Rule("rr:resource-ability.1.1")]
    public void FullSyntheticPreparationAbilityPaysInEitherFormAndExcludesSameCostOtherAbility(bool hero, string cost)
    {
        // RR: a resource for a specific cost type "cannot be triggered when paying a cost not of that type".
        // 08007 has no Hero restriction. Synthetic draw actions isolate matching-card
        // semantics inferred from Finesse; these are not printed 08008/01064 abilities.
        var (world, runner, first, second, preparation, unrelated) = AbilityBoard(hero, cost);
        Assert.Equal("2", world.Facts.Attributes(preparation.FaceId)["Cost"]);
        Assert.Equal("2", world.Facts.Attributes(unrelated.FaceId)["Cost"]);
        var action = Assert.Single(runner.Actions(world, 0));
        Assert.Equal(preparation.ObjectId, action.Card);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal(2, price.Generators.Count);
        Assert.All(price.Generators, source => Assert.Equal(ResourceRestriction.PreparationCard, source.Restriction));
        int deck = world.Seats[0].Deck.Cards.Count;
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId], []));
        Assert.False(first.Ready);
        Assert.True(second.Ready);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(deck - 1, world.Seats[0].Deck.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:traits.1")]
    public void EligibilityTracksGrantedAndLostPreparationRatherThanPrintedFace(bool originallyPreparation)
    {
        // "Some card abilities reference cards that possess or lack specific traits."
        var board = PreparationResourceFixture.Board(face: originallyPreparation ? "08008" : "01064");
        var world = board.World;
        var runner = PreparationResourceFixture.Runner();
        Assert.Equal(originallyPreparation, Eligible());
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            originallyPreparation ? Characteristics.LossOf("trait:PREPARATION") : "trait:PREPARATION", 1,
            Card: board.Target.ObjectId, Affects: board.Target.ObjectId));
        Assert.Equal(!originallyPreparation, Eligible());
        Assert.Equal(!originallyPreparation,
            CardPayment.Price(world, world.Facts, world.Seats[0], board.Target) is not null);
        Assert.True(board.First.Ready);
        Assert.True(board.Second.Ready);

        bool Eligible() => CardPayment.Generators(world, world.Facts, world.Seats[0], runner,
            abilitySource: board.Target).Any(source => source.Effect == board.First.ObjectId);
    }

    private static (World World, AbilityRunner Runner, Card First, Card Second, Card Preparation, Card Unrelated)
        AbilityBoard(bool hero, string cost)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse($$$$"""
            {"cards":[
              {"card":"08007","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "resourceRestriction":"preparationCard","printedResources":"G",
                "cost":{"exhaust":"this"},"effect":{"generate":"G"}}]},
              {"card":"08008","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"{{{{cost}}}}":"G"},"effect":{"draw":{"player":"you","count":1}}}]},
              {"card":"01064","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"{{{{cost}}}}":"G"},"effect":{"draw":{"player":"you","count":1}}}]}
            ]}
            """));
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 17);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard(hero ? "01001a" : "01001b", seat.Hero);
        for (int index = 0; index < 8; index++) world.CreateCard("01003", seat.Deck);
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var upgrades = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0);
        var first = world.CreateCard("08007", upgrades);
        var second = world.CreateCard("08007", upgrades);
        var preparation = world.CreateCard("08008", upgrades);
        var unrelated = world.CreateCard("01064", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        _ = Game.Begin(world, facts, runner);
        return (world, runner, first, second, preparation, unrelated);
    }
}

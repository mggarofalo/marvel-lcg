using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreLastingEffectCardsTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:lasting-effects.4")]
    [Rule("rr:target.5")]
    [Fact]
    public void LeadFromTheFrontAffectsTheChosenPlayersCurrentAndLaterCharacters()
    {
        var world = Board();
        var eventCard = world.CreateCard("01070", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var existing = world.CreateCard(
            "01076", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(eventCard.ObjectId, AbilityType.Action, 0),
            [payment.ObjectId], []);
        AnswerCardChoice(world, runner, eventCard, world.Seats[1].IdentityCard);

        var later = world.CreateCard(
            "01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        Assert.Equal(3, StateFields.Modified(world, existing, "attack", Cards, world.Players));
        Assert.Equal(2, StateFields.Modified(world, later, "thwart", Cards, world.Players));
        Assert.Equal(
            3,
            StateFields.Modified(
                world, world.Seats[1].IdentityCard, "attack", Cards, world.Players));
        Assert.Equal(
            2,
            StateFields.Modified(
                world, world.Seats[0].IdentityCard, "attack", Cards, world.Players));
    }

    [Rule("rr:lasting-effects.1")]
    [Rule("rr:target.5")]
    [Fact]
    public void HelicarrierReducesOnlyTheChosenPlayersNextCard()
    {
        var world = Board();
        var support = world.CreateCard(
            "01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var mine = world.CreateCard("01083", world.Seats[0].Hand);
        var theirs = world.CreateCard("01083", world.Seats[1].Hand);
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(support.ObjectId, AbilityType.Action, 0), [], []);
        AnswerCardChoice(world, runner, support, world.Seats[1].IdentityCard);

        Assert.False(support.Ready);
        Assert.Equal(3, CardPlay.CostOf(world, Cards, world.Seats[0], mine).Amount);
        Assert.Equal(2, CardPlay.CostOf(world, Cards, world.Seats[1], theirs).Amount);
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Rule("rr:initiating-abilities.step.4")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void HelicarrierReducesAnEventsPriceAndPayment()
    {
        // Step 3 determines a card's cost "taking modifiers into account", and
        // step 4 says to "apply any modifiers to the cost(s)." An event uses
        // those same play-card steps even though its action owns the prompt.
        var world = Board();
        var support = world.CreateCard(
            "01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var eventCard = world.CreateCard("01070", world.Seats[0].Hand);
        var payment = world.CreateCard("01003", world.Seats[0].Hand);
        var next = world.CreateCard("01083", world.Seats[0].Hand);
        world.CreateCard("01005", world.Seats[0].Deck);
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(support.ObjectId, AbilityType.Action, 0), [], []);
        AnswerCardChoice(world, runner, support, world.Seats[0].IdentityCard);

        var action = Assert.Single(
            runner.Actions(world, 0), candidate => candidate.Card == eventCard.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("1", price.Cost);
        Assert.Contains(price.Generators, source => source.Effect == payment.ObjectId);

        runner.Act(world, action, [payment.ObjectId], []);

        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(3, CardPlay.CostOf(world, Cards, world.Seats[0], next).Amount);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void AFailedEventPaymentDoesNotSpendHelicarriersReduction()
    {
        // Step 5 says a failed payment must "abort this process without paying
        // any costs," so attempting the event is not the next card played.
        var world = Board();
        var support = world.CreateCard(
            "01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var eventCard = world.CreateCard("01070", world.Seats[0].Hand);
        world.CreateCard("01003", world.Seats[0].Hand);
        var next = world.CreateCard("01083", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(support.ObjectId, AbilityType.Action, 0), [], []);
        AnswerCardChoice(world, runner, support, world.Seats[0].IdentityCard);
        var action = Assert.Single(
            runner.Actions(world, 0), candidate => candidate.Card == eventCard.ObjectId);

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [], []));

        Assert.Equal(2, CardPlay.CostOf(world, Cards, world.Seats[0], next).Amount);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:ownership-and-control.7.2")]
    [Fact]
    public void MakeTheCallPricesTheChosenAllyAndPutsItUnderYourControl()
    {
        var world = Board();
        var source = world.CreateCard("01071", world.Seats[0].Hand);
        world.CreateCard("01005", world.Seats[0].Deck);
        var doubleResource = world.CreateCard("01088", world.Seats[0].Hand);
        var singleResource = world.CreateCard("01003", world.Seats[0].Hand);
        var theirDiscard = world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        var chosen = world.CreateCard("01083", theirDiscard);
        world.CreateCard("01076", theirDiscard);
        var runner = AuthoredCards.Runner();

        runner.Act(
            world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var step = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, step.Index, step.Tier)!;
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(chosen.ObjectId, offer.AnchorId);
        Assert.Equal("3", Assert.Single(offer.CostOptions).Cost);

        runner.Chose(
            world, source, 0, step.Index,
            Decision.Take(
                chosen.ObjectId, [], [doubleResource.ObjectId, singleResource.ObjectId]),
            step.Tier);

        Assert.Equal(1, chosen.Owner);
        Assert.Equal(PlayArea.Of(0), chosen.Area.PlayArea);
        Assert.Equal(DeckType.AlliesArea, chosen.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleResource.Area.Type);
        Assert.Equal(DeckType.DiscardPile, singleResource.Area.Type);
    }

    [Fact]
    public void PowerOfLeadershipGeneratesTwoForMakeTheCallsLeadershipAlly()
    {
        // The official FAQ for 01071 says Make the Call pays the ally's own
        // cost. A matching Power of Aspect card therefore generates two
        // resources even though Make the Call, rather than the ally, is played.
        var world = Board();
        var source = world.CreateCard("01071", world.Seats[0].Hand);
        var power = world.CreateCard("01072", world.Seats[0].Hand);
        world.CreateCard("01005", world.Seats[0].Deck);
        var theirDiscard = world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        var maria = world.CreateCard("01067", theirDiscard);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.Act(
            world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var step = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, step.Index, step.Tier)!;
        var offer = Assert.Single(prompt.Affordances);
        var price = Assert.Single(offer.CostOptions);

        Assert.Equal(maria.ObjectId, offer.AnchorId);
        Assert.Equal("2", price.Cost);
        Assert.Equal("GG", Assert.Single(price.Generators).Generates);

        runner.Chose(
            world,
            source,
            0,
            step.Index,
            Decision.Take(maria.ObjectId, [], [power.ObjectId]),
            step.Tier);

        Assert.Equal(DeckType.AlliesArea, maria.Area.Type);
        Assert.Equal(PlayArea.Of(0), maria.Area.PlayArea);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    private static void AnswerCardChoice(
        World world, Marvel.Cards.Run.AbilityRunner runner, Card source, Card chosen)
    {
        var step = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, source, player: 0, step.Index,
            Decision.Take(chosen.ObjectId), step.Tier);
    }

    private static World Board()
    {
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            seat.IdentityCard.TurnTo("01001a");
        }

        return world;
    }
}

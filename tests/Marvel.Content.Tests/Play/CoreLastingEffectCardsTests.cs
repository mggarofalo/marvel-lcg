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
        world.CreateCard("01088", world.Seats[0].Deck);
        var eventCard = world.CreateCard("01070", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var existing = world.CreateCard(
            "01076", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var runner = AuthoredCards.Runner();

        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(eventCard.ObjectId, AbilityType.Action, 0), [payment.ObjectId], []));
        Assert.Equal(Outcome.Unfinished, world.Result);
        AnswerCardChoice(world, runner, eventCard, world.Seats[1].IdentityCard);
        Assert.Equal(Outcome.Unfinished, world.Result);

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

        Affordance action = runner.Describe(world, new PendingAbility(support.ObjectId, AbilityType.Action, 0));
        Assert.False(action.DeferredTargetSelection);
        Assert.True(action.DirectRecipientCommit);
        Assert.Contains(world.Seats[1].IdentityCard.ObjectId, action.Targets!.Legal);
        Assert.Contains("choose a player", action.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("next card this phase costs 1 fewer resources", action.Description);
        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(support.ObjectId, AbilityType.Action, 0), [], []));
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, choice)!;
        Assert.Equal(
            "Spider-Man · p1: next card −1 resource cost this phase. Applies once, when that player plays their next card.",
            prompt.Affordances.Single(option =>
                option.Id == world.Seats[1].IdentityCard.ObjectId).Description);
        AnswerCardChoice(world, runner, support, world.Seats[1].IdentityCard);

        Assert.False(support.Ready);
        Assert.Equal(3, CardPayment.CostOf(world, Cards, world.Seats[0], mine).Amount);
        Assert.Equal(2, CardPayment.CostOf(world, Cards, world.Seats[1], theirs).Amount);
    }

    [Fact]
    public void ThreatRemovingCardChoicesDescribeTheResultingThreat()
    {
        // The report is a client wire choice: show current and resulting game
        // state so the player does not have to infer the effect from card text.
        var world = Board();
        var scheme = world.CreateCard(
            "01097b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        var team = world.CreateCard(
            "01064", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        team.PlaceTokens("c_snoop", 3);
        var runner = AuthoredCards.Runner();

        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(team.ObjectId, AbilityType.Action, 0), [], []));
        var step = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, step)!;

        Assert.Contains("2/14 → 1/14 threat", Assert.Single(prompt.Affordances).Description,
            StringComparison.Ordinal);
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

        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(support.ObjectId, AbilityType.Action, 0), [], []));
        AnswerCardChoice(world, runner, support, world.Seats[0].IdentityCard);

        var action = Assert.Single(
            runner.Actions(world, 0), candidate => candidate.Card == eventCard.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        Assert.Equal("1", price.Cost);
        Assert.Contains(price.Generators, source => source.Effect == payment.ObjectId);

        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));

        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(3, CardPayment.CostOf(world, Cards, world.Seats[0], next).Amount);
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

        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(support.ObjectId, AbilityType.Action, 0), [], []));
        AnswerCardChoice(world, runner, support, world.Seats[0].IdentityCard);
        var action = Assert.Single(
            runner.Actions(world, 0), candidate => candidate.Card == eventCard.ObjectId);

        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));

        Assert.Equal(2, CardPayment.CostOf(world, Cards, world.Seats[0], next).Amount);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:ownership-and-control.7.2")]
    [Theory]
    [InlineData("01071")]
    [InlineData("03016")]
    public void MakeTheCallPricesTheChosenAllyAndPutsItUnderYourControl(string face)
    {
        var world = Board();
        var source = world.CreateCard(face, world.Seats[0].Hand);
        world.CreateCard("01005", world.Seats[0].Deck);
        var doubleResource = world.CreateCard("01088", world.Seats[0].Hand);
        var singleResource = world.CreateCard("01003", world.Seats[0].Hand);
        var theirDiscard = world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        var chosen = world.CreateCard("01083", theirDiscard);
        world.CreateCard("01076", theirDiscard);
        var runner = AuthoredCards.Runner();
        world.Abilities = new UnrelatedWorldAbilities();

        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []));
        var step = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, step)!;
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(chosen.ObjectId, offer.AnchorId);
        Assert.Equal("3", Assert.Single(offer.CostOptions).Cost);

        runner.Chose(world, step, Decision.Take(
                chosen.ObjectId, [], [doubleResource.ObjectId, singleResource.ObjectId], new Dictionary<string, long>(),
                Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(ResourcePayment.Allocate(
                    Assert.Single(offer.CostOptions), [doubleResource.ObjectId, singleResource.ObjectId]))));

        Assert.Equal(1, chosen.Owner);
        Assert.Equal(PlayArea.Of(0), chosen.Area.PlayArea);
        Assert.Equal(DeckType.AlliesArea, chosen.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleResource.Area.Type);
        Assert.Equal(DeckType.DiscardPile, singleResource.Area.Type);
    }

    [Rule("rr:cost.3")]
    [Rule("rr:cost.4.1")]
    [Theory]
    [InlineData("01071", "01072")]
    [InlineData("03016", "03018")]
    public void PowerOfLeadershipGeneratesTwoForMakeTheCallsLeadershipAlly(string callFace, string powerFace)
    {
        // The official FAQ for 01071 says Make the Call pays the ally's own
        // cost. A matching Power of Aspect card therefore generates two
        // resources even though Make the Call, rather than the ally, is played.
        // rr:cost.3 pays costs with generated resources; rr:cost.4.1 says
        // resources beyond the cost "were not paid for that cost."
        var world = Board();
        var source = world.CreateCard(callFace, world.Seats[0].Hand);
        var power = world.CreateCard(powerFace, world.Seats[0].Hand);
        world.CreateCard("01005", world.Seats[0].Deck);
        var theirDiscard = world.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        var maria = world.CreateCard("01067", theirDiscard);
        var hulk = world.CreateCard("01050", theirDiscard);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;

        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []));
        var step = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, step)!;
        var offer = Assert.Single(prompt.Affordances);
        var price = Assert.Single(offer.CostOptions);

        Assert.Equal(maria.ObjectId, offer.AnchorId);
        Assert.Equal("2", price.Cost);
        var quote = Assert.Single(price.Generators);
        Assert.Equal("G", quote.Generates);
        Assert.Equal(new ConditionalResourceBonus("G", 1), quote.ConditionalBonus);

        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(price, [power.ObjectId]));
        Assert.Equal(new ResourceAllocation(power.ObjectId, 0, "GG"), Assert.Single(allocation));
        Assert.Equal("GG", ResourcePayment.GeneratedBy(quote, allocation));
        runner.Chose(world, step, Decision.Take(maria.ObjectId, [], [power.ObjectId],
            new Dictionary<string, long>(), allocation));

        Assert.Equal(1, maria.Owner);
        Assert.Equal(DeckType.DiscardPile, hulk.Area.Type);
        Assert.Equal(DeckType.AlliesArea, maria.Area.Type);
        Assert.Equal(PlayArea.Of(0), maria.Area.PlayArea);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    private static void AnswerCardChoice(
        World world, Marvel.Cards.Run.AbilityRunner runner, Card source, Card chosen)
    {
        var step = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(world, step, Decision.Take(chosen.ObjectId));
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

    private sealed class UnrelatedWorldAbilities : NoCardAbilities
    {
        private readonly string counterFailure =
            "the unrelated World counter-pool port was used";

        public CardCounterPool? CounterPool(World world, Card card) =>
            throw new InvalidOperationException(counterFailure);

        public override AbilityExecutionResult EntersPlay(
            World world, AbilityExecution entry, Card card) =>
            throw new InvalidOperationException("the unrelated World enter-play port was used");

        public override IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player) =>
            throw new InvalidOperationException("the unrelated World resource port was used");

        public override string ResourcesGeneratedBy(World world, Card source, Card? payingFor) =>
            throw new InvalidOperationException("the unrelated World resource port was used");

        public override ResourceExecutionResult UseResource(
            World world, AbilityExecution entry, int player, int card) =>
            throw new InvalidOperationException("the unrelated World resource port was used");
    }
}

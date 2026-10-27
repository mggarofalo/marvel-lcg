using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic payment-boundary probes; no Hulk production admission.</summary>
public sealed class HandResourceFormReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:resource.1")]
    [Rule("rr:the-golden-rules")]
    public void StaleRestrictedContributorCannotHideAmongOtherwiseSufficientResources(int cost)
    {
        // Printed 10007: "Spend this card only in hero form." That restriction
        // overrides permission to generate resources "by discarding cards from their hand".
        var (world, runner, source, restricted, ordinary) = Board();
        var price = new CostOption(source.ObjectId, cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Sources: CardPayment.Generators(world, world.Facts, world.Seats[1], runner));
        int[] paying = [ordinary.ObjectId, restricted.ObjectId];
        var request = new AbilityResourcePaymentRequest(source, 1, paying, cost, "")
        { Price = price, Allocations = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(price, paying)) };
        world.Seats[1].IdentityCard.TurnTo("10001b");
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var events = new List<GameEvent>();

        Assert.Throws<RulesNotImplementedException>(() =>
            AbilityResourcePayment.Spend(world, runner, request, events));

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Empty(events);
        // The same ordinary source pays the same price, proving the stale card
        // is the reason for rejection, including when the price is zero.
        var control = request with { Paying = [ordinary.ObjectId], Price = null, Allocations = [] };
        Assert.True(AbilityResourcePayment.Spend(world, runner, control, events).IsComplete);
        Assert.Equal(DeckType.HandsArea, restricted.Area.Type);
        Assert.Equal(DeckType.DiscardPile, ordinary.Area.Type);
        world.Seats[1].IdentityCard.TurnTo("10001a");
        Assert.True(AbilityResourcePayment.Spend(world, runner,
            request with { Paying = [restricted.ObjectId], Price = null, Allocations = [] }, events).IsComplete);
        Assert.Equal(DeckType.DiscardPile, restricted.Area.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:resource.3")]
    [Rule("rr:resource.5")]
    public void UnrestrictedOverpaymentAndZeroCostRemainLegalInAlterEgo(int cost)
    {
        // "Equal to (or greater than) the card's cost" is sufficient, and
        // "Excess resources generated toward any cost are lost".
        var (world, runner, source, restricted, ordinary) = Board();
        world.Seats[1].IdentityCard.TurnTo("10001b");
        var extra = world.CreateCard("01090", world.Seats[1].Hand);
        var events = new List<GameEvent>();
        var receipt = AbilityResourcePayment.Spend(world, runner,
            new(source, 1, [ordinary.ObjectId, extra.ObjectId], cost, ""), events);
        Assert.True(receipt.IsComplete);
        Assert.Equal(DeckType.DiscardPile, ordinary.Area.Type);
        Assert.Equal(DeckType.DiscardPile, extra.Area.Type);
        Assert.Equal(DeckType.HandsArea, restricted.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:identity")]
    public void PaymentUsesTheNonFirstContributorsLiveFormAndPreservesPrintedIcons()
    {
        // "The side that is face up indicates the form" of that player.
        var (world, runner, _, restricted, _) = Board();
        world.Seats[0].IdentityCard.TurnTo("01001b");
        Assert.Contains(CardPayment.Generators(world, world.Facts, world.Seats[1], runner),
            item => item.Effect == restricted.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        world.Seats[1].IdentityCard.TurnTo("10001b");
        Assert.DoesNotContain(CardPayment.Generators(world, world.Facts, world.Seats[1], runner),
            item => item.Effect == restricted.ObjectId);
        Assert.Equal("RRR", Resources.GeneratedBy(restricted.FaceId, world.Facts));
        Assert.Equal(3, Resources.PrintedCount([restricted], 'R', world.Facts));
        world.Seats[1].IdentityCard.TurnTo("10001a");
        Assert.Equal("RRR", runner.ResourcesGeneratedBy(world, restricted, null));
    }

    [Fact]
    [Rule("rr:resource.1")]
    public void FormChangeAfterHandResourcesCommitDoesNotInvalidateThatCompletedContribution()
    {
        // Resources are generated "by discarding cards from their hand". Printed
        // 10007's hero restriction applies when spent, before the later form change.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"10007","handResourceForm":"hero","abilities":[]},
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"spend":"Y"},
                "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}}]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"Y"}}]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}}]}
            ]}
            """));
        Card? source = null;
        Card? generator = null;
        Card? restricted = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            generator = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            restricted = board.CreateCard("10007", board.Seats[0].Hand);
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            item => item.Verb == Game.ActionVerb && item.AnchorId == source!.ObjectId);
        int[] paying = [restricted!.ObjectId, generator!.ObjectId];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(action.Costs!), paying));
        game.Resolve(new Decision(action.Id, [], paying, Allocations: allocation));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, restricted.Area.Type);
        Forms.Change(world.Seats[0], world.Facts);
        Assert.False(runner.CanUseHandResources(world, restricted));
        game.Resolve(Decision.Decline);
        Assert.Equal(DeckType.DiscardPile, restricted.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(1, source!.Tokens.GetValueOrDefault("c_test"));
        Assert.False(world.Agenda.IsBusy);
    }

    private static (World World, AbilityRunner Runner, Card Source, Card Restricted, Card Ordinary) Board()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"10007","handResourceForm":"hero","abilities":[]}]}
            """));
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2, 73);
        var first = world.CreateSeat("p0");
        first.IdentityCard = world.CreateCard("01001a,01001b", first.Hero);
        var second = world.CreateSeat("p1");
        second.IdentityCard = world.CreateCard("10001a,10001b", second.Hero);
        for (int i = 0; i < 8; i++) world.CreateCard("01003", second.Deck);
        var source = world.CreateCard("01006",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var restricted = world.CreateCard("10007", second.Hand);
        var ordinary = world.CreateCard("01003", second.Hand);
        world.Abilities = runner;
        return (world, runner, source, restricted, ordinary);
    }
}

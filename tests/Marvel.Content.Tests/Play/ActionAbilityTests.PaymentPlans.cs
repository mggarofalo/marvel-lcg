using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost.7")]
    [Rule("rr:exhausted.2")]
    [Theory]
    [InlineData("legal")]
    [InlineData("identity")]
    [InlineData("exhausted")]
    [InlineData("foreign")]
    public void AChosenCostRevalidatesItsExactCardBeforeAnyCostIsPaid(string selection)
    {
        // "Abort this process without paying any costs." Being able to pay
        // with one ready ally does not authorize a different supplied card.
        // An exhausted card "cannot exhaust again until it is readied".
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""",
            cost: """
                {"seq":[{"exhaust":"this"},{"exhaustChosen":{
                  "from":{"query":"alliesYouControl"},"count":1}}]}
                """);
        Card? source = null;
        Card? legal = null;
        Card? exhausted = null;
        Card? foreign = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var allies = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
            legal = board.CreateCard(AuthoredCards.BlackCat, allies);
            exhausted = board.CreateCard("01066", allies);
            exhausted.Exhaust();
            foreign = board.CreateCard(AuthoredCards.BlackCat,
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var ability = Assert.Single(runner.Actions(world, 0),
            candidate => candidate.Card == source!.ObjectId);
        var selected = selection switch
        {
            "legal" => legal!,
            "identity" => world.Seats[0].IdentityCard,
            "exhausted" => exhausted!,
            "foreign" => foreign!,
            _ => throw new InvalidOperationException("Unknown test selection"),
        };
        string before = world.Digest().Canonical();
        int held = world.Seats[0].Hand.Cards.Count;

        if (selection != "legal")
        {
            Assert.Throws<RulesNotImplementedException>(() =>
                runner.Act(world, ability, [], [selected.ObjectId]));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.True(source!.Ready);
            Assert.True(legal!.Ready);
            return;
        }

        runner.Act(world, ability, [], [selected.ObjectId]);

        Assert.False(source!.Ready);
        Assert.False(legal!.Ready);
        Assert.True(foreign!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Rule("rr:cost.5")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OfferingASimultaneousResourceCostRequiresEveryComponent(bool hasPhysical)
    {
        // Determine "the player's ability to pay them": multiple resource
        // components are one simultaneous payment, not independently affordable
        // alternatives. Two energy icons cannot pay the physical component.
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""",
            cost: """{"seq":[{"spend":"R"},{"spend":"Y"}]}""");
        Card? source = null;
        Card? energy = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            energy = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.Where(card => card != energy).ToList())
            World.MoveToTop(card, world.Seats[0].Deck);
        var physical = hasPhysical ? world.CreateCard(Physicals, world.Seats[0].Hand) : null;
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;

        var offered = runner.Actions(world, 0).Where(candidate => candidate.Card == source!.ObjectId).ToList();

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        if (!hasPhysical)
        {
            Assert.Empty(offered);
            Assert.Same(world.Seats[0].Hand, energy!.Area);
            return;
        }

        runner.Act(world, Assert.Single(offered), [physical!.ObjectId, energy!.ObjectId], []);

        Assert.Equal(DeckType.DiscardPile, physical.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost.5")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnEventPaymentValidatesAllAllocationsBeforeItsOtherCosts(bool valid)
    {
        // "Abort this process without paying any costs." The printed zero
        // cost and the arrow's energy cost are distinct allocation components;
        // assigning the energy to the zero cost cannot exhaust the identity.
        var runner = Runner(AuthoredCards.Backflip, "Action",
            """{"draw":{"player":"you","count":1}}""",
            cost: """{"seq":[{"spend":"Y"},{"exhaust":"you"}]}""");
        Card? source = null;
        Card? generator = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
            generator = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var ability = Assert.Single(runner.Actions(world, 0),
            candidate => candidate.Card == source!.ObjectId);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var occurrence = new Occurrence(0, [Steps.TurnAction],
            Subject: source!.ObjectId, Player: 0);
        IReadOnlyList<ResourceAllocation> allocation =
            [new(generator!.ObjectId, Cost: valid ? 1 : 0, PaidAs: "Y")];

        if (!valid)
        {
            Assert.Throws<RulesNotImplementedException>(() => runner.Act(
                world, ability, [generator.ObjectId], [], occurrence, allocations: allocation));

            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
            Assert.True(identity.Ready);
            Assert.Same(world.Seats[0].Hand, source!.Area);
            Assert.Same(world.Seats[0].Hand, generator.Area);
            Assert.False(occurrence.Is(Steps.CardPlayed));
            return;
        }

        var events = runner.Act(
            world, ability, [generator.ObjectId], [], occurrence, allocations: allocation);

        Assert.False(identity.Ready);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(held - 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.True(occurrence.Is(Steps.CardPlayed));
        int eventPlaced = events.ToList().FindIndex(happened => happened is CardsMoved moved
            && moved.Cards.Any(landing => landing.Card == source.ObjectId));
        int generatorSpent = events.ToList().FindIndex(happened => happened is CardsMoved moved
            && moved.Cards.Any(landing => landing.Card == generator.ObjectId));
        int exhausted = events.ToList().FindIndex(happened => happened is FieldSet field
            && field.Card == identity.ObjectId && field.Field == "is_exhaust");
        Assert.True(eventPlaced >= 0);
        Assert.True(generatorSpent > eventPlaced);
        Assert.True(exhausted > generatorSpent);
    }

    [Rule("rr:cost.11")]
    [Rule("rr:cost.12")]
    [Rule("rr:initiating-abilities.step.5")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DamagePaymentDistinguishesUnpaidTakingFromPaidDealing(bool mustTakeAll)
    {
        // Dealing damage is paid "even if some or all of that damage is
        // prevented"; taking damage is unpaid "unless all of that damage was
        // taken." A failed taking component cannot spend the other costs.
        string operation = mustTakeAll ? "takeDamage" : "dealDamage";
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"draw":{"player":"you","count":1}}""",
            cost: $$$"""
                {"seq":[{"exhaust":"this"},{"spend":"Y"},
                  {"{{{operation}}}":{"cards":"you","amount":2}}]}
                """);
        Card? source = null;
        Card? generator = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            generator = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        var ability = Assert.Single(runner.Actions(world, 0),
            candidate => candidate.Card == source!.ObjectId);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;
        long words = world.Random.Generator.WordsConsumed;
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, Kind: "preventDamage", Amount: 1,
            Card: source!.ObjectId, Affects: identity.ObjectId,
            Lasts: new Duration(Uses: 1)));

        if (mustTakeAll)
        {
            var failure = Assert.Throws<RulesNotImplementedException>(() =>
                runner.Act(world, ability, [generator!.ObjectId], []));
            Assert.Contains("only 1 was taken", failure.Message, StringComparison.Ordinal);
            Assert.True(source.Ready);
            Assert.Same(world.Seats[0].Hand, generator!.Area);
        }
        else
        {
            runner.Act(world, ability, [generator!.ObjectId], []);
            Assert.False(source.Ready);
            Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        }

        Assert.Equal(1, identity.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Kind == "preventDamage");
    }
}

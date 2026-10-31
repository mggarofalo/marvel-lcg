using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local characteristic effects; these fixtures do not admit Goblin content.</summary>
public sealed class ConstrainedResourceDiscardTests
{
    [Fact]
    public void PowerDrainUsesTwoCardBoostReceiptForEveryPlayersSelectedResources()
    {
        // 02041: "Each player must choose and discard 1 resource of any type
        // from their hand for each boost icon discarded this way."
        var runner = Runner("02041", """
            {"seq":[{"discardTop":{"from":"encounterDeck","count":2}},
              {"choose":{"options":[{"choose":{"options":[{"exhaust":"this"},{"exhaust":"you"}]}},{"exhaust":"you"}]}},
              {"eachPlayer":{"order":"playerOrder","effect":{"discardResourceIcons":{"boostIconsDiscarded":"game"}}}},
              {"placeCounters":{"card":"this","counter":"finished","count":1}}]}
            """);
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01121", deck); // Below the physical two-card mill.
        world.CreateCard("02041", deck); // Three numeric icons.
        world.CreateCard("02042", deck); // Two numeric icons plus a star.
        var hands = Enumerable.Range(0, 2).Select(player => new[]
        {
            world.CreateCard("01088", world.Seats[player].Hand),
            world.CreateCard("01089", world.Seats[player].Hand),
            world.CreateCard("01011", world.Seats[player].Hand),
            world.CreateCard("01004", world.Seats[player].Hand),
        }).ToArray();
        var source = world.CreateCard("02041", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 1);
        for (int nested = 0; nested < 2; nested++)
        {
            var choice = Prompt(world, runner);
            Answer(world, runner, choice, Decision.Take(choice.Affordances[0].Id));
        }
        for (int player = 0; player < 2; player++)
        {
            var prompt = Prompt(world, runner);
            Assert.Equal(player, prompt.Player);
            var option = Assert.Single(prompt.Affordances);
            var request = Assert.IsType<TargetRequest>(option.Targets);
            var selected = hands[player].Take(3).Select(card => card.ObjectId).ToArray();
            Assert.True(request.Allows(selected)); // Five icons, three cards.
            Assert.False(request.Allows(selected.Take(2).ToArray()));
            Assert.False(request.Allows(hands[player].Select(card => card.ObjectId).ToArray()));
            Assert.DoesNotContain(hands[1 - player][0].ObjectId, request.Legal);
            RejectWithoutChanges(world, runner, prompt, new(option.Id, selected.Take(2).ToArray(), []));
            Answer(world, runner, prompt, new(option.Id, selected, []));
            Assert.All(hands[player].Take(3), card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
            Assert.Same(world.Seats[player].Hand, hands[player][3].Area);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Single(deck.Cards);
        Assert.Equal(1, source.Tokens["c_finished"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [Rule("rr:wild-resource.3")]
    public void TombstoneOffersOnlyPrintedMentalOrPhysicalAndDiscardsOneChosenCard(int eligible)
    {
        // 02047: "discard a [mental] or a [physical] resource from your hand, if able."
        // "When resources are not being generated for a cost, a wild resource
        // does not have any characteristic other than 'wild resource.'"
        var runner = Runner("02047", """
            {"seq":[{"discardResourceIcons":{"count":1,"resources":"BR"}},
              {"choose":{"options":[{"exhaust":"this"},{"exhaust":"you"}]}}]}
            """);
        var world = Board(runner);
        var hand = world.Seats[1].Hand;
        var energy = world.CreateCard("01088", hand);
        var wild = world.CreateCard("01011", hand);
        Assert.Equal("G", Resources.GeneratedBy(wild.FaceId, world.Facts));
        var eligibleCards = Enumerable.Range(0, eligible)
            .Select(index => world.CreateCard(index % 2 == 0 ? "01089" : "01003", hand)).ToArray();
        var source = world.CreateCard("02047", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 1);
        var prompt = Prompt(world, runner);
        Assert.Equal(1, prompt.Player);
        if (eligible > 0)
        {
            var option = Assert.Single(prompt.Affordances);
            Assert.Equal("discardResourceIcons", option.Label);
            var request = Assert.IsType<TargetRequest>(option.Targets);
            Assert.Equal(eligible, request.Legal.Count);
            Assert.DoesNotContain(energy.ObjectId, request.Legal);
            Assert.DoesNotContain(wild.ObjectId, request.Legal);
            Assert.All(eligibleCards, card => Assert.True(request.Allows([card.ObjectId])));
            foreach (var invalid in new[] { Array.Empty<int>(), new[] { wild.ObjectId },
                new[] { eligibleCards[0].ObjectId, eligibleCards[0].ObjectId } })
                RejectWithoutChanges(world, runner, prompt, new(option.Id, invalid, []));
            Answer(world, runner, prompt, new(option.Id, [eligibleCards[0].ObjectId], []));
            Assert.Equal(DeckType.DiscardPile, eligibleCards[0].Area.Type);
            Assert.All(eligibleCards.Skip(1), card => Assert.Same(hand, card.Area));
            prompt = Prompt(world, runner);
        }
        Assert.Same(hand, energy.Area);
        Assert.Same(hand, wild.Area);
        Answer(world, runner, prompt, Decision.Take(prompt.Affordances[0].Id));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(6, 2)]
    public void ZeroCountSkipsAndInsufficientHandDiscardsAllEligible(int count, int handSize)
    {
        var runner = Runner("02041", $$$$"""
            {"seq":[{"discardResourceIcons":{{{{count}}}}},
              {"placeCounters":{"card":"this","counter":"finished","count":1}}]}
            """);
        var world = Board(runner);
        var cards = Enumerable.Range(0, handSize).Select(_ => world.CreateCard("01003", world.Seats[1].Hand)).ToArray();
        var source = world.CreateCard("02041", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 1);
        if (count > 0)
        {
            var prompt = Prompt(world, runner);
            var option = Assert.Single(prompt.Affordances);
            var ids = cards.Select(card => card.ObjectId).ToArray();
            Assert.True(option.Targets!.Allows(ids));
            Answer(world, runner, prompt, new(option.Id, ids, []));
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(count == 0 ? handSize : 0, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(1, source.Tokens["c_finished"]);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("{\"count\":1,\"resources\":\"BP\"}")]
    [InlineData("{\"count\":1,\"resources\":\"BB\"}")]
    [InlineData("{\"count\":1,\"resources\":\"\"}")]
    [InlineData("{\"count\":1,\"extra\":true}")]
    public void InvalidCountsOrResourceFiltersFailAdmission(string argument)
    {
        Assert.Throws<AbilityException>(() => Runner("02047", "{\"discardResourceIcons\":" + argument + "}"));
    }

    private static void RejectWithoutChanges(World world, AbilityRunner runner, Prompt prompt, Decision answer)
    {
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        int count = world.Agenda.Count;
        Assert.Throws<ArgumentException>(() => Answer(world, runner, prompt, answer));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(count, world.Agenda.Count);
        Assert.Equal(prompt.Affordances.Select(item => item.Id), Prompt(world, runner).Affordances.Select(item => item.Id));
    }

    private static Prompt Prompt(World world, AbilityRunner runner) =>
        Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));

    private static void Answer(World world, AbilityRunner runner, Prompt prompt, Decision answer) =>
        Sequence.Answer(world, world.Facts, runner, prompt, answer, []);

    private static AbilityRunner Runner(string face, string effect) => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"{{face}}","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{{effect}}}]}]}
        """));

    private static World Board(AbilityRunner runner)
    {
        var world = new World(CardCatalogData, players: 2) { Abilities = runner };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            world.CreateCard("01003", world.AreaOf(DeckType.PlayerDeck, PlayArea.Of(player)));
        }
        world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }
}

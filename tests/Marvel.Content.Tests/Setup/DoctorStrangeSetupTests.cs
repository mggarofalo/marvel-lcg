using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Tests;
using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Xunit;
using static Marvel.Content.Tests.Setup.DoctorStrangeSetupFixture;

namespace Marvel.Content.Tests.Setup;

public sealed class DoctorStrangeSetupTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void NormalRecipeBindsTheOwnedSeparateDeckAndRevealsOnlyAfterAllMulligans(int count)
    {
        // mc08 divider: 15 signature + 17 Protection + 8 Basic; identity, obligation,
        // nemesis and five Invocations are separate. The insert says "place ... facedown"
        // and "During the game ... top card ... faceup".
        var blueprints = DoctorStrangeSetupFixture.Blueprints(count);
        var runner = new AbilityRunner(AbilityLowering.Book(AbilityCatalog.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))));
        var world = WorldSetup.Deal(PrintedCards, blueprints, Heroes(count), 8123, runner);
        int owner = count - 1;
        var invocation = Assert.IsType<InvocationDeck>(world.Seats[owner].InvocationDeck);
        Assert.Equal(40, DeckConstruction.DeckSize(Dealer.DealOrder(
            SetupCatalog.Parse(Root().ToJsonString()), "rhino", ["doctor_strange"], facts: PrintedCards), PrintedCards));
        Assert.Equal(Invocations, invocation.Deck.Cards.Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.All(invocation.Deck.Cards, card => { Assert.Equal(owner, card.Owner); Assert.False(card.FaceUp); });
        Assert.Empty(invocation.Discard.Cards);
        Assert.Equal(DeckType.AdditionalDiscardPile, invocation.Discard.Type);
        Assert.Equal(PlayArea.Of(owner), invocation.Deck.PlayArea);
        Assert.Null(invocation.PublicTop);
        Assert.All(world.Seats.Take(owner), seat => Assert.Null(seat.InvocationDeck));
        Assert.DoesNotContain(world.Seats.SelectMany(seat => seat.Deck.Cards.Concat(seat.Hand.Cards)),
            card => Invocations.Contains(card.FaceId));
        for (int index = 0; index < blueprints.Count; index++)
        {
            Assert.Equal(index, world.Cards[index].ObjectId);
            Assert.Equal(blueprints[index].Spec.Split(','), world.Cards[index].Faces);
        }
        var typed = blueprints.Select((card, index) => (card, index)).Where(item => item.card.Slot == SetupSlot.InvocationDeck).ToArray();
        Assert.Equal(Invocations, typed.Select(item => item.card.Spec));
        Assert.Equal(Enumerable.Range(typed[0].index, 5), typed.Select(item => item.index));
        Assert.Equal(SetupSlot.PlayerDeck, blueprints[typed[0].index - 1].Slot);
        Assert.Equal(SetupSlot.MainScheme, blueprints[typed[^1].index + 1].Slot);

        var game = Game.Begin(world, PrintedCards, runner);
        for (int seat = 0; seat < count; seat++)
        {
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Null(invocation.PublicTop);
            Assert.All(invocation.Deck.Cards, card => Assert.False(card.FaceUp));
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        }
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Same(invocation.Deck.Cards[^1], invocation.PublicTop);
        Assert.Equal([invocation.PublicTop], invocation.Deck.Cards.Where(card => card.FaceUp));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void AllocationAndShuffleOrderUseOneDeterministicStream(int count)
    {
        // Engine choice: each seat's five allocated after its ordinary deck; their
        // shuffle follows player decks and encounter deck, before scenario abilities.
        var blueprints = DoctorStrangeSetupFixture.Blueprints(count);
        var random = new EngineRandom(8123);
        for (int seat = 0; seat < count; seat++)
            random.Shuffle(Ids(blueprints, SetupSlot.PlayerDeck, seat));
        var encounter = Ids(blueprints, SetupSlot.Encounter, -1);
        encounter.AddRange(blueprints.Select((card, index) => (card, index))
            .Where(item => item.card.Slot == SetupSlot.Obligation).Select(item => item.index));
        random.Shuffle(encounter);
        var expected = Ids(blueprints, SetupSlot.InvocationDeck, count - 1);
        random.Shuffle(expected);
        var world = WorldSetup.DealWithoutCardAbilities(PrintedCards, blueprints, Heroes(count), 8123);
        Assert.Equal(expected, world.Seats[^1].InvocationDeck!.Deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        var repeated = WorldSetup.DealWithoutCardAbilities(PrintedCards, blueprints, Heroes(count), 8123);
        Assert.Equal(world.Digest().Canonical(), repeated.Digest().Canonical());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("owner")]
    [InlineData("identity")]
    [InlineData("ordinary")]
    public void MalformedBindingRejectsBeforeAnySetupEvent(string fault)
    {
        var blueprints = DoctorStrangeSetupFixture.Blueprints(2).ToList();
        int index = blueprints.FindIndex(card => card.Slot == SetupSlot.InvocationDeck);
        switch (fault)
        {
            case "missing": blueprints.RemoveAt(index); break;
            case "duplicate": blueprints[index] = blueprints[index + 1]; break;
            case "owner": blueprints[index] = blueprints[index] with { Seat = 0 }; break;
            case "identity":
                int identity = blueprints.FindIndex(card => card.Slot == SetupSlot.Identity && card.Seat == 1);
                blueprints[identity] = blueprints[identity] with { Spec = "01019b,01019a" }; break;
            case "ordinary": blueprints[index] = blueprints[index] with { Slot = SetupSlot.PlayerDeck }; break;
        }
        var events = new List<GameEvent>();
        Assert.Throws<ArgumentException>(() => WorldSetup.DealWithoutCardAbilities(PrintedCards, blueprints, Heroes(2), 8123, events));
        Assert.Empty(events);
    }

    [Fact]
    public void MissingRecipeRejectsAndCustomDeckCannotAbsorbAnInvocation()
    {
        var root = Root();
        root["heroes"]!["doctor_strange"]!.AsObject().Remove("invocation_deck");
        var setup = SetupCatalog.Parse(root.ToJsonString());
        var blueprints = Marvel.Rules.Setup.Blueprints.From(Dealer.DealOrder(setup, "rhino", ["doctor_strange"], facts: PrintedCards), PrintedCards);
        Assert.Throws<ArgumentException>(() => WorldSetup.DealWithoutCardAbilities(PrintedCards, blueprints, ["Strange"], 1));
        var hero = setup.Hero("doctor_strange");
        var legal = new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck]);
        Assert.Equal(40, PlayerDeckAdmission.Admit(legal, hero.HeroDeck, 0, PrintedCards, hero.Hero[0]).Count);
        Assert.Throws<ArgumentException>(() => PlayerDeckAdmission.Admit(
            new([.. legal.Cards, "09032"]), hero.HeroDeck, 0, PrintedCards, hero.Hero[0]));
        var complete = SetupCatalog.Parse(Root().ToJsonString());
        var custom = Dealer.DealOrder(complete, "rhino", ["doctor_strange"], facts: PrintedCards, playerDecks: [legal]);
        Assert.Equal(5, custom.Count(card => card.Source == CreationSource.InvocationDeck));
        Assert.Equal(40, DeckConstruction.DeckSize(custom, PrintedCards));
        Assert.Contains("doctor_strange", RuntimeProductAdmission.Discover(complete, PrintedCards,
            PrintedCards.Faces.ToHashSet(StringComparer.Ordinal)).Setup.HeroNames);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[\"09032\",\"09033\",\"09034\",\"09035\",\"09035\"]")]
    [InlineData("null")]
    [InlineData("{}")]
    public void MalformedRecipeCannotBecomeAPlausibleDeck(string json)
    {
        var root = Root();
        root["heroes"]!["doctor_strange"]!["invocation_deck"] = System.Text.Json.Nodes.JsonNode.Parse(json);
        if (json.StartsWith('['))
            Assert.Throws<ArgumentException>(() => SetupCatalog.Parse(root.ToJsonString()));
        else
            Assert.Throws<System.Text.Json.JsonException>(() => SetupCatalog.Parse(root.ToJsonString()));
    }

    private static List<int> Ids(IReadOnlyList<CardBlueprint> blueprints, SetupSlot slot, int seat) =>
        blueprints.Select((card, index) => (card, index))
            .Where(item => item.card.Slot == slot && item.card.Seat == seat).Select(item => item.index).ToList();
}

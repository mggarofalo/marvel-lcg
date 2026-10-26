using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic 05026 body; this does not author or admit Ms. Marvel content.</summary>
public sealed class GlobalPersonaCountTests : CoreCardTestBase
{
    private const string Effect = """
        {"discardTop":{"from":"yourDeck","player":"each","count":{"add":[
          {"count":{"query":"allies"}},
          {"count":{"withTrait":{"cards":{"query":"supports"},"trait":"PERSONA"}}}
        ]}}}
        """;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void SyntheticGenerationWhyDiscardsGlobalCombinedCountFromEveryDeck(int resolver)
    {
        // Printed 05026: "Discard the top card of each player's deck for each ally
        // and Persona support in play." Ordinary supports do not contribute.
        var world = Board();
        var runner = Runner();
        var source = world.CreateCard("05026", world.AreaOf(DeckType.SideSchemesArea));
        int[] counts = world.Seats.Select(seat => seat.Deck.Cards.Count).ToArray();

        var result = runner.ExecuteReveal(world, source, resolver);

        Assert.Equal([4, 4], world.Seats.Select((seat, index) => counts[index] - seat.Deck.Cards.Count));
        Assert.Equal(8, result.Events.OfType<CardsMoved>().Where(item => item.Verb == "Discard")
            .Sum(item => item.Cards.Count));
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [Rule("rr:player-deck.3")]
    public void EmptyShortAndExactlyExhaustedDecksStopAtTheirOriginalBoundary(int size)
    {
        // "No further cards are discarded from the newly shuffled deck."
        var world = Board();
        var deck = world.Seats[0].Deck;
        foreach (var card in deck.Cards.Skip(size).ToArray())
            World.MoveToTop(card, world.Seats[0].Hand);
        var source = world.CreateCard("05026", world.AreaOf(DeckType.SideSchemesArea));
        var result = Runner().ExecuteReveal(world, source, 1);

        Assert.Equal(size, result.Events.OfType<CardsMoved>()
            .Where(item => item.Verb == "Discard" && item.From.Zone == "PlayerDeck"
                && item.From.Owner == 0).Sum(item => item.Cards.Count));
        Assert.Equal(size, deck.Cards.Count);
        Assert.Equal(size == 0 ? 0 : 1, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(4, world.Seats[1].Deck.Cards.Count);
    }

    [Fact]
    public void ZeroCountLeavesEveryDeckAndRandomStreamUntouched()
    {
        var world = Hero("01001a", 2);
        foreach (var seat in world.Seats) world.CreateCard("01088", seat.Deck);
        var source = world.CreateCard("05026", world.AreaOf(DeckType.SideSchemesArea));
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var result = Runner().ExecuteReveal(world, source, 1);
        Assert.Empty(result.Events);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
    }

    [Fact]
    public void GlobalSupportQueryUsesInPlayKindRegardlessOfControllerAndIsPure()
    {
        var world = Board();
        var source = world.CreateCard("05026", world.AreaOf(DeckType.SideSchemesArea));
        // Ownership and location need not agree: the global query counts this once.
        var foreign = world.CreateCard("05006", world.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(1), cardOwner: 0));
        world.CreateCard("05008", world.Seats[0].Hand);
        world.CreateCard("01067", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0)));
        var context = new AbilityQueryContext(world, source, new Occurrence(0, []), 1,
            source.Incarnation, null, null, null, []);
        var evaluator = new CardQueryEvaluator(context, null);
        string digest = world.Digest().Canonical();
        var areas = world.Areas.ToArray();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        bool busy = world.Agenda.IsBusy;
        var expected = world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
            && Cards.Kind(card.FaceId) == CardKind.Support).ToArray();
        Assert.Equal(expected, evaluator.Evaluate(AbilityCardQuery.Supports));
        Assert.Equal(expected, evaluator.Evaluate(AbilityCardQuery.Supports));
        Assert.Contains(foreign, expected);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(areas, world.Areas);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(busy, world.Agenda.IsBusy);
        var control = Board();
        control.CreateCard("05026", control.AreaOf(DeckType.SideSchemesArea));
        control.CreateCard("05006", control.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));
        control.CreateCard("05008", control.Seats[0].Hand);
        control.CreateCard("01067", control.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0)));
        Runner().ExecuteReveal(world, source, 1);
        Runner().ExecuteReveal(control, control.Cards[source.ObjectId], 1);
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
    }

    [Fact]
    public void SingleEachPlayerInstructionFreezesItsAmountBeforeTheFirstDeckChanges()
    {
        // Engine choice: evaluate an instruction's numeric expression once. The
        // first discard changes the expression, but not the second player's quota.
        var world = Board();
        var source = world.CreateCard("05026", world.AreaOf(DeckType.SideSchemesArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05026","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"discardTop":{"from":"yourDeck","player":"each","count":
                {"floorDiv":{"value":{"count":{"cardsIn":{"area":"yourDeck"}}},"divisor":2}}}}
            }]}]}
            """));
        runner.ExecuteReveal(world, source, 0);
        Assert.Equal([4, 4], world.Seats.Select(seat => seat.Deck.Cards.Count));
    }

    [Fact]
    public void EliminatedSeatIsNotAnEachPlayerDiscardRecipient()
    {
        var world = Board();
        var source = world.CreateCard("05026", world.AreaOf(DeckType.SideSchemesArea));
        // A player's elimination procedure removes their cards from play. This
        // synthetic state retains only their deck to distinguish recipients.
        foreach (var card in world.Cards.Where(card => card.Area.PlayArea == PlayArea.Of(0)
            && DeckTypes.IsInPlay(card.Area.Type)).ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        world.Seats[0].Eliminated = true;
        Runner().ExecuteReveal(world, source, 1);
        Assert.Equal(8, world.Seats[0].Deck.Cards.Count);
        Assert.Equal(6, world.Seats[1].Deck.Cards.Count);
    }

    private static World Board()
    {
        var world = Hero("01001a", 2);
        foreach (var seat in world.Seats)
        {
            world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(seat.Index), cardOwner: seat.Index));
            world.CreateCard(seat.Index == 0 ? "05006" : "05008",
                world.AreaOf(DeckType.SupportsArea, PlayArea.Of(seat.Index), cardOwner: seat.Index));
            world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(seat.Index), cardOwner: seat.Index));
            for (int index = 0; index < 8; index++) world.CreateCard("01088", seat.Deck);
        }
        for (int index = 0; index < 8; index++) world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse($$$$$$"""
        {"cards":[{"card":"05026","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{{{{{{Effect}}}}}}
        }]}]}
        """));
}

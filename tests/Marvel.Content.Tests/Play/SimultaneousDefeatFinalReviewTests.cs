using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent synthetic contracts for shared defeat-window continuations.</summary>
public sealed class SimultaneousDefeatFinalReviewTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private const string ObserverBook = """
        {"cards":[{"card":"01101","abilities":[{
          "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
          "effect":{"placeAccelerationToken":1}}]}]}
        """;

    [Fact]
    public void RepeatedQueriesAndRejectedOfferPreserveExactOccurrencesAndRetryOutcome()
    {
        // Query and rejected-answer atomicity are engine contract choices.
        Assert.Equal(RunObserved(probe: false), RunObserved(probe: true));
    }

    [Rule("rr:interrupt.5")]
    [Fact]
    public void DecliningSharedOpportunityDeclinesBothOccurrencesForThatSeat()
    {
        // "Once all players decide they do not wish to resolve any (further) interrupts"; none is selected implicitly.
        var (world, first, second, scheme, _) = Board(ObserverBook);
        var events = new List<GameEvent>();
        var result = Resolve(world, first, second, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(2, prompt.Affordances.Count);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Decline, events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void NestedUnrelatedDefeatRetainsItsOwnInterruptWindow()
    {
        // Step 6 includes "when [character] would be defeated". A nested recipient
        // has its own step 6 even while the outer simultaneous cohort is suspended.
        const string book = """
            {"cards":[
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"this"},
                "effect":{"dealDamage":{"cards":{"titled":"Hydra Bomber"},"amount":10}}}]},
              {"card":"01110","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"this"},
                "effect":{"heal":{"card":"this","amount":10}}}]}
            ]}
            """;
        var (world, first, second, _, _) = Board(book);
        var nested = world.CreateCard("01110", first.Area);
        var events = new List<GameEvent>();
        var result = Resolve(world, first, second, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Opportunity, prompt.Asking);
        Assert.Equal(nested.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
        Assert.False(result.IsComplete);
        Assert.Equal(10, nested.Damage);
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(prompt.Affordances[0].Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(0, nested.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, nested.Area.Type);
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameSourceStepSixOffersIdentifyTheirDifferentImminentRecipients(bool sameTitle)
    {
        // Player comprehension contract: opaque offer handles cannot explain which
        // imminent defeat an otherwise identical global interrupt will answer.
        var (world, first, second, _, _) = Board(ObserverBook);
        var recipient = sameTitle ? world.CreateCard("01102", second.Area) : first;
        Resolve(world, recipient, second, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, []));
        Assert.Equal(2, prompt.Affordances.Select(offer => offer.Id).Distinct().Count());
        Assert.Equal(2, prompt.Affordances.Select(offer => JsonSerializer.Serialize(offer with { Id = 0 })).Distinct().Count());
    }

    private static string RunObserved(bool probe)
    {
        var (world, first, second, scheme, runner) = Board(ObserverBook);
        var events = new List<GameEvent>();
        var result = Resolve(world, first, second, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        var handles = prompt.Affordances.Select(offer => offer.Id).ToArray();
        Assert.Equal(2, handles.Distinct().Count());
        if (probe)
        {
            string before = Snapshot(world, events);
            var pending = world.Agenda.Outstanding.ToArray();
            var occurrence = world.Agenda.Occurrence;
            for (int index = 0; index < 3; index++)
            {
                _ = runner.Actions(world, 0);
                var repeated = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
                Assert.Equal(handles, repeated.Affordances.Select(offer => offer.Id));
            }
            Assert.Equal(before, Snapshot(world, events));
            Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, runner, prompt,
                Decision.Take(int.MaxValue), events));
            Assert.Equal(before, Snapshot(world, events));
            Assert.Same(occurrence, world.Agenda.Occurrence);
            Assert.Equal(pending, world.Agenda.Outstanding);
        }
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(handles[1]), events);
        var remaining = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Single(remaining.Affordances);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Sequence.Answer(world, Cards, runner, remaining, Decision.Take(remaining.Affordances[0].Id), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.True(result.IsComplete);
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
        return Snapshot(world, events);
    }

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage, world.Activation, world.Attack,
        Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Subject, step.OccurrenceId, step.ActivationId }),
        Events = events.Select(item => JsonSerializer.Serialize(item, item.GetType())),
    });

    private static SimultaneousDirectDamageResult Resolve(World world, Card first, Card second, List<GameEvent> events) =>
        SimultaneousDirectDamage.Resolve(world, Cards, new[] { first, second }.Select(card =>
            new DamageRequest(world.Seats[0].IdentityCard, card, 10, "synthetic-final-review", "Deal_Damage", 0)).ToArray(), events);

    private static (World World, Card First, Card Second, Card Scheme, AbilityRunner Runner) Board(string book)
    {
        var world = new World(Cards, 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("01001a", world.Seats[0].Hero);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book));
        world.Abilities = runner;
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        return (world, world.CreateCard("01101", area), world.CreateCard("01102", area), scheme, runner);
    }
}

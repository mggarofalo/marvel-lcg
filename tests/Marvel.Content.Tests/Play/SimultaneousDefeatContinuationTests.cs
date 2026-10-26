using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic interrupt and incarnation contracts for simultaneous direct damage.</summary>
public sealed class SimultaneousDefeatContinuationTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:forced.5")]
    [Rule("rr:would.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstPlayerOrdersStepSixAndEachHealingReplacementInvalidatesOnlyItsOwnDefeat(bool reverse)
    {
        // "the first player determines the order"; a healed recipient's imminent
        // defeat is invalid, while the other recipient retains its interrupt.
        var (world, first, second, _) = Board("ForcedInterrupt", healing: true);
        world.FirstPlayer = 1;
        var events = new List<GameEvent>();
        var result = Resolve(world, reverse ? [second, first] : [first, second], events);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(1, order.Player);
        Assert.Equal(2, order.Affordances.Count);
        Sequence.Answer(world, Cards, world.Abilities, order, Decision.Take(second.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.All(new[] { first, second }, card =>
        {
            Assert.Equal(0, card.Damage);
            Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type);
        });
        Assert.All(result.Recipients, recipient => Assert.Equal(Damage.Outcome.NotDefeated, recipient.Outcome));
        Assert.Empty(result.Execution.Occurrence.Defeats);
    }

    [Rule("rr:leaves-play.1")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void ReenteredRecipientCannotUseItsOldStepSixOpportunityOrDepartWithItsOldCohort()
    {
        // A card returning to play is "a new copy of the card". The captured
        // damage belongs to its old incarnation even while a sibling suspends.
        var (world, first, second, scheme) = Board("Interrupt", healing: false);
        var events = new List<GameEvent>();
        var result = Resolve(world, [first, second], events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        int original = second.Incarnation;
        World.MoveToTop(second, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(second, first.Area);
        Assert.NotEqual(original, second.Incarnation);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(first.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(DeckType.EngagedEnemiesArea, second.Area.Type);
        Assert.Equal(original, result.Recipients[1].TargetIncarnation);
        Assert.Equal(Damage.Outcome.NotDefeated, result.Recipients[1].Outcome);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Equal(first.ObjectId, Assert.Single(result.Execution.Occurrence.Defeats).Card);
    }

    [Rule("rr:interrupt.2")]
    [Rule("rr:damage.step.8")]
    [Fact]
    public void OptionalSecondRecipientCanBeSelectedFirstWithoutLosingTheFirstOccurrence()
    {
        // Multiple interrupts may be triggered; neither recipient leaves until
        // the player's chosen order has completed both exact occurrences.
        var (world, first, second, scheme) = Board("Interrupt", healing: false);
        var events = new List<GameEvent>();
        var result = Resolve(world, [first, second], events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(second.ObjectId), events);
        var remaining = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(first.ObjectId, Assert.Single(remaining.Affordances).Id);
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
        Sequence.Answer(world, Cards, world.Abilities, remaining, Decision.Take(first.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
    }

    [Rule("rr:leaves-play.1")]
    [Rule("rr:damage.step.7")]
    [Fact]
    public void ReenteredRecipientCannotResolveItsOldOwnWhenDefeatedAbility()
    {
        // A returning card is a new copy; the old When Defeated address must
        // neither execute on that copy nor remove it after a sibling's choice.
        var (world, first, second, scheme) = Board("WhenDefeated", healing: false, "WhenCardDefeated");
        var events = new List<GameEvent>();
        var result = Resolve(world, [first, second], events);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Order, order.Asking);
        World.MoveToTop(second, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(second, first.Area);
        Sequence.Answer(world, Cards, world.Abilities, order, Decision.Take(first.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(DeckType.EngagedEnemiesArea, second.Area.Type);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
    }

    private static SimultaneousDirectDamageResult Resolve(World world, Card[] recipients, List<GameEvent> events)
    {
        var cause = new Marvel.Rules.Timing.Occurrence(0, ["synthetic"]);
        return SimultaneousDirectDamage.Resolve(world, Cards, recipients.Select(card =>
            new DamageRequest(world.Seats[0].IdentityCard, card, 10, "synthetic", "Deal_Damage", 0)
                { RecordDefeatOn = cause }).ToArray(), events);
    }

    private static (World World, Card First, Card Second, Card Scheme) Board(string timing, bool healing, string condition = "WhenCardWouldBeDefeated")
    {
        string effect = healing ? "{\"heal\":{\"card\":\"this\",\"amount\":10}}" : "{\"placeAccelerationToken\":1}";
        string Row(string card) => "{\"card\":\"" + card + "\",\"abilities\":[{\"trigger\":{\"event\":\"WhenCardWouldBeDefeated\",\"timing\":\""
            + timing + "\",\"subject\":\"this\"},\"effect\":" + effect + "}]}";
        var world = new World(Cards, 2);
        for (int seat = 0; seat < 2; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard = world.CreateCard(seat == 0 ? "01001a" : "01010a", world.Seats[seat].Hero);
        }
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(("{\"cards\":[" + Row("01101") + "," + Row("01102") + "]}")
            .Replace("WhenCardWouldBeDefeated", condition, StringComparison.Ordinal)));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        return (world, world.CreateCard("01101", area), world.CreateCard("01102", area), scheme);
    }
}

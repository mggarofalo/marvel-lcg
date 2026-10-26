using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic timing contracts; these abilities are not printed card text.</summary>
public sealed class SimultaneousDamageBarrierReviewTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:damage.step.6")]
    [Rule("rr:damage.step.7")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EveryWouldBeDefeatedEffectPrecedesAnyWhenDefeatedEffect(bool stepSevenRecipientFirst)
    {
        // "Each step is resolved simultaneously for both targets": step 7
        // cannot precede the other recipient's step 6 even if nobody leaves play.
        const string book = """
            {"cards":[
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
                "effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":2}}}]},
              {"card":"01102","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"this"},
                "effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}}]}
            ]}
            """;
        var (world, first, other, scheme) = Board(book);
        var events = new List<GameEvent>();
        Resolve(world, stepSevenRecipientFirst ? [first, other] : [other, first], events);
        Sequence.Finish(world, Cards, world.Abilities, events);

        Assert.Equal(3, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(new long?[] { 1, 3 }, events.OfType<FieldSet>()
            .Where(change => change.Card == scheme.ObjectId && change.Field == "k_threat")
            .Select(change => change.To));
    }

    [Rule("rr:damage.step.6")]
    [Rule("rr:damage.step.7")]
    [Rule("rr:damage.step.8")]
    [Theory]
    [InlineData("WhenCardWouldBeDefeated", true)]
    [InlineData("WhenCardWouldBeDefeated", false)]
    [InlineData("WhenCardDefeated", true)]
    [InlineData("WhenCardDefeated", false)]
    public void DyingGlobalObserverAnswersBothRecipientsBeforeDeparture(string condition, bool observerFirst)
    {
        // ruling:304a4798e0b453b4: "each step is resolved simultaneously for both targets."
        // A synthetic forced observer counts both step-6/step-7 instances before
        // step 8 removes it. Reversing declaration order is the distinguishing control.
        var (world, observer, other, scheme) = Board(GlobalObserver(condition, "ForcedInterrupt"));
        var result = Resolve(world, observerFirst ? [observer, other] : [other, observer], []);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, []));
        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(2, order.Affordances.Count);
        Sequence.Answer(world, Cards, world.Abilities, order, Decision.Take(order.Affordances[0].Id), []);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, []));

        Assert.True(result.IsComplete);
        Assert.All(new[] { observer, other }, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
    }

    [Rule("rr:damage.step.6")]
    [Rule("rr:damage.step.8")]
    [Fact]
    public void SuspendedWouldBeDefeatedWindowDoesNotReleaseAnEarlierRecipient()
    {
        // "When [character] would be defeated" is step 6 for BOTH recipients;
        // step 8 must wait even when the first optional interrupt suspends.
        var (world, observer, other, scheme) = Board(GlobalObserver("WhenCardWouldBeDefeated", "Interrupt"));
        var events = new List<GameEvent>();
        var result = Resolve(world, [observer, other], events);
        var first = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Opportunity, first.Asking);
        Assert.False(result.IsComplete);
        Assert.Equal(2, first.Affordances.Count);
        Assert.All(new[] { observer, other }, card => Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
        Sequence.Answer(world, Cards, world.Abilities, first, Decision.Take(first.Affordances[0].Id), events);

        var second = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Opportunity, second.Asking);
        Assert.Equal(DeckType.EngagedEnemiesArea, observer.Area.Type);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Sequence.Answer(world, Cards, world.Abilities, second, Decision.Take(Assert.Single(second.Affordances).Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.IsComplete);
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
    }

    [Rule("rr:forced.5")]
    [Rule("rr:damage.step.7")]
    [Fact]
    public void FirstPlayerOrdersMandatoryDefeatAbilitiesAcrossRecipients()
    {
        // "If two or more forced abilities would initiate at the same moment,
        // the first player determines the order." Simultaneous step 7 does not
        // let recipient declaration order select the first When Defeated ability.
        const string book = """
            {"cards":[
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
                "effect":{"placeAccelerationToken":1}}]},
              {"card":"01102","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
                "effect":{"seq":[{"placeAccelerationToken":1},{"placeAccelerationToken":1}]}}]}
            ]}
            """;
        var (world, observer, other, scheme) = Board(book, players: 2);
        world.FirstPlayer = 1;
        var events = new List<GameEvent>();
        Resolve(world, [observer, other], events);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));

        Assert.Equal(Question.Order, order.Asking);
        Assert.Equal(1, order.Player);
        Assert.Equal(2, order.Affordances.Count);
        Assert.Contains(order.Affordances, option => option.Id == observer.ObjectId);
        Assert.Contains(order.Affordances, option => option.Id == other.ObjectId);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.All(new[] { observer, other }, card => Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
        Sequence.Answer(world, Cards, world.Abilities, order, Decision.Take(other.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(3, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
    }

    private static string GlobalObserver(string condition, string timing) => """
        {"cards":[{"card":"01101","abilities":[{
          "trigger":{"event":"@condition","timing":"@timing","subject":"game"},
          "effect":{"placeAccelerationToken":1}}]}]}
        """.Replace("@condition", condition, StringComparison.Ordinal)
            .Replace("@timing", timing, StringComparison.Ordinal);

    private static SimultaneousDirectDamageResult Resolve(World world, Card[] recipients, List<GameEvent> events) =>
        SimultaneousDirectDamage.Resolve(world, Cards, recipients.Select(card =>
            new DamageRequest(world.Seats[0].IdentityCard, card, 10, "synthetic-barrier", "Deal_Damage", 0)).ToArray(), events);

    private static (World World, Card Observer, Card Other, Card Scheme) Board(string book, int players = 1)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            world.CreateSeat($"p{player}");
            world.Seats[player].IdentityCard = world.CreateCard(player == 0 ? "01001a" : "01010a", world.Seats[player].Hero);
        }
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var observer = world.CreateCard("01101", area);
        var other = world.CreateCard("01102", area);
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(book));
        return (world, observer, other, scheme);
    }
}

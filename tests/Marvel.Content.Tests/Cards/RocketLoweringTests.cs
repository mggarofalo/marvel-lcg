using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// Malformed shapes of the vocabulary the Rocket Raccoon pack added are refused before gameplay.
public sealed class RocketLoweringTests
{
    private const string Action =
        """{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"}""";
    private const string Excess =
        """{"event":"WhenAttackExcessDamageWouldBeDealt","timing":"Interrupt","actor":"you","target":"enemy"}""";
    private const string Revealed =
        """{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"}""";

    [Theory]
    [InlineData(Action, """{"moveCounters":{"from":"this","counter":"charge","count":1}}""", "to")]
    [InlineData(Action, """{"moveCounters":{"from":"this","to":"this","counter":"","count":1}}""", "nonempty")]
    [InlineData(Action, """{"moveCounters":{"from":"this","to":"this","counter":"charge","count":0}}""", "positive")]
    [InlineData(Action, """{"healEachTimeYouDamageAnEnemy":{"amount":2,"until":"EndOfRound"}}""", "EndOfTurn")]
    [InlineData(Action, """{"healEachTimeYouDamageAnEnemy":{"amount":0,"until":"EndOfTurn"}}""", "positive")]
    [InlineData(Excess, """{"increaseExcessDamage":0}""", "positive")]
    [InlineData(Action, """{"increaseExcessDamage":1}""", "excess-damage interrupt")]
    [InlineData(Revealed, """{"discardUntil":{"from":"encounterDeck","kind":"minion","then":"putIntoPlay","revealStatus":"tough"}}""", "requires reveal")]
    [InlineData(Revealed, """{"discardUntil":{"from":"encounterDeck","kind":"minion","then":"reveal","revealStatus":"burn"}}""", "not a status")]
    [InlineData(Action, """{"discardUntil":{"from":"yourDeck","kind":"Ally","then":"hand","revealStatus":"tough"}}""", "encounter-deck reveal")]
    public void NewVocabularyRefusesAnyOtherShape(string trigger, string effect, string says)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(
            $$$"""{"cards":[{"card":"16029a","abilities":[{"trigger":{{{trigger}}},"effect":{{{effect}}}}]}]}""")));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMoveCarriesItsEndsCounterAndCount()
    {
        var program = AbilityLowering.Book(AbilityCatalog.Parse(
            """{"cards":[{"card":"16034","abilities":[{"trigger":""" + Action
            + ""","effect":{"moveCounters":{"from":"this","to":"chosen","counter":"charge","count":1}}}]}]}"""));
        var move = Assert.IsType<AbilityCounterEffect.MoveCounters>(Assert.Single(program.On("16034")).Effect);
        Assert.Equal("charge", move.Counter);
        Assert.Equal(1, move.Count);
    }

    [Fact]
    public void ADiscardUntilRevealCarriesItsStatus()
    {
        var program = AbilityLowering.Book(AbilityCatalog.Parse(
            """{"cards":[{"card":"16057","abilities":[{"trigger":""" + Revealed
            + ""","effect":{"discardUntil":{"from":"encounterDeck","kind":"minion","then":"reveal","revealStatus":"tough"}}}]}]}"""));
        var discard = Assert.IsType<AbilityEffect.DiscardUntil>(Assert.Single(program.On("16057")).Effect);
        Assert.Equal("tough", discard.RevealStatus);
    }
}

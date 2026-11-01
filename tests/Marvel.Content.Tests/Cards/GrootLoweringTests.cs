using Marvel.Cards.Dsl;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// Malformed shapes of the vocabulary the Groot pack added are refused before gameplay.
public sealed class GrootLoweringTests
{
    private const string Action =
        """{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"}""";
    private const string Damage =
        """{"event":"WhenDamageWouldBeDealt","damage":"taken","timing":"ForcedInterrupt","target":"this"}""";

    [Theory]
    [InlineData(Damage, """{"preventDamageWithCounters":{"card":"this"}}""", "counter")]
    [InlineData(Damage, """{"preventDamageWithCounters":{"counter":"growth"}}""", "card")]
    [InlineData(Damage, """{"preventDamageWithCounters":{"card":"this","counter":"growth","amount":1}}""", "amount")]
    [InlineData(Damage, """{"preventDamage":{"card":"trigger.target","amount":{"incomingDamage":"game"}}}""", "trigger")]
    [InlineData(Action, """{"reduceNextCardCost":{"player":"you","amount":1,"until":"EndOfRound"}}""", "EndOfTurn")]
    [InlineData(Action, """{"reduceNextCardCost":{"player":"any","amount":1,"until":"EndOfTurn"}}""", "phase")]
    public void NewVocabularyRefusesAnyOtherShape(string trigger, string effect, string says)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(
            $$$"""{"cards":[{"card":"16001a","abilities":[{"trigger":{{{trigger}}},"effect":{{{effect}}}}]}]}""")));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APlayerReductionCarriesItsTraitAndTurnExpiry()
    {
        var program = AbilityLowering.Book(AbilityCatalog.Parse(
            """{"cards":[{"card":"16024","abilities":[{"trigger":""" + Action
            + ""","effect":{"reduceNextCardCost":{"player":"you","trait":"SUPERPOWER","amount":1,"until":"EndOfTurn"}}}]}]}"""));
        var reduction = Assert.IsType<AbilityPricingEffect.ReduceNextCardCost>(Assert.Single(program.On("16024")).Effect);
        Assert.Equal("SUPERPOWER", reduction.Filter!.Trait);
        Assert.Equal(Marvel.Rules.Timing.TimingPoints.EndOfTurn, reduction.Until);
        Assert.False(reduction.AnyPlayer);
    }
}

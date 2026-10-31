using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// Malformed shapes of the vocabulary the Wasp pack added are refused before gameplay.
public sealed class WaspLoweringTests
{
    [Theory]
    [InlineData("""{"target":{"query":"attackableEnemies"},"total":0,"effect":{"dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1,"simultaneous":1}}}""", "positive integer")]
    [InlineData("""{"target":{"query":"attackableEnemies"},"total":4,"count":2,"effect":{"dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1,"simultaneous":1}}}""", "target count")]
    [InlineData("""{"target":{"query":"attackableEnemies"},"total":4,"effect":{"dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":2,"simultaneous":1}}}""", "1-damage")]
    [InlineData("""{"target":{"query":"attackableEnemies"},"total":4,"effect":{"dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1}}}""", "1-damage")]
    [InlineData("""{"target":{"query":"attackableEnemies"},"total":4,"effect":{"dealAttackDamage":{"cards":"chosen","amount":1,"simultaneous":1}}}""", "1-damage")]
    public void ADividedAttackRefusesAnyOtherShape(string attack, string says)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Action($$"""{"attack":{{attack}}}""")));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADividedAttackLowersItsTotalAsRepeatedPowerTargets()
    {
        var program = AbilityLowering.Book(Action("""{"attack":{"target":{"query":"attackableEnemies"},"total":4,"effect":{"dealAttackDamage":{"cards":{"query":"powerTargets"},"amount":1,"simultaneous":1}}}}"""));
        var power = Assert.IsType<AbilityEffect.Power>(Assert.Single(program.On("13006")).Effect);
        Assert.True(power.Distributed);
        Assert.Equal(new AbilityNumber.Constant(4), power.TargetCount);
        Assert.True(Assert.IsType<AbilityEffect.AttackDamage>(power.Effect).Distributed);
    }

    [Theory]
    [InlineData("""{"card":"yourHero","statuses":[]}""", "nonempty list")]
    [InlineData("""{"card":"yourHero","statuses":["stunned","stunned"]}""", "distinct")]
    [InlineData("""{"card":"yourHero","statuses":["dazed"]}""", "not a status kind")]
    [InlineData("""{"card":"yourHero","statuses":["stunned"],"kind":"x"}""", "kind")]
    public void AStatusFilterRefusesAnyOtherShape(string from, string says)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Action(
            """{"chooseCard":{"from":{"statusCardsOn":""" + from + """},"effect":{"discard":"chosen"}}}""")));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AStatusFilterLowersItsKinds()
    {
        var program = AbilityLowering.Book(Action(
            """{"chooseCard":{"from":{"statusCardsOn":{"card":"yourHero","statuses":["stunned","confused"]}},"effect":{"discard":"chosen"}}}"""));
        var choose = Assert.IsType<AbilityEffect.ChooseCard>(Assert.Single(program.On("13006")).Effect);
        var from = Assert.IsType<AbilityCardSelection.StatusCardsOn>(choose.From);
        Assert.Equal(["stunned", "confused"], from.Statuses);
    }

    [Theory]
    [InlineData("""{"card":"this","resource":"B"}""", "only energy")]
    [InlineData("""{"card":"you","resource":"Y"}""", "this")]
    public void ATypedOverpaymentAdmitsOnlyThisCardsEnergy(string excess, string says)
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Action(
            """{"placeCounters":{"card":"this","counter":"pym","count":{"entryResourceExcess":""" + excess + """}}}""")));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADiscardShuffleRefusesAnUnknownResource()
    {
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(Action(
            """{"chooseDiscardToShuffle":{"max":2,"min":0,"differentTitles":"false","resource":"Q"}}""")));
        Assert.Contains("resource", failure.Message, StringComparison.Ordinal);
    }

    internal static AbilityBook Action(string effect) => AbilityCatalog.Parse($$"""
        {"cards":[{"card":"13006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "labels":["attack"],
          "effect":{{effect}}}]}]}
        """);
}

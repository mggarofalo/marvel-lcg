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

    internal static AbilityBook Action(string effect) => AbilityCatalog.Parse($$"""
        {"cards":[{"card":"13006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "labels":["attack"],
          "effect":{{effect}}}]}]}
        """);
}

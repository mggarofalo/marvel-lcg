using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class BaseDefinitionLoweringTests
{
    private const string Definition = """{"defineBaseValue":{"field":"attack","value":{"remainingHealth":"this"}}}""";
    private const string Constant = """{"trigger":{"timing":"Constant","subject":"this"},"effect":__EFFECT__}""";

    [Fact]
    public void ABaseDefinitionIsTypedAndOwnsItsNumericExpression()
    {
        var program = Compile(Constant.Replace("__EFFECT__", Definition, StringComparison.Ordinal));
        var definition = Assert.IsType<AbilityEffect.DefineBaseValue>(Assert.Single(program.Abilities).Effect);
        Assert.Equal("attack", definition.Field);
        var value = Assert.IsType<AbilityNumber.CardValue>(definition.Value);
        Assert.Equal(AbilityCardNumberProperty.RemainingHealth, value.Property);
    }

    [Theory]
    [InlineData("Action", "constant")]
    [InlineData("ForcedResponse", "constant")]
    public void DefinitionsCannotBecomeTriggeredAssignments(string timing, string diagnostic)
    {
        string ability = """{"trigger":{"event":"WhenActionTriggered","timing":"__TIMING__","subject":"this"},"effect":__EFFECT__}"""
            .Replace("__TIMING__", timing, StringComparison.Ordinal).Replace("__EFFECT__", Definition, StringComparison.Ordinal);
        Assert.Contains(diagnostic, Assert.Throws<AbilityException>(() => Compile(ability)).Message);
    }

    [Theory]
    [InlineData("health")]
    [InlineData("guard")]
    [InlineData("misspelled")]
    public void UnsupportedBaseFieldsFailInsteadOfBecomingIgnoredMetadata(string field)
    {
        string effect = Definition.Replace("attack", field, StringComparison.Ordinal);
        Assert.Contains("basic-power base", Assert.Throws<AbilityException>(() =>
            Compile(Constant.Replace("__EFFECT__", effect, StringComparison.Ordinal))).Message);
    }

    [Fact]
    public void CompetingConditionalAndNestedDefinitionsAreRejectedBeforePlay()
    {
        string ability = Constant.Replace("__EFFECT__", Definition, StringComparison.Ordinal);
        Assert.Contains("duplicate base definition", Assert.Throws<AbilityException>(() => Compile(ability + "," + ability)).Message);
        string nested = Constant.Replace("__EFFECT__", "{\"seq\":[" + Definition + "]}", StringComparison.Ordinal);
        Assert.Contains("constant ability root", Assert.Throws<AbilityException>(() => Compile(nested)).Message);
        string conditional = ability.Replace("\"effect\":", "\"when\":{\"inForm\":{\"player\":\"you\",\"form\":\"hero\"}},\"effect\":", StringComparison.Ordinal);
        Assert.Contains("unconditional", Assert.Throws<AbilityException>(() => Compile(conditional)).Message);
        string cost = ability.Replace("\"effect\":", "\"cost\":{\"exhaust\":\"this\"},\"effect\":", StringComparison.Ordinal);
        Assert.Contains("cost-free", Assert.Throws<AbilityException>(() => Compile(cost)).Message);
    }

    private static AbilityProgram Compile(string abilities) => AbilityLowering.Book(AbilityCatalog.Parse(
        "{\"cards\":[{\"card\":\"test\",\"abilities\":[" + abilities + "]}]}"));
}

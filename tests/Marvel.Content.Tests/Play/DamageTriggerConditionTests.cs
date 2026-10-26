using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DamageTriggerConditionTests
{
    [Theory]
    [InlineData("", DamageTriggerCondition.Dealt)]
    [InlineData(",\"damage\":\"dealt\"", DamageTriggerCondition.Dealt)]
    [InlineData(",\"damage\":\"taken\"", DamageTriggerCondition.Taken)]
    public void DamageQualifierPreservesTheAuthoredMeaning(string qualifier, DamageTriggerCondition expected)
    {
        var book = AbilityCatalog.Parse(Json("WhenDamageWouldBeDealt", "Interrupt", qualifier));
        Assert.Equal(expected, Assert.Single(book.Abilities).Trigger.Damage);
    }

    [Theory]
    [InlineData("WhenDamageWouldBeDealt", "Interrupt", "\"Taken\"")]
    [InlineData("WhenDamageWouldBeDealt", "Interrupt", "\"remaining\"")]
    [InlineData("WhenDamageWouldBeDealt", "Interrupt", "null")]
    [InlineData("WhenDamageWouldBeDealt", "Interrupt", "true")]
    [InlineData("WhenDamageWouldBeDealt", "Interrupt", "1")]
    [InlineData("WhenAttackInitiated", "Interrupt", "\"taken\"")]
    [InlineData("WhenDamageWouldBeDealt", "Response", "\"taken\"")]
    [InlineData("WhenDamageWouldBeDealt", "Response", "\"dealt\"")]
    public void InvalidDamageQualifierIsRejected(string condition, string timing, string value)
    {
        Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(Json(condition, timing, ",\"damage\":" + value)));
    }

    private static string Json(string condition, string timing, string qualifier) => """
        {"cards":[{"card":"05017","abilities":[{
          "trigger":{"event":"CONDITION","timing":"TIMING","target":"you"QUALIFIER},
          "effect":{"heal":{"card":"you","amount":1}}
        }]}]}
        """.Replace("CONDITION", condition, StringComparison.Ordinal)
            .Replace("TIMING", timing, StringComparison.Ordinal)
            .Replace("QUALIFIER", qualifier, StringComparison.Ordinal);
}

using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class TemporaryProfileLoweringTests
{
    private const string Definition = """
        {"trigger":{"timing":"Constant","subject":"this"},
         "effect":{"defineProfile":{"id":"scout","title":"Scout","kind":"minion",
           "traits":["ROBOT"],"baseValues":{"SCH":2,"ATK":3,"HP":4}}}}
        """;

    [Theory]
    [InlineData("""{"engageTopAsMinion":{"player":"you","count":1,"profile":"missing"}}""")]
    [InlineData("""{"returnOwnedToDiscard":{"defeatedWithProfile":{"profile":"missing","faceDown":"true"}}}""")]
    [InlineData("""{"chooseCard":{"from":{"last":{"defeatedWithProfile":{"profile":"missing"}}},"effect":{"ready":"chosen"}}}""")]
    public void AssignmentReferencesAreResolvedBeforeAnyGameCanRun(string effect)
    {
        string json = """{"cards":[{"card":"test","abilities":[__DEFINITION__,{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},"effect":__EFFECT__}]}]}"""
            .Replace("__DEFINITION__", Definition, StringComparison.Ordinal).Replace("__EFFECT__", effect, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
        Assert.Contains("unknown profile 'missing'", error.Message, StringComparison.Ordinal);
        Assert.Contains("'test' ability 1", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("hands")]
    [InlineData("playerDeck")]
    [InlineData("encounterDeck")]
    public void AGenericPublicAreaSelectorDoesNotGrantHiddenAreaAccess(string area)
    {
        string json = """{"cards":[{"card":"test","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},"effect":{"ready":{"last":{"inPlayerArea":{"area":"__AREA__","player":"you"}}}}}]}]}"""
            .Replace("__AREA__", area, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
        Assert.Contains("not a public player area", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateAndInvalidDefinitionsCannotBeAcceptedAsRuntimeDefaults()
    {
        string json = """{"cards":[{"card":"test","abilities":[__FIRST__,__SECOND__]}]}"""
            .Replace("__FIRST__", Definition, StringComparison.Ordinal).Replace("__SECOND__", Definition, StringComparison.Ordinal);
        Assert.Contains("duplicate profile", Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json))).Message);
        json = """{"cards":[{"card":"test","abilities":[__DEFINITION__]}]}"""
            .Replace("__DEFINITION__", Definition.Replace("\"HP\":4", "\"HP\":0", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("HP", Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json))).Message);
    }
}

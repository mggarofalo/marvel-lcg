using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class StatusReplacementLoweringTests
{
    [Theory]
    [InlineData("{\"card\":\"chosen\",\"status\":\"ready\"}")]
    [InlineData("{\"card\":\"chosen\",\"status\":{\"query\":\"statusCards\"}}")]
    [InlineData("{\"card\":{\"query\":\"statusCards\"},\"status\":\"tough\"}")]
    [InlineData("{\"card\":\"chosen\",\"status\":\"tough\",\"amount\":2}")]
    [InlineData("{\"card\":\"chosen\"}")]
    public void ReplacementRejectsUnboundedOrMalformedDestination(string argument)
    {
        var json = """
            {"cards":[{"card":"test","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
            "effect":{"replaceStatus":ARGUMENT}}]}]}
            """.Replace("ARGUMENT", argument, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
        Assert.Contains("replaceStatus", error.Message);
    }
}

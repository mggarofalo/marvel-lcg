using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EnvironmentTraitAdmissionTests
{
    [Theory]
    [InlineData("Action", "", "{\"gainEnvironmentTraits\":\"this\"}")]
    [InlineData("Constant", "\"cost\":{\"exhaust\":\"this\"},", "{\"gainEnvironmentTraits\":\"this\"}")]
    [InlineData("Constant", "\"limitPerRound\":1,", "{\"gainEnvironmentTraits\":\"this\"}")]
    [InlineData("Constant", "", "{\"seq\":[{\"gainEnvironmentTraits\":\"this\"}]}")]
    [InlineData("Constant", "", "{\"gainEnvironmentTraits\":\"villain\"}")]
    [InlineData("Constant", "", "{\"gainEnvironmentTraits\":1}")]
    public void UnsupportedEnvelopeCannotEnterAProgram(string timing, string fields, string effect)
    {
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"04076","abilities":[{
              "trigger":{"timing":"{{{{timing}}}}","subject":"this"},
              {{{{fields}}}}"effect":{{{{effect}}}}}]}]}
            """)));
    }
}

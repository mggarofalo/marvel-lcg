using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// playFromHand lowers only where its continuation closes the ability: the whole effect or a
// defeater's whole eachPlayer body. Anything after the play would need a resumption point.
public sealed class PlayFromHandLoweringTests
{
    private const string Defeated = "{\"event\":\"WhenCardDefeated\",\"timing\":\"WhenDefeated\",\"subject\":\"this\"}";

    [Theory]
    [InlineData("{\"playFromHand\":{\"reduction\":3,\"optional\":\"true\"}}")]
    [InlineData("{\"playFromHand\":{\"reduction\":1}}")]
    [InlineData("{\"eachPlayer\":{\"player\":\"defeater\",\"effect\":{\"playFromHand\":{\"reduction\":3,\"optional\":\"true\"}}}}")]
    public void TheWholeFrameLowers(string effect) =>
        Assert.NotNull(new AbilityRunner(AbilityCatalog.Parse(Book(effect))));

    [Theory]
    [InlineData("{\"playFromHand\":{\"reduction\":0}}")]
    [InlineData("{\"playFromHand\":{\"optional\":\"true\"}}")]
    [InlineData("{\"playFromHand\":{\"reduction\":3,\"optional\":\"maybe\"}}")]
    [InlineData("{\"playFromHand\":{\"reduction\":3,\"player\":\"you\"}}")]
    [InlineData("{\"seq\":[{\"playFromHand\":{\"reduction\":3}},{\"draw\":{\"player\":\"you\",\"count\":1}}]}")]
    [InlineData("{\"seq\":[{\"draw\":{\"player\":\"you\",\"count\":1}},{\"playFromHand\":{\"reduction\":3}}]}")]
    [InlineData("{\"eachPlayer\":{\"effect\":{\"playFromHand\":{\"reduction\":3}}}}")]
    public void OtherShapesFailLowering(string effect) =>
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(Book(effect))));

    private static string Book(string effect) =>
        "{\"cards\":[{\"card\":\"16130\",\"abilities\":[{\"trigger\":" + Defeated + ",\"effect\":" + effect + "}]}]}";
}

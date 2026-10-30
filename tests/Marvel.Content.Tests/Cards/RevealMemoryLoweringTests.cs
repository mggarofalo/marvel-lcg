using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// differentTitleFromRevealed reads what its own ability revealed, so the engine
// admits it only in an effect that reveals a card.
public sealed class RevealMemoryLoweringTests
{
    private const string Search =
        """{"cardsIn":{"area":"encounterDeck","kind":"Obligation","differentTitleFromRevealed":"true"}}""";

    [Fact]
    public void AnEffectThatRevealsMayReadItsReveals()
    {
        var book = AbilityLowering.Book(AbilityCatalog.Parse(Card(
            Choose("reveal"))));
        Assert.NotNull(book);
    }

    [Fact]
    public void AnEffectThatRevealsNothingIsRejected()
    {
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(Card(
            Choose("discard")))));
        Assert.Contains("differentTitleFromRevealed requires an effect that reveals a card", error.Message);
    }

    [Fact]
    public void APlayConditionIsRejected()
    {
        var error = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(Card(
            Choose("reveal"),
            ",\"when\":{\"exists\":" + Search + "}"))));
        Assert.Contains("at 'when': differentTitleFromRevealed is read only by an effect", error.Message);
    }

    private static string Choose(string instruction) =>
        "{\"chooseCard\":{\"from\":" + Search + ",\"effect\":{\"" + instruction + "\":\"chosen\"}}}";

    private static string Card(string effect, string extra = "") =>
        "{\"cards\":[{\"card\":\"test\",\"abilities\":[{"
        + "\"trigger\":{\"event\":\"WhenCardRevealed\",\"timing\":\"WhenRevealed\",\"subject\":\"this\"},"
        + "\"effect\":" + effect + extra + "}]}]}";
}

using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ChoiceDamageDescriptionTests
{
    [Fact]
    public void CurrentAttackAmountIncludesModifiersBeforeTargetPrevention()
    {
        // Synthetic checked event isolates quantity description from a card's cost.
        var runner = Runner("01005", "Action", """
            {"chooseCard":{"from":{"query":"villain"},"effect":
                {"attack":{"target":"chosen","effect":
                    {"dealAttackDamage":{"cards":"chosen","amount":2}}}}}}
            """);
        Card? source = null;
        var (_, world) = Playing(board =>
            source = board.CreateCard("01005", board.Seats[0].Hand), hero: true, abilities: runner);
        Card villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attackDamage",
            Amount: 2, Card: source!.ObjectId, Affects: source.ObjectId, Lasts: new Duration(Uses: 1)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "preventDamage",
            Amount: 3, Affects: villain.ObjectId, Lasts: new Duration(Uses: 1)));
        string before = world.Digest().Fingerprint();
        Prompt prompt = Assert.IsType<Prompt>(runner.Choosing(world, source, 0, 0, null, false));
        string description = Assert.IsType<string>(Assert.Single(prompt.Affordances).Description);
        long hp = Damage.Health(world, world.Facts, villain);

        Assert.Contains("Current attack damage: 4 before prevention and replacement", description);
        Assert.Contains("prevents 3 damage", description);
        Assert.Contains($"{hp}/{hp} → {hp - 1}/{hp} HP", description);
        Assert.DoesNotContain("Final step", description);
        Assert.Equal(before, world.Digest().Fingerprint());
    }

    [Theory]
    [InlineData(20)]
    [InlineData(21)]
    public void ACappedPreviewDoesNotAuthorizeRevealingAnUncappedConcealedCount(int count)
    {
        // Synthetic checked instruction tests an unsupported numeric disclosure.
        // These hidden copies are not a playable deck or a Core card ability.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"chooseCard":{"from":{"query":"villain"},"effect":
                {"dealDamage":{"cards":"chosen","amount":
                    {"count":{"cardsIn":{"area":"yourDeck","kind":"Upgrade"}}}}}}}
            """);
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            for (int index = 0; index < count; index++) board.CreateCard("01047", board.Seats[0].Deck);
        }, hero: true, abilities: runner);
        string before = world.Digest().Fingerprint();
        Prompt prompt = Assert.IsType<Prompt>(runner.Choosing(world, source!, 0, 0, null, false));
        string description = Assert.IsType<string>(Assert.Single(prompt.Affordances).Description);

        Assert.DoesNotContain("Current damage", description);
        Assert.DoesNotContain($": {count}", description);
        Assert.Contains("HP", description);
        Assert.Equal(before, world.Digest().Fingerprint());
    }
    [Theory]
    [InlineData("direct")]
    [InlineData("outer-final")]
    [InlineData("inner-final")]
    public void HiddenBranchConditionsCannotDiscloseUncappedDamage(string nesting)
    {
        // Synthetic checked branches differ only in a concealed card's kind.
        // Both amounts have the same capped outcome against this villain.
        string damage20 = """{"dealDamage":{"cards":"chosen","amount":20}}""";
        string damage21 = """{"dealDamage":{"cards":"chosen","amount":21}}""";
        string then = nesting == "inner-final"
            ? $$$$$$"""{"if":{"test":{"finalStep":"true"},"then":{{{{{{damage20}}}}}},"else":{{{{{{damage21}}}}}}}}"""
            : damage20;
        string effect = $$$$$$"""{"if":{"test":{"exists":{"withTrait":{"cards":{"cardsIn":{"area":"yourDeck","kind":"Upgrade"}},"trait":"BLACK_PANTHER"}}},"then":{{{{{{then}}}}}},"else":{{{{{{damage21}}}}}}}}""";
        if (nesting == "outer-final")
            effect = $$$$$$"""{"if":{"test":{"finalStep":"true"},"then":{{{{{{effect}}}}}},"else":{{{{{{damage20}}}}}}}}""";
        var descriptions = new List<string>();
        foreach (string hiddenFace in new[] { "01047", "01088" })
        {
            var runner = Runner(AuthoredCards.AuntMay, "Action",
                $$$$$$"""{"chooseCard":{"from":{"query":"villain"},"effect":{{{{{{effect}}}}}}}}""");
            Card? source = null;
            var (_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
            world.CreateCard(hiddenFace, world.Seats[0].Deck);
            Assert.Equal(hiddenFace == "01047", world.Seats[0].Deck.Cards.Any(card => card.FaceId == "01047"));
            string before = world.Digest().Fingerprint();
            Prompt prompt = Assert.IsType<Prompt>(runner.Choosing(world, source!, 0, 0, null, true));
            string description = Assert.IsType<string>(Assert.Single(prompt.Affordances).Description);
            Assert.DoesNotContain("Current damage", description);
            Assert.Contains("HP", description);
            Assert.Equal(before, world.Digest().Fingerprint());
            descriptions.Add(description);
        }
        Assert.Equal(descriptions[0], descriptions[1]);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 4)]
    public void PublicFinalStepEffectBranchesRetainTheirCurrentAmount(bool finalStep, int amount)
    {
        // Synthetic effect-level branch exercises the same public flag as numeric branches.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"chooseCard":{"from":{"query":"villain"},"effect":
                {"if":{"test":{"finalStep":"true"},
                    "then":{"dealDamage":{"cards":"chosen","amount":4}},
                    "else":{"dealDamage":{"cards":"chosen","amount":2}}}}}}
            """);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Prompt prompt = Assert.IsType<Prompt>(runner.Choosing(world, source!, 0, 0, null, finalStep));
        string description = Assert.IsType<string>(Assert.Single(prompt.Affordances).Description);
        Assert.Contains($"Current damage: {amount} before prevention and replacement", description);
        Assert.Equal(finalStep, description.Contains("Final step", StringComparison.Ordinal));
    }

}

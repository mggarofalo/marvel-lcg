using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests;

public sealed class PrintedStatFactsTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("01084", "ATK", "2", false, 1)]
    [InlineData("01084", "THW", "2", false, 1)]
    [InlineData("01002", "ATK", "1", false, 0)]
    [InlineData("01002", "THW", "1", false, 1)]
    [InlineData("03014", "ATK", "3", true, 1)]
    [InlineData("27047", "ATK", "3", false, 2)]
    [InlineData("27047", "THW", "3", false, 3)]
    [InlineData("41001a", "ATK", "1", true, 0)]
    [InlineData("41001a", "THW", "1", true, 0)]
    [InlineData("41001a", "DEF", "2", true, 0)]
    [InlineData("41001a", "HS", "4", false, 0)]
    [InlineData("01029a", "HS", "1", false, 0)]
    [InlineData("01099", "ATK+", "3", true, 0)]
    [InlineData("01153", "ATK+", "1", false, 0)]
    [Rule("rr:star-icon")]
    [Rule("rr:consequential-damage")]
    public void PrintedMarkersAreIndependent(
        string face, string attribute, string value, bool specialStar, int consequential)
    {
        // A star "is merely a reminder to check the card's text box". An ally
        // takes damage "equal to the number of consequential damage icons".
        // Research-only expansion faces exercise metadata, not playable content.
        PrintedStatValue stat = Cards.PrintedStats(face)[attribute];
        Assert.Equal(new PrintedStatValue(value, specialStar, false, consequential), stat);
    }

    [Fact]
    [Rule("rr:dash-value")]
    [Rule("rr:non-numerical-variable")]
    public void AbsentDashVariableAndZeroAreDifferentPrintedFacts()
    {
        // "A value presented as a dash (–) indicates that value cannot be used."
        // A variable is treated "as the defined value" when its ability defines it.
        Assert.Equal("—", Cards.PrintedStats("01050")["THW"].Value);
        Assert.False(Cards.Attributes("01050").ContainsKey("THW"));
        Assert.Equal("X", Cards.PrintedStats("01162")["ATK"].Value);
        Assert.Equal("0", Cards.Attributes("01162")["ATK"]);
        Assert.Equal("0", Cards.PrintedStats("01003")["Cost"].Value);
        Assert.False(Cards.PrintedStats("01029a").ContainsKey("REC"));
        Assert.Equal("★", Cards.PrintedStats("40130")["ATK"].Value);
        Assert.True(Cards.PrintedStats("40130")["ATK"].SpecialStar);
    }

    [Theory]
    [InlineData("01094", "HP", "14")]
    [InlineData("01097b", "TargetThreat", "7")]
    public void PerPlayerValuesKeepThePrintedNumber(string face, string attribute, string value)
    {
        PrintedStatValue stat = Cards.PrintedStats(face)[attribute];
        Assert.Equal(new PrintedStatValue(value, false, true, 0), stat);
        Assert.Equal(2 * long.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            Cards.PrintedValue(face, attribute, players: 2));
    }

    [Fact]
    public void ASpecialCostStarIsNotAPerPlayerIcon()
    {
        // The structured cost_star source field records a special ability.
        Assert.Equal(new PrintedStatValue("1", true, false, 0),
            Cards.PrintedStats("60023")["Cost"]);
    }

    [Theory]
    [InlineData("01121", "0")]
    [InlineData("01146", "1")]
    [Rule("rr:boost-boost-icon.1")]
    public void ABoostStarIsIndependentFromTheBoostIconCount(string face, string icons)
    {
        // "A star icon is not itself considered a boost icon, and does not
        // contribute to the villain’s ATK or SCH value."
        Assert.Equal(new PrintedStatValue(icons, true, false, 0), Cards.PrintedStats(face)["Boost"]);
    }

    [Theory]
    [InlineData("02001a", "ATK", "★")]
    [InlineData("02020", "ATK+", "★")]
    [InlineData("50119", "ATK+", "X")]
    public void AnExplicitSourceStarCanIdentifyAFieldWithoutANumericAttribute(
        string face, string attribute, string value)
    {
        Assert.Equal(new PrintedStatValue(value, true, false, 0), Cards.PrintedStats(face)[attribute]);
    }

    [Fact]
    public void ASourceVerifiedHandSizeStarIsSupportedWithoutReadingRulesText()
    {
        // Synthetic source fixture: no verified Core identity prints an HS star.
        CardCatalog catalog = CardCatalog.Parse("""
            {"cards":[{"card_id":"fixture","type":"Hero","attributes":{"HS":"4"},
            "stat_annotations":{"HS":{"special_star":true}},"text_plain":"No marker here."}]}
            """);
        Assert.Equal(new PrintedStatValue("4", true, false, 0), catalog.PrintedStats("fixture")["HS"]);
        Assert.Equal("4", catalog.Attributes("fixture")["HS"]);
    }

    [Fact]
    public void ASpecialReminderCanCoexistWithPlayerScaling()
    {
        CardCatalog catalog = CardCatalog.Parse("""
            {"cards":[{"card_id":"fixture","type":"Villain","attributes":{"HP":"14*"},
            "stat_annotations":{"HP":{"special_star":true}}}]}
            """);
        Assert.Equal(new PrintedStatValue("14", true, true, 0), catalog.PrintedStats("fixture")["HP"]);
    }

    [Fact]
    public void DisplayFieldsRetainAllStatAliasesWithoutTreatingResourcesOrUniquenessAsStats()
    {
        string[] names = ["REC", "THW", "ATK", "DEF", "SCH", "HP", "HS", "Stage", "Cost",
            "REC+", "THW+", "ATK+", "DEF+", "SCH+", "HP+", "StartingThreat", "TargetThreat",
            "EscalationThreat", "Boost", "Acceleration", "Amplify", "Crisis", "Hazard"];
        var attributes = names.ToDictionary(name => name, _ => "0", StringComparer.Ordinal);
        attributes["RES"] = "R";
        attributes["Unique"] = "1";
        attributes["Class"] = "Basic";
        Assert.Equal(names.Order(), PrintedStatFacts.From(CardKind.Event, attributes).Keys.Order());
    }

    [Fact]
    public void AnAnnotationCannotCreateAnEmptyPrintedValue()
    {
        Assert.Throws<FormatException>(() => CardCatalog.Parse("""
            {"cards":[{"card_id":"invalid","attributes":{},
            "stat_annotations":{"HS":{"special_star":true}}}]}
            """));
    }
}

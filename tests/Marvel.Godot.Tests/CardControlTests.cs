using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardControlTests
{
    [Fact]
    public void HandSummaryRetainsCostAndResourceWithoutBoardState()
    {
        BoardCardPresentation card = Card("EVENT", fields:
            [new BoardFieldPresentation("DAMAGE", "3")]) with
        {
            Cost = "2",
            PrintedStats = [new BoardFieldPresentation("RES", "Energy")],
        };

        IReadOnlyList<BoardFieldPresentation> values =
            CardControl.CompactValues(card, CardDisplaySize.Hand);

        Assert.Equal(["COST", "RES"], values.Select(value => value.Name));
        Assert.Equal("Energy", CardRulesMarkup.ResourceNames(Assert.Single(
            values, value => value.Name == "RES").Value));
        Assert.Equal(["E"], CardRulesMarkup.ResourceGlyphs(Assert.Single(
            values, value => value.Name == "RES").Value));
        Assert.Equal(
            [("Energy", "E")],
            CardRulesMarkup.ResourceTokens(Assert.Single(
                values, value => value.Name == "RES").Value));
        Assert.True(CardControl.CompactHeight(
            card,
            VisualSystem.Card(CardDisplaySize.Hand, InterfaceScale.Standard),
            CardDisplaySize.Hand) >= 108);
        Assert.Null(CardControl.CompactState(card, CardDisplaySize.Hand));
    }

    [Fact]
    public void SchemeSummaryCombinesThreatAndKeepsEscalationWithoutStage()
    {
        BoardCardPresentation card = Card(
            "MAIN SCHEME",
            fields:
            [
                new BoardFieldPresentation("THREAT", "4"),
                new BoardFieldPresentation("TARGET_THREAT", "7"),
                new BoardFieldPresentation("PRINTED_STAGE", "1"),
            ]) with
        {
            PrintedStats =
            [
                new BoardFieldPresentation("Stage", "1"),
                new BoardFieldPresentation("TargetThreat", "7*"),
                new BoardFieldPresentation("EscalationThreat", "1"),
            ],
            Counters = [new BoardFieldPresentation("ACCELERATION", "1")],
        };

        IReadOnlyList<BoardFieldPresentation> values =
            CardControl.CompactValues(card, CardDisplaySize.Board);

        Assert.Contains(values, value => value.Name == "THREAT" && value.Value == "4/7");
        Assert.Contains(values, value => value.Name == "ESCALATION_THREAT" && value.Value == "1");
        Assert.Contains(values, value => value.Name == "ACCELERATION" && value.Value == "1");
        Assert.DoesNotContain(values, value => value.Name.Contains("Stage", StringComparison.OrdinalIgnoreCase));
        Assert.Single(values, value => value.Name == "THREAT");
    }

    [Fact]
    public void CompactSummaryDropsUnmatchedDiagnosticFields()
    {
        BoardCardPresentation card = Card(
            "ALLY",
            fields:
            [
                new BoardFieldPresentation("THW", "1"),
                new BoardFieldPresentation("ATTACK", "2"),
                new BoardFieldPresentation("THWART", "1"),
                new BoardFieldPresentation("HEALTH", "3/3"),
                new BoardFieldPresentation("ALLY_LIMIT", "3"),
                new BoardFieldPresentation("HAND_SIZE", "5"),
                new BoardFieldPresentation("FIRST_PLAYER_TOKEN", "1"),
                new BoardFieldPresentation("RESTRICTED_LIMIT", "2"),
            ]);

        IReadOnlyList<BoardFieldPresentation> values =
            CardControl.CompactValues(card, CardDisplaySize.Board);

        Assert.Equal(["THW", "ATK", "HEALTH"], values.Select(value => value.Name));
    }

    [Theory]
    [InlineData("HERO", "THWART", "THW")]
    [InlineData("MINION", "SCHEME", "SCH")]
    public void CharacterSummaryPrefersEffectiveStatsAndCurrentMaximumHealth(
        string kind,
        string projectedStat,
        string compactStat)
    {
        BoardCardPresentation card = Card(
            kind,
            fields:
            [
                new BoardFieldPresentation(projectedStat, "3"),
                new BoardFieldPresentation("HEALTH", "6/9"),
            ]) with
        {
            PrintedStats =
            [
                new BoardFieldPresentation(compactStat, "2"),
                new BoardFieldPresentation("HP", "9"),
            ],
        };

        IReadOnlyList<BoardFieldPresentation> values =
            CardControl.CompactValues(card, CardDisplaySize.Board);

        Assert.Contains(values, value => value.Name == compactStat && value.Value == "3");
        Assert.Contains(values, value => value.Name == "HEALTH" && value.Value == "6/9");
        Assert.Single(values, value => value.Name == compactStat);
        Assert.Single(values, value => value.Name == "HEALTH");
        Assert.True(CardControl.IsCompactProgressValue(
            Assert.Single(values, value => value.Name == "HEALTH")));
    }

    [Theory]
    [InlineData("1", "1", "2", "14/14")]
    [InlineData("2", "1", "3", "15/15")]
    public void SameTitleCoreVillainStagesRetainOneStageBesideLiveStats(
        string stage,
        string scheme,
        string attack,
        string health)
    {
        BoardCardPresentation rhino = Card(
            "ENCOUNTER VILLAIN",
            fields:
            [
                new BoardFieldPresentation("SCHEME", scheme),
                new BoardFieldPresentation("ATTACK", attack),
                new BoardFieldPresentation("HEALTH", health),
            ]) with
        {
            Title = "Rhino",
            PrintedStats =
            [
                new BoardFieldPresentation("Stage", stage),
                new BoardFieldPresentation("Boost", "2"),
            ],
        };

        IReadOnlyList<BoardFieldPresentation> values =
            CardControl.CompactValues(rhino, CardDisplaySize.Board);

        Assert.Equal(["Stage", "SCH", "ATK", "HEALTH"],
            values.Select(value => value.Name));
        Assert.Equal(stage, Assert.Single(values, value => value.Name == "Stage").Value);
        Assert.DoesNotContain(values, value => value.Name == "Boost");
    }

    [Fact]
    public void FutureEnemyStageDoesNotCompeteWithTheActiveEnemy()
    {
        BoardCardPresentation card = Card("ENCOUNTER VILLAIN") with
        {
            PrintedStats =
            [
                new BoardFieldPresentation("Stage", "2"),
                new BoardFieldPresentation("Boost", "2"),
                new BoardFieldPresentation("SCH", "2"),
                new BoardFieldPresentation("ATK", "3"),
                new BoardFieldPresentation("HP", "15"),
            ],
        };

        IReadOnlyList<BoardFieldPresentation> values =
            CardControl.CompactValues(card, CardDisplaySize.Board);

        Assert.Equal(["Stage", "Boost"], values.Select(value => value.Name));
        Assert.DoesNotContain(values, value => value.Name is "HP" or "HEALTH");
    }

    [Fact]
    public void SideSchemeNeedsLiveThreatBeforeItShowsProgress()
    {
        BoardCardPresentation stored = Card("ENCOUNTER SIDE SCHEME") with
        {
            PrintedStats =
            [
                new BoardFieldPresentation("StartingThreat", "3"),
                new BoardFieldPresentation("Boost", "1"),
            ],
        };
        BoardCardPresentation active = stored with
        {
            Fields =
            [
                new BoardFieldPresentation("THREAT", "3"),
                new BoardFieldPresentation("CRISIS", "1"),
                new BoardFieldPresentation("HAZARD", "0"),
            ],
        };

        IReadOnlyList<BoardFieldPresentation> storedValues =
            CardControl.CompactValues(stored, CardDisplaySize.Board);
        IReadOnlyList<BoardFieldPresentation> activeValues =
            CardControl.CompactValues(active, CardDisplaySize.Board);

        Assert.Equal(["Boost"], storedValues.Select(value => value.Name));
        Assert.Contains(activeValues, value => value.Name == "THREAT" && value.Value == "3");
        Assert.Contains(activeValues, value => value.Name == "CRISIS" && value.Value == "1");
        Assert.DoesNotContain(activeValues, value => value.Name == "HAZARD");
        Assert.DoesNotContain(activeValues, value => value.Name == "Stage");
    }

    [Fact]
    public void HandHeightGrowsForAWrappedUntruncatedTitle()
    {
        CardLayoutMetrics layout = VisualSystem.Card(
            CardDisplaySize.Hand, InterfaceScale.Standard);
        BoardCardPresentation shortTitle = Card("EVENT");
        BoardCardPresentation longTitle = shortTitle with
        {
            Title = "A Very Long Opening Hand Card Title That Must Wrap",
        };

        Assert.True(
            CardControl.CompactHeight(longTitle, layout, CardDisplaySize.Hand)
            > CardControl.CompactHeight(shortTitle, layout, CardDisplaySize.Hand));
    }

    [Fact]
    public void CompactStateSuppressesReadyButRetainsExhaustionAndOtherStatuses()
    {
        BoardCardPresentation ready = Card("ALLY") with { Status = "READY" };
        BoardCardPresentation exhausted = ready with { Status = "EXHAUSTED" };
        BoardCardPresentation stunned = ready with { Status = "STUNNED" };

        Assert.Null(CardControl.CompactState(ready, CardDisplaySize.Board));
        Assert.Equal("EXHAUSTED", CardControl.CompactState(exhausted, CardDisplaySize.Board));
        Assert.Equal("STUNNED", CardControl.CompactState(stunned, CardDisplaySize.Board));
    }

    [Fact]
    public void ConcealedCardsDoNotProduceReadableSummaryFacts()
    {
        BoardCardPresentation card = Card(
            "MINION",
            fields: [new BoardFieldPresentation("HEALTH", "2/2")]) with
        {
            Concealed = true,
            Cost = "1",
            PrintedStats = [new BoardFieldPresentation("ATK", "2")],
        };

        Assert.Empty(CardControl.CompactValues(card, CardDisplaySize.Board));
    }

    private static BoardCardPresentation Card(
        string kind,
        IReadOnlyList<BoardFieldPresentation>? fields = null) => new(
            TargetId: 1,
            Count: 1,
            Concealed: false,
            Title: "Test card",
            Subtitle: string.Empty,
            Kind: kind,
            Status: string.Empty,
            Fields: fields ?? []);
}

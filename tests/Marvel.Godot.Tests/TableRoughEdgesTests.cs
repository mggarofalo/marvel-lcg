using Marvel.Client;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

// The table states facts and stays quiet about the client's own plumbing.
public sealed class TableRoughEdgesTests
{
    [Theory]
    [InlineData(GameProgressKind.AwaitingDecision, false)]
    [InlineData(GameProgressKind.WaitingForOtherPlayer, false)]
    [InlineData(GameProgressKind.Resolving, false)]
    [InlineData(GameProgressKind.Synchronizing, false)]
    [InlineData(GameProgressKind.Recovered, false)]
    [InlineData(GameProgressKind.PlayersWin, false)]
    [InlineData(GameProgressKind.SynchronizationUnavailable, true)]
    [InlineData(GameProgressKind.DecisionRejected, true)]
    [InlineData(GameProgressKind.DecisionNotSent, true)]
    [InlineData(GameProgressKind.Unconfirmed, true)]
    [InlineData(GameProgressKind.ServiceUnavailable, true)]
    [InlineData(GameProgressKind.StorageFailure, true)]
    public void TheSyncMarkShowsOnlyWhileTheTableMayBeStale(GameProgressKind kind, bool stale) =>
        Assert.Equal(stale, SyncIndicator.Stale(kind));

    [Fact]
    public void TheSyncMarkExplainsItselfOnHover()
    {
        Assert.Equal("Table not synchronized.\nThe server is busy.",
            SyncIndicator.Tooltip("Table not synchronized.", "The server is busy."));
        Assert.Equal("Decision rejected.", SyncIndicator.Tooltip("Decision rejected.", ""));
    }

    [Theory]
    [InlineData(2, 3, "2/3")]
    [InlineData(5, 3, "3/3")]
    [InlineData(-1, 2, "0/2")]
    [InlineData(1, 0, "")]
    public void ATrackerShowsASmallNumberBesideItsPips(int filled, int total, string number) =>
        Assert.Equal(number, Tracker.Number(filled, total));

    [Fact]
    public void AnAttackIsChipsOfWhoWhomItsValueAndAnUnresolvedBoost()
    {
        Wording[] facts = TableWording.AttackFacts(new DecisionAttack("Rhino", "Spider-Man", 2, false, true));
        Assert.Equal(["Rhino → Spider-Man", "ATK 2", "Boost"], facts.Select(fact => fact.Label));
        Assert.Equal("Rhino is attacking Spider-Man.", facts[0].Tooltip);
        Assert.Equal(["attack", "", "boost"], facts.Select(fact => fact.Icon));
        Assert.Equal(["Rhino → Spider-Man", "Damage 3"],
            TableWording.AttackFacts(new DecisionAttack("Rhino", "Spider-Man", 3, true, false)).Select(fact => fact.Label));
    }

    [Fact]
    public void TheSheetAndThePromptLineCarryTheAttackAfterTheCauses()
    {
        var frame = new DecisionFrame("Spider-Man", DecisionSubject.Resolving, "Rhino", true, ["Assault"],
            "Rhino is attacking Spider-Man. ATK 2 before boost icons and defense.")
        {
            Attack = new DecisionAttack("Rhino", "Spider-Man", 2, false, true),
        };
        Assert.Equal(["Spider-Man", "Optional", "Assault", "Rhino → Spider-Man", "ATK 2", "Boost"],
            DecisionSheetHead.Facts(frame, "Leave attack undefended").Select(fact => fact.Label));
    }

    [Fact]
    public void TheQuestionsTooltipHoldsTheWholeDecision()
    {
        var frame = new DecisionFrame("She-Hulk", DecisionSubject.Resolving, "Kang (Iron Lad)", true, [],
            "Enemy attack · Kang (Iron Lad) is attacking She-Hulk.");
        Assert.Equal("Decision for She-Hulk · Resolving Kang (Iron Lad)\nYou may leave attack undefended.\n"
            + "Enemy attack · Kang (Iron Lad) is attacking She-Hulk.",
            TableWording.DecisionDetail(frame, "Leave attack undefended"));
    }

    [Theory]
    [InlineData(InterfaceScale.Percent100, 962, InterfaceScale.Percent100)]
    [InlineData(InterfaceScale.Percent100, 1000, InterfaceScale.Percent100)]
    [InlineData(InterfaceScale.Percent100, 1350, InterfaceScale.Percent140)]
    [InlineData(InterfaceScale.Percent150, 1350, InterfaceScale.Percent140)]
    [InlineData(InterfaceScale.Percent80, 1350, InterfaceScale.Percent110)]
    [InlineData(InterfaceScale.Percent100, 2000, InterfaceScale.Percent150)]
    [InlineData(InterfaceScale.Percent150, 860, InterfaceScale.Percent90)]
    public void ATallTableGrowsItsCards(InterfaceScale reading, float height, InterfaceScale cards) =>
        Assert.Equal(cards, SpatialCardMetrics.TableScale(reading, height));

    [Fact]
    public void GameAreasTakeTheEngagedColumnsWhenNoEnemyIsEngaged()
    {
        var engaged = new AstraTableGeometry(1864, 990, false) { HasGameAreas = true };
        var free = engaged with { HasEngagedEnemies = false };
        TableGrid grid = TableGrid.For(1864, InterfaceScale.Percent100);
        Assert.Equal(grid.Span(2, 8), (engaged.GameAreaBand.Position.X, engaged.GameAreaBand.Size.X));
        Assert.Equal(grid.Span(2, 10), (free.GameAreaBand.Position.X, free.GameAreaBand.Size.X));
    }

    [Fact]
    public void ASeparatedTableShowsItsTallerFarSideInProportion()
    {
        var reference = new AstraTableGeometry(1864, AstraTableGeometry.ReferenceHeight, false) { HasGameAreas = true };
        var tall = reference with { Height = 1350 };
        Assert.Equal(344, reference.VillainMat.Size.Y);
        Assert.Equal(344 * 1350 / AstraTableGeometry.ReferenceHeight, tall.VillainMat.Size.Y, 3);
    }

    [Fact]
    public void AnOrdinaryTableHasNoProgressDescription()
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "turn", "Turn", true, []);
        var world = new WorldDescriptor([], [], [], Marvel.Rules.Play.Outcome.Unfinished);
        var response = new Marvel.Protocol.EngineResponse(Marvel.Protocol.EngineProtocol.Version, "r", "g", null, prompt,
            [], world);
        Assert.Equal("", GameProgressPresentation.FromResponse(response).Description);
        Assert.Equal("", GameProgressPresentation.FromResponse(response with { Prompt = null }).Description);
    }
}

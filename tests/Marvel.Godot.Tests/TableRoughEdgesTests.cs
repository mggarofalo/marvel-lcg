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
        Wording[] facts = DecisionWording.AttackFacts(new DecisionAttack("Rhino", "Spider-Man", 2, false, true, false));
        Assert.Equal(["Rhino → Spider-Man", "ATK 2", "Boost"], facts.Select(fact => fact.Label));
        Assert.Equal("Rhino is attacking Spider-Man.", facts[0].Tooltip);
        Assert.Equal(["attack", "", "boost"], facts.Select(fact => fact.Icon));
        Assert.Equal(["Rhino → Spider-Man", "Damage 3"],
            DecisionWording.AttackFacts(new DecisionAttack("Rhino", "Spider-Man", 3, true, false, false)).Select(fact => fact.Label));
    }

    [Fact]
    public void APreventedAttackShowsPreventedInsteadOfItsValue()
    {
        Wording[] facts = DecisionWording.AttackFacts(new DecisionAttack("Rhino", "Spider-Man", 3, false, true, true));
        Assert.Equal(["Rhino → Spider-Man", "Prevented", "Boost"], facts.Select(fact => fact.Label));
        Assert.Equal("All damage from this attack is prevented; boost effects still resolve.", facts[1].Tooltip);
        Assert.Equal(["Rhino → Spider-Man", "Prevented"],
            DecisionWording.AttackFacts(new DecisionAttack("Rhino", "Spider-Man", 3, true, false, true)).Select(fact => fact.Label));
    }

    [Theory]
    [InlineData(true, "The attacking enemy's ATK now, before boost and defense.")]
    [InlineData(false, "The attacking enemy's ATK now, before defense and later effects.")]
    public void TheATKTooltipNamesBoostOnlyWhileItIsUnresolved(bool boostUnresolved, string tooltip) =>
        Assert.Equal(tooltip,
            DecisionWording.AttackFacts(new DecisionAttack("Rhino", "Spider-Man", 2, false, boostUnresolved, false))[1].Tooltip);

    [Fact]
    public void TheSheetAndThePromptLineCarryTheAttackAfterTheCauses()
    {
        var frame = new DecisionFrame("Spider-Man", DecisionSubject.Resolving, "Rhino", true, ["Assault"],
            "Rhino is attacking Spider-Man. ATK 2 before boost icons and defense.")
        {
            Attack = new DecisionAttack("Rhino", "Spider-Man", 2, false, true, false),
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
            DecisionWording.DecisionDetail(frame, "Leave attack undefended"));
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
        Assert.Equal(344 * 1.4f, tall.VillainMat.Size.Y, 3);
        Assert.Equal(344, (reference with { Height = 1000 }).VillainMat.Size.Y);
    }

    [Theory]
    [InlineData(700.0f, 820.0f)]
    [InlineData(900.6f, 900.0f)]
    [InlineData(1000.0f, 962.0f)]
    [InlineData(1057.0f, 962.0f)]
    [InlineData(1059.0f, 1059.0f)]
    [InlineData(1330.0f, 1330.0f)]
    public void TheTableTakesTheWholeRoomOnlyFromOneGrowthStep(float room, float height) =>
        Assert.Equal(height, TableCanvasBounds.Fit(room));

    [Theory]
    [InlineData(100, 900, 962, true)]
    [InlineData(100, 962, 1000, true)]
    [InlineData(100, 1000, 1040, true)]
    [InlineData(100, 1000, 1100, false)]
    [InlineData(100, 1350, 1360, true)]
    [InlineData(100, 820, 962, false)]
    [InlineData(90, 1160, 1170, true)]
    [InlineData(90, 1160, 1200, false)]
    [InlineData(150, 1000, 1100, false)]
    public void ATableStretchesInPlaceOnlyWithinOneGrowthStep(int reading, float rendered, float height, bool reflowable) =>
        Assert.Equal(reflowable, TableCanvasBounds.Reflowable((InterfaceScale)reading, rendered, height));

    [Fact]
    public void AStretchAtNinetyPercentKeepsTheCardsItWasDrawnWith()
    {
        Assert.NotEqual(
            SpatialCardMetrics.TableScale(InterfaceScale.Percent90, 1160),
            SpatialCardMetrics.TableScale(InterfaceScale.Percent90, 1200));
        Assert.Equal(
            SpatialCardMetrics.TableScale(InterfaceScale.Percent100, 1160),
            SpatialCardMetrics.TableScale(InterfaceScale.Percent100, 1200));
        Assert.False(TableCanvasBounds.Reflowable(InterfaceScale.Percent90, 1160, 1200));
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

    [Fact]
    public void AConsequenceWithoutTypedFactsStaysOneLineAndTheAttackReplacesIt()
    {
        var frame = new DecisionFrame("Spider-Man", DecisionSubject.Resolving, "Beat Cop", false, [],
            "Choose an enemy.\nCurrent damage: 3 before prevention/replacement.");
        Assert.Equal("Choose an enemy. · Current damage: 3 before prevention/replacement.",
            DecisionWording.UnstructuredConsequence(frame, PublicDecisionKind.Choice));
        Assert.Equal("", DecisionWording.UnstructuredConsequence(frame with
        {
            Attack = new DecisionAttack("Rhino", "Spider-Man", 2, false, true, false),
        }, PublicDecisionKind.Choice));
    }

    [Theory]
    [InlineData(PublicDecisionKind.Choice, false)]
    [InlineData(PublicDecisionKind.OpeningHand, true)]
    [InlineData(PublicDecisionKind.EndPhaseDiscards, true)]
    public void ATypedEffectOrAStagingKindReplacesTheConsequenceLine(PublicDecisionKind kind, bool staging)
    {
        var frame = new DecisionFrame("Spider-Man", DecisionSubject.Resolving, "Swinging Web Kick", false, [],
            "Choose an enemy to attack. Printed damage: 8; modifiers, prevention and later effects can change the result.");
        Assert.Equal(staging, DecisionWording.UnstructuredConsequence(frame, kind).Length == 0);
        Assert.Equal("", DecisionWording.UnstructuredConsequence(frame with
        {
            Effect = new PromptEffect(PromptEffectKind.Damage, 8),
        }, kind));
    }

    [Theory]
    [InlineData(PromptEffectKind.Damage, 8, "Printed damage 8", "Icon_damage")]
    [InlineData(PromptEffectKind.RemoveThreat, 3, "Printed threat removal 3", "Icon_remove_threat")]
    [InlineData(PromptEffectKind.Draw, 1, "Printed draw 1", "Icon_draw")]
    [InlineData(PromptEffectKind.Stun, 0, "Printed stun", "Icon_stun")]
    [InlineData(PromptEffectKind.Confuse, 0, "Printed confuse", "Icon_confuse")]
    [InlineData(PromptEffectKind.Tough, 0, "Printed tough", "Icon_tough")]
    [InlineData(PromptEffectKind.Heal, 2, "Printed heal 2", "Icon_heal")]
    [InlineData(PromptEffectKind.Ready, 0, "Printed ready", "Icon_ready")]
    [InlineData(PromptEffectKind.Exhaust, 0, "Printed exhaust", "Icon_exhaust")]
    [InlineData(PromptEffectKind.PlaceThreat, 1, "Printed threat placed 1", "Icon_place_threat")]
    [InlineData(PromptEffectKind.MoveThreat, 2, "Printed threat moved 2", "Icon_move_threat")]
    public void APrintedEffectIsOneChipNamingItsPrintedAmount(PromptEffectKind kind, long amount, string label, string icon)
    {
        Wording wording = DecisionWording.Effect(new PromptEffect(kind, amount));
        Assert.Equal(label, wording.Label);
        Assert.Equal(icon, "Icon_" + wording.Icon);
        Assert.NotEmpty(wording.Tooltip);
    }
}

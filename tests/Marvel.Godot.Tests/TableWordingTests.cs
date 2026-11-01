using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

// The wording table is the client's only English for typed facts; these pin what each fact reads as.
public sealed class TableWordingTests
{
    [Fact]
    public void EveryFactHasALabelATooltipAndEveryStateFactAnIcon()
    {
        foreach (TableFact fact in Enum.GetValues<TableFact>())
        {
            Wording wording = TableWording.For(fact);
            Assert.False(string.IsNullOrWhiteSpace(wording.Label), fact.ToString());
            Assert.False(string.IsNullOrWhiteSpace(wording.Tooltip), fact.ToString());
            Assert.True(wording.Label.Split(' ').Length <= 3, $"{fact} label is not short: {wording.Label}");
            Assert.Equal(fact is not (TableFact.HeroForm or TableFact.AlterEgoForm or TableFact.Engaged
                or TableFact.DecisionRequired or TableFact.DecisionOptional or TableFact.DecisionCause
                or TableFact.StepThreat or TableFact.StepActivation or TableFact.StepEncounter),
                wording.Icon.Length > 0);
        }
    }

    [Fact]
    public void FactsShareAnIconOnlyWhereTheIconReadmeSaysTheyStateOneConcept()
    {
        // assets/icons/table/README.md: the lock and the diamond each mark one concept.
        string[][] shared =
        [
            [nameof(TableFact.AreaIsolation), nameof(TableFact.OutOfReach)],
            [nameof(TableFact.CentralTokensWaitFor4A), nameof(TableFact.CompletionStoresDominion), nameof(TableFact.StoredDominions)],
        ];
        string[][] groups = [.. Enum.GetValues<TableFact>().Where(fact => TableWording.For(fact).Icon.Length > 0)
            .GroupBy(fact => TableWording.For(fact).Icon).Where(group => group.Count() > 1)
            .Select(group => group.Select(fact => fact.ToString()).Order(StringComparer.Ordinal).ToArray())];
        Assert.Equal(shared, groups.OrderBy(group => group[0], StringComparer.Ordinal));
    }

    [Fact]
    public void EveryRuleKindAndReachReasonHasWording()
    {
        foreach (KangNoteKind kind in Enum.GetValues<KangNoteKind>())
            Assert.False(string.IsNullOrWhiteSpace(TableWording.Rule(kind, 2)));
        foreach (ReachReason reason in Enum.GetValues<ReachReason>())
            Assert.Equal(TableFact.OutOfReach, TableWording.Fact(reason));
        Assert.Throws<ArgumentOutOfRangeException>(() => TableWording.Fact((KangNoteKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => TableWording.Fact((ReachReason)99));
    }

    [Theory]
    [InlineData(1, "counts the 1 player who started the scenario.")]
    [InlineData(3, "counts all 3 players who started the scenario.")]
    public void APerPlayerValueCountsThePlayersWhoStartedTheScenario(int players, string ending)
    {
        // rr:per-player-icon: "multiplies that value by the number of players who started the scenario".
        Assert.EndsWith(ending, TableWording.Rule(KangNoteKind.EachPlayerIsArea, players), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, "Threat")]
    [InlineData(2, "Activation")]
    [InlineData(4, "Encounter")]
    public void EachVillainPhaseStepHasItsFact(int step, string label) =>
        Assert.Equal(label, TableWording.For(TableWording.Step(step)).Label);

    [Fact]
    public void StepThreeIsNotAVillainPhaseFocus() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TableWording.Step(3));

    [Theory]
    [InlineData(1, null, 0, 0, "Kang (I)", "Defeat Kang (I) to split the table into separate game areas.")]
    [InlineData(1, 9, 0, 0, "Kang (I)", "Kang (I) is defeated. At the end of the phase each player forms")]
    [InlineData(2, null, 2, 0, "Kang (II)", "2 Kang (II) remain. A game area's group joins another")]
    [InlineData(2, null, 1, 1, "Kang (II)", "1 Kang (II) remains.")]
    [InlineData(2, null, 0, 2, "Kang (II)", "No Kang (II) remains. At the end of the phase the last group leaves")]
    [InlineData(3, null, 0, 3, "Kang (III)", "Defeat Kang (III) to win.")]
    public void ProgressWordsTheStageAndCounts(int stage, int? split, int remaining, int stored, string label, string start)
    {
        var kang = new KangTableDescriptor(stage, remaining, stored, [], split, []);
        Assert.Equal(label, TableWording.Stage(kang));
        Assert.StartsWith(start, TableWording.Progress(kang), StringComparison.Ordinal);
        Assert.EndsWith(stored switch
        {
            0 => ".",
            1 => " 1 Kang's Dominion is stored under stage 4A.",
            _ => $" {stored} Kang's Dominions are stored under stage 4A.",
        }, TableWording.Progress(kang), StringComparison.Ordinal);
    }

    [Fact]
    public void ADepartureNamesItsCauseItsPlayersAndWhereTheyGo()
    {
        Assert.Equal("Kang (Iron Lad) was defeated: Alice and Bob leave at the end of the phase and join another game area.",
            TableWording.Departure("Kang (Iron Lad)", ["Alice", "Bob"], completed: false, reunion: false));
        Assert.Equal("Kang (Rama-Tut) completed his stage: the retained components leave at the end of the phase, "
            + "and the table reunites at stage 4A.", TableWording.Departure("Kang (Rama-Tut)", [], completed: true, reunion: true));
    }

    [Fact]
    public void OnePlayerLeavesAndJoinsInTheSingular()
    {
        Assert.Equal("Kang (Iron Lad) was defeated: Spider-Man leaves at the end of the phase and joins another game area.",
            TableWording.Departure("Kang (Iron Lad)", ["Spider-Man"], completed: false, reunion: false));
        Assert.Equal("Kang (Iron Lad) completed his stage: Spider-Man leaves at the end of the phase, and the table "
            + "reunites at stage 4A.", TableWording.Departure("Kang (Iron Lad)", ["Spider-Man"], completed: true, reunion: true));
    }

    [Theory]
    [InlineData(7L, 10L, "7/10")]
    [InlineData(7L, null, "HP 7")]
    [InlineData(7L, 0L, "HP 7")]
    [InlineData(null, 10L, "HP —")]
    public void ASeatsHealthReadsAsAFractionWhenTheMaximumIsKnown(long? health, long? maximum, string expected) =>
        Assert.Equal(expected, TableWording.Health(health, maximum));

    [Fact]
    public void ATrackerReadsItsCountsAndCounter()
    {
        Assert.Equal("Kang (II) 2 of 3, Dominions 1", TableWording.TrackerReading("Kang (II)", 2, 3, "Dominions", 1));
        Assert.Equal("Kang (III)", TableWording.TrackerReading("Kang (III)", 0, 0, null, 0));
        Assert.Equal("Player 3", TableWording.Seat(2));
    }

    [Theory]
    [InlineData(DecisionSubject.None, null, "")]
    [InlineData(DecisionSubject.Resolving, "Rhino", "Resolving Rhino")]
    [InlineData(DecisionSubject.MinionActivation, "Hydra Soldier", "Choosing the next minion activation")]
    public void ADecisionsSubjectIsOneShortLine(DecisionSubject subject, string? title, string expected) =>
        Assert.Equal(expected, TableWording.DecisionSubjectLine(new DecisionFrame("Thor", subject, title, false, [], "")));

    [Fact]
    public void AnAreaIsTitledByItsPlayers()
    {
        Assert.Equal("Alice · Bob", TableWording.AreaTitle(["Alice", "Bob"]));
        Assert.Equal("Dan", TableWording.AreaTitle(["Dan"]));
        Assert.Equal("Retained area", TableWording.AreaTitle([]));
    }

    [Theory]
    [InlineData(DecisionSubject.None, null, "Decision for Thor")]
    [InlineData(DecisionSubject.Resolving, "Rhino", "Decision for Thor · Resolving Rhino")]
    [InlineData(DecisionSubject.Inspecting, "False Alarm", "Decision for Thor · Inspecting False Alarm")]
    [InlineData(DecisionSubject.MinionActivation, "Hydra Soldier", "Decision for Thor · Choosing the next minion activation")]
    public void ADecisionNamesItsAnsweringSeatAndSubject(DecisionSubject subject, string? title, string expected) =>
        Assert.Equal(expected, TableWording.DecisionContext(new DecisionFrame("Thor", subject, title, false, [], "")));

    [Fact]
    public void ADecisionSaysWhetherItMayBeDeclinedAndWhatCausedIt()
    {
        var frame = new DecisionFrame("Thor", DecisionSubject.None, null, Optional: true, ["Heimdall", "Rhino"], "Look.");
        Assert.Equal("You may pass this opportunity.", TableWording.DecisionRequirement(frame, "Pass this opportunity"));
        Assert.Equal("Choose to continue.", TableWording.DecisionRequirement(frame with { Optional = false }, "Pass"));
        Assert.Equal("Cause: Heimdall, Rhino. Look.", TableWording.DecisionResolution(frame));
        Assert.Equal("Look.", TableWording.DecisionResolution(frame with { Causes = [] }));
    }
}

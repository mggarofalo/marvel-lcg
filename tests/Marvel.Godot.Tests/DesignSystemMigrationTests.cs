using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

// The ordinary table states its draft, payment, ability windows and results as trackers and chips.
public sealed class DesignSystemMigrationTests
{
    [Theory]
    [InlineData(Game.ResolveMulligans, "Redraw")]
    [InlineData(Game.EndPhaseVerb, "Discard")]
    public void StagedHandCardsAreATrackerAgainstTheMostThatCanBeStaged(string verb, string label)
    {
        var composer = Composer(new Affordance(3, verb, 19, 0, verb, Targets: new TargetRequest([41, 42, 43], 0, 3)));
        composer.SelectAffordance(3);
        composer.SelectTargets([42]);

        DraftPart part = Assert.Single(TableDraftSummary.Parts(composer, Prompt([Visible(3, 19)]))!);

        Assert.Equal((label, 1, 3), (part.Label, part.Filled, part.Total));
        Assert.True(DraftFactsRow.Tracks(part));
        Assert.Contains("staged for", part.Text);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 3, 3)]
    public void SelectedTargetsAreATrackerAgainstTheMostThatCanBeSelected(int minimum, int maximum, int total)
    {
        var composer = Composer(new Affordance(3, "Attack", 19, 0, "Attack",
            Targets: new TargetRequest([41, 42, 43], minimum, maximum)));
        composer.SelectAffordance(3);
        composer.SelectTargets([42]);

        DraftPart targets = Assert.Single(TableDraftSummary.Parts(composer, Prompt([Visible(3, 19)]))!,
            part => part.Label == DecisionWording.DraftTargets);

        Assert.Equal((1, total), (targets.Filled, targets.Total));
    }

    [Fact]
    public void AnUntrackedPartIsAChipOfItsWordsAndTheSummaryJoinsEveryPart()
    {
        var composer = Composer(new Affordance(3, "Attack", 19, 0, "Attack"));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt([Visible(3, 19) with { Description = "Exhaust Spider-Man to attack." }]);

        DraftPart action = Assert.Single(TableDraftSummary.Parts(composer, prompt)!);

        Assert.False(DraftFactsRow.Tracks(action));
        Assert.Equal(action.Text, TableDraftSummary.From(composer, prompt));
        Assert.False(DraftFactsRow.Tracks(new DraftPart("No pips", DecisionWording.DraftPaid, 0, 0)));
    }

    [Fact]
    public void SelectedResourcesFillPaymentPipsAgainstWhatTheCostNeeds()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Web-Shooter",
            Costs: [new CostOption(19, "2", Sources: [new ResourceSource(41, "Y"), new ResourceSource(42, "Y")])]));
        composer.SelectAffordance(3);
        Assert.Equal((0, 2), PaymentProgressTracker.Pips(composer.Progress().Payment));
        composer.ToggleResource(41);
        Assert.Equal((1, 2), PaymentProgressTracker.Pips(composer.Progress().Payment));
        composer.ToggleResource(42);
        Assert.Equal((2, 2), PaymentProgressTracker.Pips(composer.Progress().Payment));

        DraftPart paid = Assert.Single(TableDraftSummary.Parts(composer, Prompt([Visible(3, 19)]))!,
            part => part.Label == DecisionWording.DraftPaid);
        Assert.Equal((2, 2), (paid.Filled, paid.Total));
    }

    [Fact]
    public void AnOverpaymentFillsTheCostAndCountsTheRestAsExcess()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Web-Shooter",
            Costs: [new CostOption(19, "1", Sources: [new ResourceSource(41, "Y"), new ResourceSource(42, "Y")])]));
        composer.SelectAffordance(3);
        composer.ToggleResource(41);
        composer.ToggleResource(42);
        PaymentProgress payment = composer.Progress().Payment;

        Assert.Equal((1, 1), PaymentProgressTracker.Pips(payment));
        Assert.Equal(1, payment.ExcessIcons);
        DraftPart paid = Assert.Single(TableDraftSummary.Parts(composer, Prompt([Visible(3, 19)]))!,
            part => part.Label == DecisionWording.DraftPaid);
        Assert.Equal((1, 1), (paid.Filled, paid.Total));
    }

    [Fact]
    public void AnUnknownRequirementHasNoPipsEvenWhenSatisfied()
    {
        var unknown = new PaymentProgress(CostSelectionState.Selected, 19, 1, 2, 2, 0, 0, 0, IsSatisfied: true);
        Assert.Null(PaymentProgressTracker.Pips(unknown));
        Assert.Equal((2, 3), PaymentProgressTracker.Pips(unknown with { Required = 3 }));
    }

    [Fact]
    public void TheAbilityWindowTitlesItsOneAbilityOrItsQuestion()
    {
        var prompt = new PromptPresentation("Choose a response", DecisionFrame.Empty, "", []);
        Assert.Equal("Spider-Man Interrupt", AbilityWindowHead.Title(prompt, null, ["Spider-Man Interrupt"]));
        Assert.Equal("Choose a response", AbilityWindowHead.Title(prompt, null, ["Spider-Man Interrupt", "Heimdall Response"]));
        var notice = new ForcedAbilityNotification(4, "Black Cat — Forced Response", "Discarded 2 cards.", 0, 1, true);
        Assert.Equal("Black Cat — Forced Response", AbilityWindowHead.Title(prompt, notice, ["Spider-Man Interrupt"]));
    }

    [Theory]
    [InlineData(Outcome.PlayersWin, PlayerLossReason.Unspecified, nameof(TableFact.OutcomeVillainDefeated))]
    [InlineData(Outcome.VillainWins, PlayerLossReason.Unspecified, nameof(TableFact.OutcomeSchemeCompleted))]
    [InlineData(Outcome.PlayersLose, PlayerLossReason.AllPlayersEliminated, nameof(TableFact.OutcomeEliminated))]
    [InlineData(Outcome.PlayersLose, PlayerLossReason.EncounterCardsExhausted, nameof(TableFact.OutcomeEncounterExhausted))]
    [InlineData(Outcome.PlayersLose, PlayerLossReason.Unspecified, nameof(TableFact.OutcomeEnded))]
    public void AFinishedGameNamesWhyItEndedAsOneFact(Outcome outcome, PlayerLossReason reason, string expected)
    {
        TableFact fact = Enum.Parse<TableFact>(expected);
        Assert.Equal(fact, DecisionWording.OutcomeFact(outcome, reason));
        // The fact's tooltip is the engine-owned result sentence, word for word.
        if (fact != TableFact.OutcomeEnded)
            Assert.Equal(OutcomeDescription.Result(outcome, reason), TableWording.For(fact).Tooltip);
    }

    [Fact]
    public void AnotherSeatsDecisionNamesItsCausesThenWhatItResolves()
    {
        Wording[] facts = DecisionWording.Pending(["Stampede"], ["Rhino"]);
        Assert.Equal(["Stampede", "Rhino"], facts.Select(fact => fact.Label));
        Assert.Contains("caused the decision", facts[0].Tooltip);
        Assert.Contains("resolves", facts[1].Tooltip);
        Assert.Empty(DecisionWording.Pending([], []));
    }

    [Fact]
    public void TheSidebarNamesYourSeatTheActiveSeatAndTheShownSeatAsChips()
    {
        Wording[] facts = TableWording.SeatContext("Spider-Man", "Thor", "Thor");
        Assert.Equal(["your_seat", "active_seat", "public_focus"], facts.Select(fact => fact.Icon));
        Assert.Equal(["Spider-Man", "Thor", "Thor"], facts.Select(fact => fact.Label));
        Assert.Equal("It is Thor's turn.", facts[1].Tooltip);
        Wording shared = Assert.Single(TableWording.SeatContext(null, null, null));
        Assert.Equal("Shared view", shared.Label);
    }

    [Theory]
    [InlineData(2, "Boost +2", "2 uncancelled boost icons")]
    [InlineData(1, "Boost +1", "1 uncancelled boost icon.")]
    [InlineData(0, "Boost +0", "0 uncancelled boost icons")]
    public void ARevealedBoostCardCountsItsIconsInPlaceOfTheUnresolvedBoost(long icons, string label, string tooltip)
    {
        var attack = new DecisionAttack("Rhino", "Spider-Man", 2, false, true, false) { BoostIcons = icons };
        Wording boost = DecisionWording.AttackFacts(attack)[^1];
        Assert.Equal(label, boost.Label);
        Assert.Contains(tooltip, boost.Tooltip);
        Assert.Single(DecisionWording.AttackFacts(attack), fact => fact.Icon == "boost");
    }

    private static DecisionComposer Composer(params Affordance[] offers) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", false, offers));

    private static PromptPresentation Prompt(IReadOnlyList<AffordancePresentation> affordances) =>
        new("", DecisionFrame.Empty, "", affordances);

    private static AffordancePresentation Visible(int id, int card) => new(
        id, "Visible", null, "Play", "Visible", card, 0, null, "", [])
    {
        Source = new AffordanceSourceDescriptor(AffordanceAnchorKind.Card, card, null, 0),
    };
}

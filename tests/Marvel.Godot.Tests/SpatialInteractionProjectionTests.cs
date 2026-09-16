using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SpatialInteractionProjectionTests
{
    [Fact]
    public void ExplicitCardAnchorRemainsOperableWhenNoLocatedSourceIsProjected()
    {
        var composer = Composer(new Affordance(9, "End Phase", 1, 0, "End Phase"));
        var visible = new AffordancePresentation(
            9, "End Phase", null, "End Phase", "Spider-Man", 1, 0, null, "", [])
        {
            AnchorKind = AffordanceAnchorKind.Card,
        };
        PromptPresentation prompt = Prompt([visible]);

        CardInteractionControlDescriptor action = Assert.Single(
            BoardInteractionControlProjection.From(composer, prompt));

        Assert.Equal(1, action.CardId);
        Assert.Equal(CardInteractionIntent.Action, action.Intent);
        Assert.Equal(CardInteractionCue.OfferedAction,
            BoardInteractionCueProjection.From(composer, prompt)[1]);
    }

    [Fact]
    public void AlternativeCostsAreChosenOnTheSourceCard()
    {
        CostOption[] costs = [new(19, "first"), new(19, "second")];
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible", Costs: costs));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt([Visible(3, 19)], costs);

        CardInteractionControlDescriptor[] controls = [.. BoardInteractionControlProjection
            .From(composer, prompt).Where(control => control.Intent == CardInteractionIntent.Cost)];

        Assert.Equal([0, 1], controls.Select(control => control.Option));
        Assert.All(controls, control => Assert.Equal(19, control.CardId));
    }

    [Fact]
    public void ReadyDraftExecutesOnItsSourceCard()
    {
        var composer = Composer(new Affordance(3, "Use", 19, 0, "Visible"));
        composer.SelectAffordance(3);

        CardInteractionControlDescriptor submit = Assert.Single(
            BoardInteractionControlProjection.From(composer, Prompt([Visible(3, 19)])),
            control => control.Intent == CardInteractionIntent.Submit);

        Assert.Equal(19, submit.CardId);
        Assert.Equal("EXECUTE", submit.Text);
    }

    [Fact]
    public void SelectedDraftReplacesCompetingActionsWithLocalCompositionControls()
    {
        var composer = Composer(
            new Affordance(3, "Play", 19, 0, "Web-Shooter"),
            new Affordance(4, "Use", 20, 0, "Change form"));
        composer.SelectAffordance(3);

        IReadOnlyList<CardInteractionControlDescriptor> controls =
            BoardInteractionControlProjection.From(
                composer, Prompt([Visible(3, 19), Visible(4, 20)]));

        Assert.DoesNotContain(controls, control => control.Intent == CardInteractionIntent.Action);
        Assert.Contains(controls, control => control.Intent == CardInteractionIntent.Submit);
    }

    [Fact]
    public void TableDraftSummaryNamesTheSelectedActionAndPaymentProgress()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Web-Shooter",
            Costs: [new CostOption(19, "1", Sources: [new ResourceSource(41, "Y")])]));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt([Visible(3, 19)], composer.Selected!.CostOptions);

        string before = Assert.IsType<string>(TableDraftSummary.From(composer, prompt));
        Assert.Contains("SELECTED · Web-Shooter", before);
        Assert.Contains("RES 0", before);
        Assert.Contains("COMPOSING", before);

        composer.ToggleResource(41);

        string ready = Assert.IsType<string>(TableDraftSummary.From(composer, prompt));
        Assert.Contains("RES 1", ready);
        Assert.Contains("READY", ready);
    }

    private static DecisionComposer Composer(params Affordance[] offers) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", false, offers));

    private static PromptPresentation Prompt(
        IReadOnlyList<AffordancePresentation> affordances,
        IReadOnlyList<CostOption>? costs = null) => new("", "", "", "", "",
        affordances.Select(view => view with { CostOptions = costs ?? [] }).ToArray());

    private static AffordancePresentation Visible(int id, int card) => new(
        id, "Visible", null, "Play", "Visible", card, 0, null, "", [])
    {
        Source = new AffordanceSourceDescriptor(AffordanceAnchorKind.Card, card, null, 0),
    };
}

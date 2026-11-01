using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>
/// The complete-choices sheet's head: the question as its title, who answers, whether it is required, its
/// causes and its source as chips, and the engine's instruction once, as the one-line reason.
/// </summary>
internal static class DecisionSheetHead
{
    /// <summary>The head's chips, in order: the answering seat, required or optional, then each cause.</summary>
    internal static Wording[] Facts(DecisionFrame frame, string declineLabel) =>
    [
        new(TableWording.For(TableFact.AnsweringSeat).Icon, frame.SeatName, TableWording.DecisionContext(frame)),
        TableWording.For(frame.Optional ? TableFact.DecisionOptional : TableFact.DecisionRequired) with
        {
            Tooltip = TableWording.DecisionRequirement(frame, declineLabel),
        },
        .. frame.Causes.Select(cause => TableWording.For(TableFact.DecisionCause) with
        {
            Label = cause, Tooltip = $"{TableWording.For(TableFact.DecisionCause).Tooltip} {cause}.",
        }),
    ];

    /// <summary>The head for the panel's current prompt, with the sheet's close control at the title's end.</summary>
    internal static VBoxContainer Create(DecisionPanel panel, Main main, Control close)
    {
        InterfaceScale scale = panel.ReadingScale;
        if (panel.composer is not { } composer || panel.world is not { } world)
            return TablePanel.Create("DecisionHead", Title("Current choice"), [], "", scale, close);
        PromptPresentation prompt = PromptPresentation.From(composer.Prompt, world);
        string title = DecisionCardChoices.IsChoice(composer.Prompt) ? DecisionCardChoices.Heading(composer.Prompt) : prompt.Heading;
        var facts = new List<Control>();
        Wording[] wordings = Facts(prompt.Frame, prompt.DeclineLabel);
        for (int index = 0; index < wordings.Length; index++)
            facts.Add(Chip.Create($"DecisionFact{index}", wordings[index], null, scale));
        if (prompt.ContextCards.Count == 1) facts.Add(Source(prompt.ContextCards[0], main));
        return TablePanel.Create("DecisionHead", Title(title), facts, prompt.Frame.Instruction, scale, close);
    }

    private static Label Title(string text) => new()
    {
        Name = "DecisionTitle", Text = text, ThemeTypeVariation = GodotThemeVariations.Heading,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    // The source stays one press from its inspector.
    private static Button Source(BoardCardPresentation card, Main main)
    {
        var source = new Button { Name = "DecisionSource", Text = card.Title, Flat = true, TooltipText = "Inspect card" };
        source.Pressed += () => main.boardController.ShowCardInspector(card, source, pinned: true);
        return source;
    }
}

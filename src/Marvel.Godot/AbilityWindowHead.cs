using Godot;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>
/// The ability window's head: the ability as its title and the decision's facts as chips, with the engine's whole
/// instruction on the title's hover. A forced notification keeps its result, the one thing it reports.
/// </summary>
internal static class AbilityWindowHead
{
    /// <summary>The head for <paramref name="prompt"/>, or for a forced notification when one is shown.</summary>
    internal static VBoxContainer Create(PromptPresentation prompt, ForcedAbilityNotification? notification,
        IReadOnlyList<string> titles, InterfaceScale scale)
        => Compose(Title(prompt, notification, titles), DecisionWording.DecisionDetail(prompt.Frame, prompt.DeclineLabel),
            DecisionSheetHead.Facts(prompt.Frame, prompt.DeclineLabel), notification?.Summary ?? "", scale);

    /// <summary>The head from its parts: the answering seat is the first fact.</summary>
    internal static VBoxContainer Compose(string text, string detail, IReadOnlyList<Wording> wordings, string reason,
        InterfaceScale scale)
    {
        var title = new Label
        {
            Name = "AbilityWindowHeading", Text = text, TooltipText = detail,
            ThemeTypeVariation = GodotThemeVariations.Heading, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        var facts = new List<Control>();
        for (int index = 0; index < wordings.Count; index++)
            facts.Add(Chip.Create(index == 0 ? "AbilityWindowAnswerer" : $"AbilityWindowFact{index}", wordings[index], null, scale));
        return TablePanel.Create("AbilityWindowHead", title, facts, reason, scale);
    }

    /// <summary>The window's title: the notification's, the one offered ability's, or the prompt's question.</summary>
    internal static string Title(PromptPresentation prompt, ForcedAbilityNotification? notification,
        IReadOnlyList<string> titles) => notification?.Title ?? (titles.Count == 1 ? titles[0] : prompt.Heading);

    /// <summary>Marks a choice with its printed effect's icon, named in its accessible description.</summary>
    internal static void MarkEffect(Button button, PromptEffect effect, InterfaceScale scale)
    {
        Wording wording = DecisionWording.Effect(effect);
        button.Icon = CardGlyphRendering.Rasterize($"Marvel.Godot.Assets.TableIcons.{wording.Icon}.svg", TableBlockStyle.Icon(scale));
        button.SetMeta("table_effect", wording.Label);
        button.AccessibilityDescription = string.Join("\n", new[] { wording.Label, button.AccessibilityDescription }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}

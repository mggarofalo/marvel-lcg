using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Binds menu leaves to the existing authorized prompt and its single composer.</summary>
internal static class CardContextMenuBinding
{
    internal static bool Show(DecisionPanel panel, DecisionComposer composer, CardPointerGesture gesture)
    {
        if (panel.world is not { } world || gesture.Card.TargetId is not { } source) return false;
        int generation = panel.GetRenderGeneration();
        var offers = PromptPresentation.From(composer.Prompt, world).Affordances;
        var entries = CardMenuChoices.Build(composer, world, source, offers);
        var previews = new CardPreviewOwnership();
        previews.Changed += id => panel.CurrentBoard?.PreviewMenuCard(id, gesture.Source);
        CardContextMenu.Show(gesture.Source, entries,
            () => panel.IsCurrentDraft(composer, generation),
            choice => Choose(panel, composer, gesture, generation, choice),
            () => panel.CurrentBoard?.InspectCard(gesture.Card, gesture.Source), previews, panel.Theme);
        return true;
    }

    private static void Choose(DecisionPanel panel, DecisionComposer composer,
        CardPointerGesture gesture, int generation, CardMenuChoice choice)
    {
        var binding = panel.BindTableDraft(composer, generation);
        if (!CardMenuChoices.Select(composer, binding, choice)) return;
        var offered = PromptPresentation.From(composer.Prompt, panel.world!).Affordances.Single(offer => offer.Id == choice.Offer);
        panel.LocalDecision.Begin(gesture, offered);
        panel.RaiseDraftStarted();
        if (choice.Commit && composer.TryBuild(out var decision, out _))
        {
            panel.NotifySubmitted(decision!, generation);
            return;
        }
        panel.CurrentBoard?.AnchorDraft(gesture.Source.GetGlobalRect().End, choice.Offer);
        BoardInteractionBinder.RefreshDraft(panel, composer, generation, gesture.Card.TargetId!.Value);
    }
}

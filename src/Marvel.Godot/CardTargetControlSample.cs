using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Exercises spatial selectors with a synthetic two-target contract, not a played game.</summary>
internal static class CardTargetControlSample
{
    internal static void Add(Control fixture, CardControl face, BoardCardPresentation card)
    {
        if (card.Concealed || card.TargetId is not { } target) return;
        var offer = new Affordance(1, "Synthetic target", int.MaxValue - 1, 0,
            "Synthetic target", new TargetRequest([target, int.MaxValue], 1, 1));
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption,
            TimingPriority.Untimed, "Synthetic", "Choose a target", false, [offer]));
        composer.SelectAffordance(1);
        var presentation = new AffordancePresentation(1, offer.Label, null, offer.Verb,
            "Synthetic source", offer.AnchorId, 0, null, "", [])
        {
            AnchorKind = AffordanceAnchorKind.Card, TargetRequest = offer.Targets,
        };
        var prompt = new PromptPresentation("", "", "", "", "", [presentation]);
        var visible = new Dictionary<int, List<CardControl>> { [target] = [face] };
        var controls = new BoardCardInteractionControls(() => GodotObject.IsInstanceValid(face));
        controls.Track(face, card, isHand: false);
        face.SetMeta("spatial_resting_z", face.ZIndex);
        fixture.SetMeta("synthetic_target", target);
        fixture.VisibilityChanged += () =>
        {
            if (fixture.IsVisibleInTree())
                Callable.From(() => controls.Present(visible, composer, prompt)).CallDeferred();
        };
        controls.Bind(gesture =>
        {
            if (gesture.Intent != CardInteractionIntent.Target) return false;
            if (composer.Targets.Contains(target)) composer.RemoveTarget(target);
            else composer.AddTarget(target);
            fixture.SetMeta("synthetic_target_selected", composer.Targets.Contains(target));
            controls.Present(visible, composer, prompt);
            return true;
        });
    }
}

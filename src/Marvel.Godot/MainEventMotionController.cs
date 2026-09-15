using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Sequences semantic event cues and their board focus animation.</summary>
internal sealed class MainEventMotionController
{
    private readonly Main main;

    internal MainEventMotionController(Main main)
    {
        this.main = main;
    }

    internal void Present(IReadOnlyList<EventPresentation> presented)
    {
        Skip();
        if (!main.eventMotion.ButtonPressed || presented.Count == 0)
        {
            return;
        }

        main.eventSkip.Disabled = false;
        int generation = main.eventGeneration;
        main.eventTween = main.CreateTween();
        foreach (EventPresentation entry in presented)
        {
            main.eventTween.TweenCallback(
                Callable.From(() => BeginCue(entry, generation))).Dispose();
            main.eventTween.TweenProperty(main.eventCue, "modulate:a", 1.0f, 0.10).Dispose();
            main.eventTween.TweenInterval(0.30).Dispose();
            main.eventTween.TweenProperty(main.eventCue, "modulate:a", 0.35f, 0.10).Dispose();
        }

        main.eventTween.TweenCallback(
            Callable.From(() => Finish(generation))).Dispose();
    }

    internal void BeginCue(EventPresentation entry, int generation)
    {
        if (generation != main.eventGeneration)
        {
            return;
        }

        main.eventCueKind.Text = entry.Motion.ToString().ToUpperInvariant();
        main.eventCue.Visible = true;
        EventCueBoardFocus.Present(main, entry);
        main.eventCueKind.ThemeTypeVariation = entry.Motion switch
        {
            EventMotionKind.Damage or EventMotionKind.Defeat or EventMotionKind.Terminal =>
                GodotThemeVariations.DangerText,
            EventMotionKind.Create or EventMotionKind.Heal =>
                GodotThemeVariations.StatusText,
            _ => GodotThemeVariations.Eyebrow,
        };
        main.eventCue.Modulate = new Color(1f, 1f, 1f, 0.20f);
        main.boardRender?.Present(entry.Anchors);
    }

    internal void Skip()
    {
        main.eventGeneration++;
        ReleaseTween();
        SetSettled();
    }

    internal void ReleaseTween()
    {
        Tween? tween = main.eventTween;
        main.eventTween = null;
        if (tween is null)
        {
            return;
        }

        tween.Kill();
        tween.Dispose();
    }

    internal void Finish(int generation)
    {
        if (generation != main.eventGeneration)
        {
            return;
        }

        SetSettled();
    }

    internal void SetSettled()
    {
        main.eventCue.Visible = false;
        main.eventCue.Modulate = Colors.White;
        main.eventSkip.Disabled = true;
        main.boardRender?.Present([]);
    }
}

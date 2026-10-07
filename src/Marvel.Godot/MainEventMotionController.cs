using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Sequences authorized result cues while leaving game decisions available.</summary>
internal sealed class MainEventMotionController : IDisposable
{
    private readonly Main main;
    private readonly EventCueSurface surface;
    private readonly EventPlaybackSequence playback = new();

    internal MainEventMotionController(Main main)
    {
        this.main = main;
        surface = new EventCueSurface(main, () => Move(-1), () => Move(1), TogglePlayback);
    }

    internal void Present(IReadOnlyList<EventPresentation> presented)
    {
        Skip();
        playback.Replace(presented, main.eventMotion.ButtonPressed);
        ShowCurrent();
        Schedule();
    }

    private void Schedule()
    {
        if (!playback.Playing || playback.Current is null) return;
        int generation = main.eventGeneration;
        main.eventSkip.Disabled = false;
        main.eventTween = main.CreateTween();
        for (int index = playback.Index; index < playback.Count; index++)
        {
            double seconds = playback[index].Motion is EventMotionKind.Move
                or EventMotionKind.State or EventMotionKind.Create ? 0.45 : 1.4;
            main.eventTween.TweenInterval(seconds).Dispose();
            main.eventTween.TweenCallback(Callable.From(() => Advance(generation))).Dispose();
        }
    }

    private void Advance(int generation)
    {
        if (generation != main.eventGeneration) return;
        if (playback.Advance()) ShowCurrent();
        else SetSettled();
    }

    private void ShowCurrent(bool focusSubject = false)
    {
        if (playback.Current is not { } entry) return;
        surface.Present(entry, playback.Index, playback.Count, playback.Playing);
        if (focusSubject) EventCueBoardFocus.Present(main, entry);
        main.boardRender?.Present(entry.Anchors);
    }

    internal void BeginCue(EventPresentation entry, int generation)
    {
        if (generation != main.eventGeneration) return;
        surface.Present(entry, playback.Index, playback.Count, playback.Playing);
        EventCueBoardFocus.Present(main, entry);
        main.boardRender?.Present(entry.Anchors);
    }

    private void Move(int delta)
    {
        StopTween();
        playback.Move(delta);
        ShowCurrent(focusSubject: true);
    }

    private void TogglePlayback()
    {
        if (playback.Playing) Pause();
        else
        {
            StopTween();
            playback.Resume();
            ShowCurrent();
            Schedule();
        }
    }

    internal void Pause()
    {
        StopTween();
        playback.Pause();
        ShowCurrent();
    }

    internal void Complete()
    {
        StopTween();
        playback.Finish();
        ShowCurrent();
        main.boardRender?.Present([]);
    }

    internal void Skip()
    {
        StopTween();
        playback.Clear();
        surface.Clear();
        main.boardRender?.Present([]);
    }

    private void StopTween()
    {
        main.eventGeneration++;
        ReleaseTween();
        main.eventSkip.Disabled = true;
    }

    internal void ReleaseTween()
    {
        Tween? tween = main.eventTween;
        main.eventTween = null;
        if (tween is null) return;
        tween.Kill();
        tween.Dispose();
    }

    internal void Finish(int generation)
    {
        if (generation == main.eventGeneration) SetSettled();
    }

    internal void SetSettled()
    {
        playback.Finish();
        if (playback.Current is { } entry)
            surface.Present(entry, playback.Index, playback.Count, false);
        main.eventSkip.Disabled = true;
        main.boardRender?.Present([]);
    }

    internal void RefreshVisibility() => surface.RefreshVisibility(playback.Current is not null);
    public void Dispose()
    {
        main.eventGeneration++;
        ReleaseTween();
        surface.Dispose();
    }

}

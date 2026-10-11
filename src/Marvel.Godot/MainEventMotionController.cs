using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps accepted consequences readable without delaying the current decision.</summary>
internal sealed class MainEventMotionController : IDisposable
{
    private readonly Main main;
    private readonly EventCueSurface surface;
    private readonly EventActivityPresenter activity;
    private bool hasResults;

    internal MainEventMotionController(Main main)
    {
        this.main = main;
        surface = new EventCueSurface(main);
        activity = new EventActivityPresenter { Name = "AcceptedEventActivity" };
        main.AddChild(activity);
        activity.Configure(main);
    }

    internal void Present(IReadOnlyList<EventPresentation> presented)
    {
        ReleaseTween();
        main.eventGeneration++;
        hasResults = presented.Count > 0;
        surface.Present(presented);
        activity.Present(presented);
        main.eventSkip.Disabled = true;
        main.eventSkip.Visible = false;
        main.boardRender?.Present([]);
    }

    internal void BeginCue(EventPresentation entry, int generation)
    {
        if (generation != main.eventGeneration) return;
        main.boardRender?.Present(entry.Anchors);
    }

    internal void Pause() => SetSettled();
    internal void Complete() => SetSettled();

    internal void Skip()
    {
        main.eventGeneration++;
        ReleaseTween();
        hasResults = false;
        surface.Clear();
        activity.Clear();
        main.eventSkip.Disabled = true;
        main.boardRender?.Present([]);
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
        ReleaseTween();
        activity.Complete();
        main.eventSkip.Disabled = true;
        main.boardRender?.Present([]);
    }

    internal void RefreshVisibility() => surface.RefreshVisibility(hasResults);

    public void Dispose()
    {
        main.eventGeneration++;
        ReleaseTween();
        surface.Dispose();
        activity.Clear();
        activity.QueueFree();
    }
}

using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps accepted consequences readable without delaying the current decision.</summary>
internal sealed class MainEventMotionController : IDisposable
{
    private readonly Main main;
    private readonly EventActivityPresenter activity;
    private DecisionPanel? draftOwner;

    internal MainEventMotionController(Main main)
    {
        this.main = main;
        activity = new EventActivityPresenter { Name = "AcceptedEventActivity" };
        main.AddChild(activity);
        activity.Configure(main);
    }

    internal void Present(IReadOnlyList<EventPresentation> presented)
    {
        if (draftOwner is null)
        {
            draftOwner = main.decisions;
            draftOwner.DraftStarted += SettleForDraft;
        }
        ReleaseTween();
        main.eventGeneration++;
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
    private void SettleForDraft() => activity.SettleForDraft();

    internal void Skip()
    {
        main.eventGeneration++;
        ReleaseTween();
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

    internal void RefreshVisibility()
    {
        main.eventCue.Visible = false;
        main.eventCueSummary.Text = string.Empty;
        main.GetNode<Control>("StatusBar/Spacer").CustomMinimumSize = Vector2.Zero;
    }

    public void Dispose()
    {
        if (InteractionControl.IsUsable(draftOwner)) draftOwner!.DraftStarted -= SettleForDraft;
        main.eventGeneration++;
        ReleaseTween();
        activity.Clear();
        activity.QueueFree();
    }
}

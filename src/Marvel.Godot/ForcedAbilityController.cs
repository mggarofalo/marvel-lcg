using Godot;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shows forced receipts without submitting an invented engine answer or replaying a resolved effect.</summary>
internal sealed class ForcedAbilityController(Main main) : IDisposable
{
    private readonly ForcedAbilityQueue queue = new();
    private AbilityModalFrame? frame;
    private Prompt? prompt;
    private long revision;

    internal void Record(EngineResponse response, IReadOnlyList<EventPresentation> events, bool accepted, bool reset)
    {
        prompt = response.Prompt;
        revision = response.Revision;
        queue.Record(response.GameId, revision, events, accepted, reset);
        Present();
    }

    private void Present()
    {
        frame?.Dispose();
        frame = null;
        ForcedAbilityNotification? required = queue.RequiredDecision(prompt, revision);
        main.decisions.AbilityWindows.SetNotification(queue.Current is not null && required is null, required);
        if (queue.Current is not { } receipt || required is not null) return;
        ShowNotification(receipt);
    }

    private void ShowNotification(ForcedAbilityNotification receipt)
    {
        frame = new AbilityModalFrame(main, "AbilityWindow", main.decisions.Theme);
        frame.AddText("ForcedAbilityHeading", receipt.Title, GodotThemeVariations.Heading);
        if (receipt.SourceCard is { } source
            && frame.Frame.FindChild("ForcedAbilityHeading", true, false) is Control title)
            AbilitySourceInspection.Bind(main, title, source);
        frame.AddText("ForcedAbilitySummary", receipt.Summary, GodotThemeVariations.Body);
        if (!receipt.Completed)
            frame.AddText("ForcedAbilityState", "Resolution is still in progress", GodotThemeVariations.Caption);
        if (prompt is not null && prompt.PublicKind != PublicDecisionKind.PlayerAction
            && main.CurrentGame?.World is { } world)
            frame.AddText("PendingDecision", "Next: " + PromptPresentation.From(prompt, world).Heading,
                GodotThemeVariations.Caption);
        Button acknowledge = frame.AddButton("AcknowledgeForcedAbility", "Acknowledge", () => Acknowledge(receipt));
        acknowledge.SetMeta("table_mutation", true);
        Callable.From(() =>
        {
            if (InteractionControl.IsUsable(acknowledge)) acknowledge.GrabFocus();
        }).CallDeferred();
    }

    private void Acknowledge(ForcedAbilityNotification expected)
    {
        if (!ReferenceEquals(queue.Current, expected)) return;
        queue.Acknowledge();
        Present();
    }

    internal void Input(InputEvent input)
    {
        if (frame is not null) AbilityModalFocus.Route(frame.Frame, input);
    }

    internal void Clear()
    {
        queue.Clear();
        frame?.Dispose();
        frame = null;
        main.decisions.AbilityWindows.SetNotification(false, null);
    }

    public void Dispose()
    {
        queue.Clear();
        frame?.Dispose();
    }
}

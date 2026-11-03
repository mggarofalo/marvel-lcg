using Marvel.Protocol;
using Godot;
using Marvel.Rules.Prompts;
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
        frame.Content.AddChild(Facts(receipt));
        Button acknowledge = frame.AddButton("AcknowledgeForcedAbility", "Acknowledge", () => Acknowledge(receipt));
        acknowledge.SetMeta("table_mutation", true);
        Callable.From(() =>
        {
            if (InteractionControl.IsUsable(acknowledge)) acknowledge.GrabFocus();
        }).CallDeferred();
    }

    // What happened, whether it is still resolving and the decision that follows, as chips whole on hover.
    private HFlowContainer Facts(ForcedAbilityNotification receipt)
    {
        HFlowContainer row = DraftFactsRow.Create(main.interfaceScale);
        row.Name = "ForcedAbilityFacts";
        var parts = new List<DraftPart> { new(receipt.Summary) };
        if (!receipt.Completed) parts.Add(new(ForcedAbilityWording.InProgress.Tooltip, ForcedAbilityWording.InProgress.Label));
        if (prompt is not null && prompt.PublicKind != PublicDecisionKind.PlayerAction && main.CurrentGame?.World is { } world)
            parts.Add(new(ForcedAbilityWording.Next(PromptPresentation.From(prompt, world).Heading)));
        DraftFactsRow.Fill(row, parts);
        if (row.GetChildCount() > 0) row.GetChild(0).Name = "ForcedAbilitySummary";
        return row;
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

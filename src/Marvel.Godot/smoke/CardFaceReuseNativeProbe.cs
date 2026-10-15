using Godot;
using Marvel.Client;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Synthetic native checks of passive face transfer and its rejection boundaries.</summary>
public sealed partial class CardFaceReuseNativeProbe : Node
{
    /// <summary>Checks replacement facts and art without interpreting a gameplay rule.</summary>
    public string VerifyBoundaries()
    {
        var card = new BoardCardPresentation(7, 1, false, "Synthetic source", "", "UPGRADE", "READY", [])
        { RulesText = "Synthetic authorized text", FaceId = "01001a", Counters = [new("Uses", "3")] };
        string failure = VerifyStableFace(card, new EmptyArt());
        return failure.Length > 0 ? failure : VerifyReplacementFaces(card, new EmptyArt());
    }

    private string VerifyStableFace(BoardCardPresentation card, ICardArtProvider art)
    {
        CardControl current = CreateInitial(card, art);
        ulong initial = Face(current).GetInstanceId();
        int children = Face(current).GetChildCount();
        current = Replace(current, card with { Counters = [new("Uses", "3")] }, art);
        if (Face(current).GetInstanceId() != initial || Face(current).GetChildCount() != children)
            return "equal newly allocated facts do not preserve a single ready face";
        string failure = VerifyTransientReceipt(current, card, art);
        return failure;
    }

    private CardControl CreateInitial(BoardCardPresentation card, ICardArtProvider art)
    {
        using var initialReuse = new BoardCardFaceReuse(null);
        CardControl current = CardControl.CreateReconciled(card, CardDisplaySize.Board,
            InterfaceScale.Standard, art, initialReuse);
        AddChild(current);
        return current;
    }

    private string VerifyTransientReceipt(CardControl current, BoardCardPresentation card, ICardArtProvider art)
    {
        // This synthetic passive descendant exercises receipt rendering's mutable
        // progress fallback without introducing a card rule or an engine state.
        var progress = new Label { Name = "LiveProgressValue", Text = "5/5" };
        Face(current).AddChild(progress);
        EventActivityCardRendering.Apply(current, card with { Fields = [new("HEALTH", "4/5")] });
        if (progress.Text != "4/5") return "the synthetic receipt did not reach the mutable face value";
        current = RequireReplacement(current, card, art, out string failure);
        if (failure.Length > 0) return "an activity-mutated face survives under its original snapshot key";
        if (Face(current).FindChild("LiveProgressValue", true, false) is not null)
            return "reconciliation carries a stale transient value into the authoritative face";
        current.QueueFree();
        return string.Empty;
    }

    private string VerifyReplacementFaces(BoardCardPresentation card, ICardArtProvider art)
    {
        CardControl current = CreateInitial(card, art);
        current = RequireReplacement(current, card with { Counters = [new("Uses", "2")] }, art,
            out string failure);
        if (failure.Length > 0) return failure;
        current = RequireReplacement(current, current.DisplayedPresentation!, new EmptyArt(), out failure);
        if (failure.Length > 0) return "replacement art provider retains the old face";
        card = current.DisplayedPresentation! with { TargetId = 8 };
        current = RequireReplacement(current, card, current.FaceArt, out failure);
        if (failure.Length > 0) return "a different engine copy retains the old face";
        Control oldBody = Face(current);
        current = Replace(current, card with { Concealed = true, FaceId = null }, current.FaceArt);
        if (!oldBody.IsQueuedForDeletion() || current.GetNodeOrNull("CardSurface/CardFace") is not null
            || current.GetNodeOrNull("CardSurface/CardBack") is null)
            return "concealment retains an authorized printed face";
        current.QueueFree();
        return string.Empty;
    }

    /// <summary>Exercises an operational presentation without sending a mutation.</summary>
    public void ShowSending(Main main)
    {
        if (main.GetTree() != GetTree()) throw new InvalidOperationException("Probe and table must share a tree.");
        main.ApplyProgress(GameProgressPresentation.Resolving());
    }

    /// <summary>Returns the same unpaid draft to the explicit safe-retry state.</summary>
    public void ShowNotSent(Main main)
    {
        if (main.GetTree() != GetTree()) throw new InvalidOperationException("Probe and table must share a tree.");
        main.ApplyProgress(GameProgressPresentation.DecisionNotSent(
            new ClientStartupError("synthetic_not_sent", "Synthetic presentation boundary.")));
    }

    private CardControl RequireReplacement(CardControl current, BoardCardPresentation card,
        ICardArtProvider? art, out string failure)
    {
        Control body = Face(current);
        ulong prior = body.GetInstanceId();
        CardControl next = Replace(current, card, art);
        failure = Face(next).GetInstanceId() == prior
            ? "changed authorized state retains a prior face" : string.Empty;
        return next;
    }

    private CardControl Replace(CardControl current, BoardCardPresentation card, ICardArtProvider? art)
    {
        var previous = new BoardRenderResult();
        previous.Register(current.TargetId!.Value, current);
        using var reuse = new BoardCardFaceReuse(previous);
        RemoveChild(current);
        current.QueueFree();
        CardControl next = CardControl.CreateReconciled(card, CardDisplaySize.Board,
            InterfaceScale.Standard, art, reuse);
        AddChild(next);
        return next;
    }

    private static Control Face(CardControl card) => card.GetNode<Control>("CardSurface/CardFace");

    private sealed class EmptyArt : ICardArtProvider
    {
        public Texture2D? Find(string faceId) => null;
    }
}

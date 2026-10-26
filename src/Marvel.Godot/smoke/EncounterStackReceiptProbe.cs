using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Synthetic native verification of receipt impact on an exposed encounter stack header.</summary>
public sealed partial class EncounterStackReceiptProbe : Node
{
    private CardControl? denseFirst;
    private CardControl? denseSecond;
    private BoardCardPresentation? densePresentation;

    public void PrepareDense()
    {
        var surface = new Control { Size = new Vector2(500, 500) };
        AddChild(surface);
        densePresentation = new BoardCardPresentation(10, 1, false, "First environment", "", "ENVIRONMENT", "", [])
            { Counters = [new("Infamy", "2")] };
        var second = new BoardCardPresentation(11, 1, false, "Second scheme", "", "SIDE SCHEME", "", []);
        var cards = SchemeStackCollection.Render(surface, new Rect2(10, 10, 180, 300),
            [densePresentation, second], new BoardRenderResult(), new SpatialCardFactory(InterfaceScale.Standard, null, null));
        denseFirst = cards[0];
        denseSecond = cards[1];
    }

    public void GrowReceipt()
    {
        EventActivityCardRendering.Apply(denseFirst!, densePresentation! with
        {
            Counters = [new("Infamy", "1"), new("Madness", "2"), new("Synthetic public counter", "3"),
                new("Second public counter", "4"), new("Third public counter", "5"), new("Fourth public counter", "6")]
        });
    }

    public string DenseState()
    {
        var first = denseFirst!.GetNode<Control>("EncounterStackOverlay/SchemeStackHeader");
        var second = denseSecond!.GetNode<Control>("EncounterStackOverlay/SchemeStackHeader");
        return $"first={first.GetGlobalRect()} second={second.GetGlobalRect()} overlap={first.GetGlobalRect().Intersects(second.GetGlobalRect())}";
    }

    public bool DenseHeadersOverlap()
    {
        var first = denseFirst!.GetNode<Control>("EncounterStackOverlay/SchemeStackHeader");
        var second = denseSecond!.GetNode<Control>("EncounterStackOverlay/SchemeStackHeader");
        return first.GetGlobalRect().Intersects(second.GetGlobalRect());
    }

    public float DenseSecondTop() => denseSecond!.Position.Y;

    public void ShrinkReceipt() => EventActivityCardRendering.Apply(denseFirst!, densePresentation!);

    public string VerifyReceipt()
    {
        var before = new BoardCardPresentation(7, 1, false, "Synthetic environment", "", "ENVIRONMENT", "", [])
            { Counters = [new("Infamy", "2")] };
        CardControl card = CardControl.Create(before);
        AddChild(card);
        var overlay = new Control { Name = "EncounterStackOverlay" };
        card.AddChild(overlay);
        overlay.AddChild(EncounterStackHeader.Create(before, card.CustomMinimumSize.X));
        EventActivityCardRendering.Apply(card, before with
            { Title = "Changed environment", Counters = [new("Infamy", "0"), new("Madness", "2")] });
        string title = ((Label)card.FindChild("StackTitle", true, false)).Text;
        string counters = ((Label)card.FindChild("StackStatuses", true, false)).Text;
        card.QueueFree();
        return title == "Changed environment" && counters == "0 Infamy · 2 Madness"
            ? "" : "Receipt impact left the encounter stack title or quantities stale.";
    }
}

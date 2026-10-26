using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Synthetic native verification of receipt impact on an exposed encounter stack header.</summary>
public sealed partial class EncounterStackReceiptProbe : Node
{
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

using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps progressive stages off the table while making them browsable in the inspector.</summary>
internal sealed class CardInspectorStageNavigation
{
    private readonly Main main;
    private IReadOnlyList<BoardCardPresentation> sequence = [];
    private int index = -1;
    private Control? source;

    internal CardInspectorStageNavigation(Main main) => this.main = main;

    internal bool IsVisible => sequence.Count > 1 && index >= 0;

    internal void Configure(
        BoardCardPresentation card,
        Control? inspectedFrom,
        IReadOnlyList<BoardCardPresentation> cards)
    {
        HBoxContainer header = main.cardInspectorTitle.GetParent<HBoxContainer>();
        RemoveButtons(header);
        sequence = cards;
        index = cards.ToList().IndexOf(card);
        source = inspectedFrom;
        header.Visible = IsVisible;
        main.cardInspectorClose.Visible = IsVisible;
        if (!IsVisible)
        {
            return;
        }

        main.cardInspectorTitle.Text = $"STAGE {index + 1} OF {sequence.Count}";
        Button previous = Button("PreviousStage", "‹", "previous", index == 0, -1);
        Button next = Button("NextStage", "›", "next", index == sequence.Count - 1, 1);
        header.AddChild(previous);
        header.MoveChild(previous, 0);
        header.AddChild(next);
        header.MoveChild(next, 2);
    }

    internal bool Route(InputEvent input)
    {
        if (!main.cardInspector.Visible || !main.cardInspectorPinned
            || input is not InputEventKey { Pressed: true, Echo: false } key
            || key.Keycode is not (Key.Left or Key.Right))
        {
            return false;
        }

        Navigate(key.Keycode == Key.Left ? -1 : 1);
        return true;
    }

    private Button Button(string name, string text, string direction, bool disabled, int offset)
    {
        var button = new Button
        {
            Name = name,
            Text = text,
            Disabled = disabled,
            TooltipText = $"Inspect {direction} stage ({(offset < 0 ? "Left" : "Right")} arrow)",
        };
        button.Pressed += () => Navigate(offset);
        return button;
    }

    private void Navigate(int offset)
    {
        int next = index + offset;
        if (next >= 0 && next < sequence.Count)
        {
            main.ShowCardInspector(sequence[next], source, pinned: true);
        }
    }

    private static void RemoveButtons(HBoxContainer header)
    {
        foreach (Node child in header.GetChildren().Where(child => child.Name.ToString()
                     is "PreviousStage" or "NextStage"))
        {
            header.RemoveChild(child);
            child.QueueFree();
        }
    }
}

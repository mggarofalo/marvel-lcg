using Godot;
using Marvel.Protocol;

namespace Marvel.Godot;

/// <summary>Selects one server-offered recipe through mode and named component choices.</summary>
internal sealed class ScenarioModeControls(Main main)
{
    private readonly List<OptionButton> variants = [];
    private readonly List<Control> labels = [];
    private string[] modes = [];
    private string? displayedMode;

    internal IReadOnlyList<OptionButton> Variants => variants;

    internal ScenarioSetupChoice Selected
    {
        get
        {
            string mode = modes[Math.Max(0, main.mode.Selected)];
            var matching = main.visibleModes.Where(choice => choice.Mode == mode);
            return matching.First(choice => mode != "custom" || choice.Variants.Select(item => item.Value)
                .SequenceEqual(variants.Select(item => item.GetItemText(item.Selected)), StringComparer.Ordinal));
        }
    }

    internal void Populate()
    {
        modes = main.visibleModes.Select(choice => choice.Mode).Distinct(StringComparer.Ordinal).ToArray();
        main.mode.Clear();
        foreach (string mode in modes) main.mode.AddItem(Label(mode));
        main.mode.Select(0);
        displayedMode = null;
        Refresh();
    }

    internal void Refresh()
    {
        if (modes.Length == 0) return;
        string mode = modes[Math.Max(0, main.mode.Selected)];
        if (mode == displayedMode) return;
        displayedMode = mode;
        foreach (Control control in labels.Concat<Control>(variants))
        {
            if (main.setupGrid.GetNodeOrNull<Control>(control.Name + "Choices") is { } row)
            { main.setupGrid.RemoveChild(row); row.QueueFree(); }
            main.setupGrid.RemoveChild(control);
            control.QueueFree();
        }
        labels.Clear(); variants.Clear();
        if (mode != "custom") return;
        var offered = main.visibleModes.Where(choice => choice.Mode == mode).ToArray();
        for (int index = 0; index < offered[0].Variants.Count; index++)
        {
            var label = new Label { Name = "ScenarioVariantLabel" + index, Text = offered[0].Variants[index].Name };
            var option = new OptionButton { Name = "ScenarioVariant" + index };
            foreach (string value in offered.Select(choice => choice.Variants[index].Value).Distinct(StringComparer.Ordinal))
                option.AddItem(value);
            option.Select(0);
            option.ItemSelected += _ => main.setupController.RefreshBriefing();
            main.setupGrid.AddChild(label); main.setupGrid.AddChild(option);
            main.setupGrid.MoveChild(label, main.modular.GetIndex());
            main.setupGrid.MoveChild(option, label.GetIndex() + 1);
            labels.Add(label); variants.Add(option);
        }
    }

    internal static string Label(string mode) => mode switch
    { "standard" => "Standard", "expert" => "Expert", "custom" => "Custom", "extreme" => "Extreme", _ => mode };
}

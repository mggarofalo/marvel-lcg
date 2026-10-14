using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Prepares source-control construction during setup loading on the native UI thread.</summary>
internal static class SourceTableauWarmup
{
    private static bool prepared;

    internal static void Prepare(Main main)
    {
        if (prepared) return;
        prepared = true;
        // Presentation policy: an invisible blank specimen initializes native
        // controls and packaged fonts. It has no engine or draft and is retired
        // immediately; no card, copy or arrangement registry receives it.
        var host = new Control
        {
            Name = "SourceControlsPreparing",
            Visible = false, ProcessMode = Node.ProcessModeEnum.Disabled,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        ClientThemeInstallation.Apply(host, main.interfaceScale);
        main.AddChild(host);
        var scroll = new ScrollContainer();
        var grid = new GridContainer { Columns = 1 };
        host.AddChild(scroll);
        scroll.AddChild(grid);
        var result = new BoardRenderResult { IsCurrent = () => false };
        var gestures = new SourceGridGesture();
        host.AddChild(gestures);
        gestures.Configure(grid, scroll, -1, result);
        var blank = new BoardCardPresentation(null, 1, false, "", "", "SUPPORT", "", []);
        CardControl card = SourceTableauTile.CreateCard(blank, 160, main.interfaceScale, compact: true);
        var slot = new Control { CustomMinimumSize = card.CustomMinimumSize };
        grid.AddChild(slot);
        slot.AddChild(card);
        gestures.Track(card, slot);
        SourceTableauViewport.Bind(scroll, grid);
        host.QueueFree();
    }
}

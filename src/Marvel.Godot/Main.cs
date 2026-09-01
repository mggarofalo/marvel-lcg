using Godot;
using Marvel.Server;

namespace Marvel.Godot;

/// <summary>The desktop client's composition boundary and root scene.</summary>
public sealed partial class Main : Control
{
    private Label description = null!;
    private Label status = null!;

    /// <summary>The initial visibility-safe game view, retained for board rendering.</summary>
    public EngineResponse? OpenedGame { get; private set; }

    /// <inheritdoc />
    public override void _Ready()
    {
        GetWindow().MinSize = new Vector2I(960, 600);
        description = GetNode<Label>("Margin/Shell/Content/Description");
        status = GetNode<Label>("Margin/Shell/Content/Status/Text");
        _ = OpenDevelopmentGameAsync();
    }

    private async Task OpenDevelopmentGameAsync()
    {
        try
        {
            string dataRoot = ProjectSettings.GlobalizePath("res://../..");
            ClientStartupResult startup = await LocalGameClient.OpenLocalAsync(dataRoot);
            if (!startup.Succeeded)
            {
                ShowFailure(startup.Error!);
                return;
            }

            OpenedGame = startup.Response!;
            description.Text =
                $"Spider-Man versus Rhino · {OpenedGame.World!.Areas.Count} visible areas · "
                + $"{OpenedGame.Events.Count} setup events";
            status.Text =
                $"GAME OPEN  ·  {OpenedGame.World.Outcome.ToString().ToUpperInvariant()}  ·  "
                + OpenedGame.Prompt!.Asking.ToString().ToUpperInvariant();
        }
        catch (Exception)
        {
            ShowFailure(new ClientStartupError(
                "startup_failed",
                "The local game could not be displayed. Try opening it again."));
        }
    }

    private void ShowFailure(ClientStartupError failure)
    {
        description.Text = failure.Message;
        status.Text = $"GAME UNAVAILABLE  ·  {failure.Code.ToUpperInvariant()}";
    }
}

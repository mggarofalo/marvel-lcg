using Godot;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Drains admitted embedded requests before normal window close without blocking the main loop.</summary>
internal sealed class MainShutdownController(Main main)
{
    private bool closing;

    internal void Attach()
    {
        SceneTree tree = main.GetTree();
        tree.AutoAcceptQuit = false;
        tree.Root.CloseRequested += OnCloseRequested;
        main.TreeExiting += Detach;
    }

    private void Detach()
    {
        main.GetTree().Root.CloseRequested -= OnCloseRequested;
        main.TreeExiting -= Detach;
    }

    private async void OnCloseRequested()
    {
        if (closing) return;
        closing = true;
        SceneTree tree = main.GetTree();
        main.lifecycle.Detach();
        main.SetSetupControlsEnabled(false);
        main.status.Text = "Finishing the current request…";
        main.statusPanel.Visible = true;
        await ClientComposition.ShutdownAsync();
        // The captured Godot context resumes here before scene teardown. The existing
        // root TreeExited finalizer drain still runs after Quit releases the scene.
        tree.Quit();
    }
}

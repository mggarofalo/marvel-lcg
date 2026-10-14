using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Owns temporary setup discovery connections and their disposal.</summary>
internal sealed class SetupDiscoveryController(Main main, MainSetupController controller)
{
    internal async Task LoadAsync()
    {
        long generation = main.lifecycle.BeginEntry();
        string requestedEndpoint = main.endpoint.Text;
        main.setupChoices = null;
        main.reloadSetup.Disabled = true;
        main.start.Disabled = true;
        main.SetAssignmentControlsEnabled(false);
        main.status.Text = "Loading setup choices…";
        main.statusPanel.Visible = true;
        try
        {
            LocalClientConnection connection = ClientComposition.Connect(
                DesktopDataRoot.Current(),
                requestedEndpoint);
            if (!connection.Succeeded)
            {
                controller.ApplySetupFailure(generation, requestedEndpoint, connection.Error!);
                return;
            }

            using LocalGameClient candidate = connection.Client!;
            ClientSetupResult setup = await candidate.ReadSetupAsync();
            if (!controller.IsCurrentSetupLoad(generation, requestedEndpoint))
            {
                return;
            }

            if (!setup.Succeeded)
            {
                controller.ApplySetupFailure(generation, requestedEndpoint, setup.Error!);
                return;
            }

            main.setupChoices = setup.Choices;
            main.lifecycle.FinishEntry(generation);
            controller.PopulateSetupChoices();
            controller.RefreshEntryAvailability();
            main.reloadSetup.Disabled = false;
            main.title.Text = "Assemble the table.";
            main.description.Text = string.Empty;
            main.status.Text = string.Empty;
            main.statusPanel.Visible = false;
            main.statusPanel.ThemeTypeVariation = GodotThemeVariations.StatusPanel;
            main.status.ThemeTypeVariation = GodotThemeVariations.StatusText;
        }
        catch (Exception)
        {
            controller.ApplySetupFailure(generation, requestedEndpoint, new ClientStartupError(
                "startup_failed",
                "The setup options could not be loaded. Check the endpoint and try again."));
        }
    }
}

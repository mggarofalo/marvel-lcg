using Godot;
namespace Marvel.Godot;

/// <summary>Local name, file and destructive-choice dialogs for campaign management.</summary>
internal static class CampaignManagementDialogs
{
    internal static void Rename(CampaignControls ui, string name, Action<string> accept)
    {
        var dialog = new ConfirmationDialog { Name = "CampaignRenameDialog", Title = "Rename campaign", OkButtonText = "Rename",
            MinSize = new(480, 140) };
        var field = new LineEdit { Name = "CampaignName", Text = name, MaxLength = 128 };
        dialog.AddChild(field);
        ui.Window.AddChild(dialog);
        dialog.Confirmed += () => { accept(field.Text); dialog.QueueFree(); };
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered();
        field.GrabFocus(); field.SelectAll();
    }

    internal static void Delete(CampaignControls ui, string name, Action accept)
    {
        var dialog = new ConfirmationDialog { Name = "CampaignDeleteDialog", Title = "Delete campaign permanently?",
            DialogText = $"Delete '{name}' and every saved game in it?\nThis cannot be undone. Export a backup first if you want to keep it.",
            OkButtonText = "Delete permanently", MinSize = new(620, 180) };
        ui.Window.AddChild(dialog);
        dialog.Confirmed += () => { accept(); dialog.QueueFree(); };
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered();
        dialog.GetCancelButton().GrabFocus();
    }

    internal static void Import(CampaignControls ui, Action<string> accept) =>
        File(ui, false, path =>
        {
            if (new System.IO.FileInfo(path).Length > 3 * 1024 * 1024)
                throw new System.IO.IOException("Campaign file exceeds the 3 MiB transfer limit.");
            accept(System.IO.File.ReadAllText(path));
        });

    internal static void Export(CampaignControls ui, string value) =>
        File(ui, true, path => { System.IO.File.WriteAllText(path, value); ui.Status.Text = "Campaign exported."; });

    private static void File(CampaignControls ui, bool save, Action<string> selected)
    {
        var dialog = new FileDialog { Title = save ? "Export campaign backup" : "Import an independent campaign copy",
            FileMode = save ? FileDialog.FileModeEnum.SaveFile : FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem, Filters = ["*.marvelcampaign ; Marvel campaign backup"],
            CurrentFile = save ? "campaign.marvelcampaign" : "", Size = new(800, 600) };
        ui.Window.AddChild(dialog);
        dialog.FileSelected += path =>
        {
            try { selected(path); }
            catch (Exception) { ui.Status.Text = "The campaign file could not be read or written. Existing campaigns are preserved."; }
            dialog.QueueFree();
        };
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered();
    }
}

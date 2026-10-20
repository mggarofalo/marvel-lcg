using Godot;
using Marvel.Client;
using Marvel.Desktop;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>Owns native campaign navigation, retained commands, and local resume credentials.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The Main scene owns controls and disposes connections on TreeExiting.")]
internal sealed class CampaignController
{
    private readonly Main main;
    private readonly Button open = new() { Text = "Campaigns", Visible = false,
        SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
    private readonly CampaignControls ui;
    private CampaignConnectionStore? store;
    private IReadOnlyList<CampaignConnection> saved = [];
    private CampaignConnection? connection;
    private CampaignDescriptor? campaign;
    private LocalGameClient? client;
    private DeckCatalog? catalog;
    private bool busy;
    private bool disposed;

    internal CampaignController(Main main)
    {
        this.main = main;
        ui = new(main);
        main.setupPanel.GetParent().AddChild(open);
        main.setupPanel.GetParent().MoveChild(open, main.setupPanel.GetIndex());
        open.Pressed += Show;
        ui.New.Pressed += New;
        ui.Refresh.Pressed += () => Choose(new("sync"));
        ui.ShowArchived.Toggled += _ => ReloadSaved();
        ui.Import.Pressed += () => CampaignManagementDialogs.Import(ui, Import);
        ui.Rename.Pressed += () => CampaignManagementDialogs.Rename(ui, connection!.Label,
            name => Choose(new("rename") { Name = name }));
        ui.Archive.Pressed += () => Choose(new(campaign!.Archived ? "restore" : "archive"));
        ui.Export.Pressed += () => Choose(new("export"));
        ui.Delete.Pressed += () => CampaignManagementDialogs.Delete(ui, connection!.Label, () => Choose(new("delete")));
        ui.Retry.Pressed += Recover;
        var timer = new global::Godot.Timer { WaitTime = 0.5, Autostart = true };
        timer.Timeout += () => open.Visible = main.setupChoices?.Campaigns.Count > 0 || saved.Count > 0;
        main.AddChild(timer);
        main.TreeExiting += () => { disposed = true; client?.Dispose(); store?.Dispose(); };
    }

    private void Show()
    {
        if (busy) return;
        try
        {
            store ??= new CampaignConnectionStore(System.Environment.GetEnvironmentVariable("MARVEL_CAMPAIGN_ACCESS_DIRECTORY")
                ?? ProjectSettings.GlobalizePath("user://campaign-access"));
            saved = store.Load();
            ReloadSaved();
            ui.Window.PopupCentered();
            if (connection is null)
            {
                if (saved.Count == 0) New();
                else { ui.Selected = 0; ResumeSaved(); }
            }
            else Choose(new("sync"));
        }
        catch (Exception) { ui.Status.Text = "Campaign access files could not be opened. Existing saves have been preserved."; }
    }

    private async Task<bool> Connect(string endpoint)
    {
        busy = true;
        RefreshRecovery();
        try
        {
            client?.Dispose();
            var connected = ClientComposition.Connect(DesktopDataRoot.Current(), endpoint);
            if (!connected.Succeeded) { ui.Status.Text = connected.Error!.Message; return false; }
            client = connected.Client!;
            var result = await client.ReadDecksAsync();
            if (disposed) return false;
            if (!result.Succeeded) { ui.Status.Text = result.Error!.Message; return false; }
            catalog = await DeckResearchCatalog.LoadAsync(client, result.Decks!.Catalog!);
            return !disposed;
        }
        catch (Exception)
        {
            if (!disposed) ui.Status.Text = "The campaign service could not be reached. Saved campaigns are preserved.";
            return false;
        }
        finally { busy = false; if (!disposed) RefreshRecovery(); }
    }

    private async void New()
    {
        if (busy || connection?.Pending is not null) return;
        connection = null; campaign = null;
        ui.Management.Visible = false;
        ReloadSaved();
        if (!await Connect(main.endpoint.Text) || catalog is null) return;
        string? product = main.setupChoices?.Campaigns is { Count: > 0 } products ? products[0] : null;
        if (product is null) { ui.Status.Text = "This content installation has no complete campaign available."; return; }
        CampaignCreationForm.Render(ui, catalog, product, Create);
    }

    private async void Create(string label, CampaignQuery query)
    {
        if (busy) return;
        connection = new(Guid.NewGuid().ToString("N"), main.endpoint.Text,
            string.IsNullOrWhiteSpace(label) ? "Rise of Red Skull" : label, null, null, null, null);
        campaign = null;
        var request = new EngineRequest(EngineProtocol.Version, Guid.NewGuid().ToString("N"), EngineProtocol.Campaign,
            connection.Label, Entry: EntryClaim.Create()) { Campaign = query };
        await Send(request);
    }

    private async void ResumeSaved()
    {
        if (busy || connection?.Pending is not null || ui.Selected < 0 || ui.Selected >= saved.Count) return;
        connection = saved[ui.Selected];
        campaign = null;
        if (!await Connect(connection.Endpoint)) return;
        if (connection.Pending is not null) { ui.Status.Text = "A choice was awaiting confirmation. Recover that exact choice before continuing."; RefreshRecovery(); }
        else Choose(new("sync"));
    }

    private async void Import(string transfer)
    {
        if (busy || connection?.Pending is not null) return;
        if (!await Connect(main.endpoint.Text)) return;
        connection = new(Guid.NewGuid().ToString("N"), main.endpoint.Text, "Imported campaign", null, null, null, null);
        campaign = null;
        await Send(new(EngineProtocol.Version, Guid.NewGuid().ToString("N"), EngineProtocol.Campaign,
            "Imported campaign", Entry: EntryClaim.Create()) { Campaign = new("import") { Transfer = transfer } });
    }

    private async void Choose(CampaignQuery query)
    {
        if (busy || connection is null || client is null) return;
        if (connection.Pending is not null) { RefreshRecovery(); return; }
        var request = new EngineRequest(EngineProtocol.Version, Guid.NewGuid().ToString("N"), EngineProtocol.Campaign,
            connection.CampaignId!, Capability: connection.Capability, ExpectedRevision: campaign?.Revision,
            Entry: query.Action == "start" ? EntryClaim.Create() : null) { Campaign = query };
        await Send(request);
    }

    private async void Recover()
    {
        if (!busy && connection?.Pending is { } pending) await Send(pending);
    }

    private async Task Send(EngineRequest request)
    {
        if (connection is null || client is null || store is null) return;
        busy = true; RefreshRecovery();
        ui.Status.Text = "Saving campaign choice…";
        try
        {
            connection = connection with { Pending = request };
            store.Save(connection);
            var result = await client.ExchangeCampaignAsync(request);
            if (disposed) return;
            if (result.Succeeded)
            {
                await Accept(result, request);
            }
            else
            {
                Reject(result);
            }
        }
        catch (Exception) { if (!disposed) ui.Status.Text = "The campaign result could not be saved locally. Recover this exact choice before continuing."; }
        finally { busy = false; if (!disposed) RefreshRecovery(); }
    }

    private void Reject(ClientCampaignResult result)
    {
        connection = connection! with { Pending = result.Pending };
        if (connection.CampaignId is null && result.Pending is null)
        {
            store!.Delete(connection.LocalId);
            connection = null; campaign = null; saved = store.Load();
            ReloadSaved(); ui.Clear(); ui.Management.Visible = false;
            ui.Status.Text = result.Error!.Message;
            return;
        }
        store!.Save(connection);
        if (result.Response?.Campaign is { } latest) campaign = latest;
        Render();
        ui.Status.Text = result.Error!.Message;
    }

    private async Task Accept(ClientCampaignResult result, EngineRequest request)
    {
        var response = result.Response!;
        if (response.CampaignDeleted)
        {
            AcceptDeletion();
            return;
        }
        var proposed = connection! with { CampaignId = response.Campaign!.Id,
            Capability = request.Campaign!.Action is "create" or "import" ? response.Capability : connection!.Capability,
            Label = response.Campaign.Name, Snapshot = response.Campaign,
            Game = response.CampaignGame is { } imported ? new(imported.GameId, imported.Capability)
                : result.Entry?.Session ?? connection!.Game, Pending = null };
        store!.Save(proposed);
        connection = proposed;
        campaign = response.Campaign;
        saved = store.Load(); ReloadSaved(); Render();
        ui.Status.Text = "Saved";
        if (response.CampaignTransfer is { } transfer) CampaignManagementDialogs.Export(ui, transfer);
        if (result.Entry is not null) await OpenTable();
    }

    private void AcceptDeletion()
    {
        if (main.CurrentGame?.GameId == connection!.Game?.GameId) CloseDeletedTable();
        store!.Delete(connection!.LocalId);
        connection = null; campaign = null; saved = store.Load();
        ReloadSaved(); ui.Clear(); ui.Management.Visible = false;
        ui.Status.Text = "Campaign and its saved games permanently deleted.";
    }

    private void CloseDeletedTable()
    {
        main.lifecycle.Detach();
        main.boardController.ResetForSession();
        main.decisions.ClearDecision();
        main.board.Visible = false;
        main.cardInspector.Visible = false;
        main.setupPanel.Visible = true;
        main.SetSetupControlsEnabled(true);
    }

    private void Render()
    {
        if (campaign is null || catalog is null) return;
        ui.Management.Visible = true;
        ui.Archive.Text = campaign.Archived ? "Restore" : "Archive";
        CampaignPresentation.Render(ui, campaign, catalog, Choose, seat =>
            CampaignDeckEditor.Open(main, ui, connection!, campaign, catalog, seat, Choose), ResumeTable);
        ui.SetBusy(busy);
    }

    private async void ResumeTable()
    {
        if (busy) return;
        busy = true; RefreshRecovery();
        try { await OpenTable(); }
        catch (Exception) { if (!disposed) ui.Status.Text = "The campaign table could not be opened. Its saved attempt is preserved."; }
        finally { busy = false; if (!disposed) RefreshRecovery(); }
    }

    private async Task OpenTable()
    {
        if (connection is null || campaign is null) return;
        string? error = await CampaignTableEntry.OpenAsync(main, connection, campaign);
        if (disposed) return;
        if (error is null) ui.Window.Hide(); else ui.Status.Text = error;
    }

    private void RefreshRecovery()
    {
        ui.SetBusy(busy);
        ui.Retry.Visible = connection?.Pending is not null;
        ui.Retry.Disabled = busy;
        ui.New.Disabled = ui.Refresh.Disabled = busy || connection?.Pending is not null;
        ui.Import.Disabled = ui.Rename.Disabled = ui.Archive.Disabled = ui.Export.Disabled = ui.Delete.Disabled
            = busy || connection?.Pending is not null;
    }

    private void ReloadSaved()
    {
        CampaignLibrary.Render(ui, saved, connection, catalog, index => { ui.Selected = index; ResumeSaved(); });
    }
}

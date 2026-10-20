using Godot;
using Marvel.Client;
using Marvel.Desktop;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>Synthetic tracker presentation only; this probe never creates or advances a campaign.</summary>
public sealed partial class CampaignTrackerNativeProbe : Node
{
    public bool Prepared { get; private set; }
    public string Failure { get; private set; } = "";
    public string LastChoice { get; private set; } = "";

    public async void Show(Node scene, string responseJson)
    {
        try
        {
            var main = (Main)scene;
            var connected = ClientComposition.Connect(DesktopDataRoot.Current(), "");
            using var client = connected.Client ?? throw new InvalidOperationException(connected.Error?.Message);
            var admitted = await client.ReadDecksAsync();
            var catalog = await DeckResearchCatalog.LoadAsync(client, admitted.Decks!.Catalog!);
            var response = EngineJson.ReadResponse(System.Text.Encoding.UTF8.GetBytes(responseJson));
            var controls = new CampaignControls(main);
            controls.Window.Name = "SyntheticCampaignTracker";
            controls.Window.Title = "Synthetic tracker component — no campaign is running";
            CampaignPresentation.Render(controls, response.Campaign!, catalog,
                choice => LastChoice = choice.Action, seat => LastChoice = "edit:" + seat,
                () => LastChoice = "resume");
            controls.Status.Text = "Synthetic component evidence. Printed cards are real catalog entries; campaign state is a fixture.";
            controls.Window.PopupCentered();
            Prepared = true;
        }
        catch (Exception error) { Failure = error.Message; }
    }
}

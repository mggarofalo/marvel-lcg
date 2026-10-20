using Godot;
using Marvel.Protocol;
using Marvel.Client;
namespace Marvel.Godot;

/// <summary>Exercises a real engine-produced prompt snapshot without submitting into a live session.</summary>
public sealed partial class DecisionDialogNativeProbe : Node, IEngineTransport
{
    private Main main = null!;
    public int Submissions { get; private set; }
    public int Selected => main.decisions.composer?.Selected?.Id ?? -1;
    public int Submitted { get; private set; } = -1;

    public void Show(Node scene, string responseJson)
    {
        main = (Main)scene;
        var response = EngineJson.ReadResponse(System.Text.Encoding.UTF8.GetBytes(responseJson));
        main.lifecycle.Enter(main.lifecycle.BeginEntry(), new LocalGameClient(this),
            new(new(response.GameId, "fixture"), response, [], null));
        main.decisions.Submitted -= main.OnDecisionSubmitted;
        main.boardController.RenderGame(response, resetEvents: true, operation: EngineProtocol.Sync);
        main.setupPanel.Visible = false;
        main.board.Visible = true;
        main.decisions.Submitted += decision => { Submitted = decision.Affordance; Submissions++; };
        main.ApplyResponsivePlayLayout();
    }

    public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("A decision snapshot cannot execute game commands.");
}

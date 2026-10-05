using Marvel.Godot;
using Marvel.Server;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ResponseReceiptTests
{
    [Fact]
    public void CommittedNestedResultIsVisibleBeforeItsParentActionCompletes()
    {
        // Synthetic authorized response: an ability draws while its enclosing attack is open.
        var draw = new EventPresentation("Spider-Man drew one card.", "Spider-Sense", [], EventMotionKind.Move);
        var batch = new EventBatchPresentation([draw], [draw], [draw]);
        var response = new EngineResponse(EngineProtocol.Version, "draw", "game", null, null, [])
        {
            History = new HistoryDescriptor(2, [], [], [], ActionOpen: true),
        };

        Assert.Equal([draw], BoardResponsePresentation.Highlights(response, batch));
    }

    [Fact]
    public void CompletedActionReceiptRetainsEffectAndNamedPaymentSources()
    {
        // Synthetic authorized response: history names the commitment; events describe its result.
        var damage = new EventPresentation("Rhino health: 14 → 6 (8 damage).", "Swinging Web Kick", [9], EventMotionKind.Damage);
        var batch = new EventBatchPresentation([damage], [damage], [damage]);
        var action = new HistoryEntryDescriptor(2,
            "Spider-Man played Swinging Web Kick, generating resources from Web-Shooter and Energy.", []);
        var response = new EngineResponse(EngineProtocol.Version, "target", "game", null, null, [])
        {
            History = new HistoryDescriptor(3, [], [], [action], ActionOpen: false),
        };

        IReadOnlyList<EventPresentation> receipt = BoardResponsePresentation.Highlights(response, batch,
            new DecisionReceiptContext(action.Summary, []));

        Assert.Equal(action.Summary, receipt[0].Summary);
        Assert.Equal(damage, receipt[1]);
    }

    [Fact]
    public void CardReturnedByAnAbilityRemainsVisibleAlongsideItsPlayCommitment()
    {
        var returned = new EventPresentation("Swinging Web Kick returned to Spider-Man's hand.",
            "Black Cat", [5], EventMotionKind.HandGain);
        var batch = new EventBatchPresentation([returned], [returned], [returned]);
        var play = new HistoryEntryDescriptor(2, "Spider-Man played Black Cat.", []);
        var response = new EngineResponse(EngineProtocol.Version, "play", "game", null, null, [])
        {
            History = new HistoryDescriptor(3, [], [], [play], ActionOpen: false),
        };
        IReadOnlyList<EventPresentation> receipt = BoardResponsePresentation.Highlights(response, batch,
            new DecisionReceiptContext(play.Summary, []));
        Assert.Equal(play.Summary, receipt[0].Summary);
        Assert.Equal(returned, receipt[1]);
    }

    [Fact]
    public void CompletionReceiptRetainsTheDefenderAlongsidePhaseHistory()
    {
        // Synthetic authorized completion: no health delta is needed to name
        // the established defender; phase history is a separate enclosing task.
        var attack = new EventPresentation("Rhino's attack on Spider-Man ended. Spider-Man defended.",
            "Attack", [9, 7], EventMotionKind.Attack);
        var batch = new EventBatchPresentation([attack], [attack], [attack]);
        var phase = new HistoryEntryDescriptor(2, "Spider-Man kept their hand at phase end.", []);
        var response = new EngineResponse(EngineProtocol.Version, "defend", "game", null, null, [])
        {
            History = new HistoryDescriptor(3, [], [], [phase], ActionOpen: false),
        };

        IReadOnlyList<EventPresentation> receipt = BoardResponsePresentation.Highlights(response, batch);

        Assert.Equal(attack, Assert.Single(receipt));
        Assert.DoesNotContain(receipt, entry => entry.Summary == phase.Summary);
    }
}

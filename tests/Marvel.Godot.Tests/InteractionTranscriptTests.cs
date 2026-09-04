using Marvel.Client;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class InteractionTranscriptTests
{
    [Fact]
    public void ExportKeepsAuthorizedGameContentAndRemovesOperationalSecrets()
    {
        var runtime = new RuntimeIdentity(
            "1.2.3", "commit", "replay", "rng", "digest", 11, 1,
            "cards", "setup", "abilities");
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "Player 1", false)],
            [new AreaDescriptor(
                1, "HandsArea", 0, -1,
                [new CardDescriptor(
                    42, CardBack.Player, true, true, -1,
                    new CardFaceDescriptor(
                        "01005", "Webbed Up", "", Marvel.Rules.State.CardKind.Upgrade,
                        new Dictionary<string, long>()))],
                [])],
            [],
            Outcome.Unfinished);
        var response = new EngineResponse(
            11, "machine-request", "private-deployment-label", "bearer-secret",
            null, [], world,
            Invitations: [new SeatInvitation(1, "invitation-secret")], Revision: 7);
        var transcript = new InteractionTranscript();

        transcript.Reset(12345, runtime);
        transcript.RecordDecision(6, new EngineDecision(9, [42]));
        transcript.RecordResponse(EngineProtocol.Resolve, response);
        string report = transcript.Export();

        Assert.Contains("Webbed Up", report, StringComparison.Ordinal);
        Assert.Contains("01005", report, StringComparison.Ordinal);
        Assert.Contains("12345", report, StringComparison.Ordinal);
        Assert.Contains("commit", report, StringComparison.Ordinal);
        Assert.Contains("42", report, StringComparison.Ordinal);
        Assert.DoesNotContain("bearer-secret", report, StringComparison.Ordinal);
        Assert.DoesNotContain("invitation-secret", report, StringComparison.Ordinal);
        Assert.DoesNotContain("machine-request", report, StringComparison.Ordinal);
        Assert.DoesNotContain("private-deployment-label", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetDoesNotMixTwoTables()
    {
        var transcript = new InteractionTranscript();
        transcript.Reset(1, runtime: null);
        transcript.RecordDecision(0, new EngineDecision(1, []));

        transcript.Reset(2, runtime: null);

        Assert.Empty(transcript.Entries);
        Assert.Equal((uint)2, transcript.Seed);
    }

    [Fact]
    public void ExportUsesActionHistoryWithoutRawEventCauses()
    {
        var response = new EngineResponse(
            EngineProtocol.Version,
            "request",
            "game",
            "secret",
            Prompt: null,
            Events:
            [
                new CardsMoved(
                    AreaRef.Player("HandsArea", 0),
                    AreaRef.Player("DiscardPile", 0),
                    [new Landing(7, 0)])
                {
                    Trigger = CardPlay.Verb,
                    Verb = "Discard",
                },
            ],
            History: new HistoryDescriptor(
                1,
                [0],
                [],
                [new HistoryEntryDescriptor(
                    0,
                    "Spider-Man played Black Cat, generating resources from First Aid.",
                    [])],
                ActionOpen: false));
        var transcript = new InteractionTranscript();

        transcript.RecordResponse(EngineProtocol.Resolve, response);
        string report = transcript.Export();

        Assert.Contains("Spider-Man played Black Cat", report, StringComparison.Ordinal);
        Assert.DoesNotContain("trigger", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CardsMoved", report, StringComparison.Ordinal);
    }
}

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
        var response = new EngineResponse(
            11, "machine-request", "private-deployment-label", "bearer-secret",
            null, [], World(),
            Invitations: [new SeatInvitation(1, "invitation-secret")], Revision: 7);
        var transcript = new InteractionTranscript();
        transcript.Reset(12345, Runtime());
        transcript.RecordDecision(6, new EngineDecision(9, [42]));
        transcript.RecordResponse(EngineProtocol.Resolve, response, []);

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
    public void ExportPreservesPolymorphicEventsAndPresentedNarrativeInResponseOrder()
    {
        var response = new EngineResponse(
            EngineProtocol.Version, "request", "game", "secret", Prompt: null,
            Events:
            [
                new CardsMoved(
                    AreaRef.Player("HandsArea", 0),
                    AreaRef.Player("DiscardPile", 0),
                    [new Landing(42, 0)])
                {
                    Trigger = CardPlay.Verb,
                    Verb = "Discard",
                },
                new FieldSet(42, "damage", 0, 1),
            ],
            World: World(), Revision: 7);
        var transcript = new InteractionTranscript();
        EventPresentation[] displayed =
        [
            new("Spider-Man played Webbed Up, generating resources from Scientist.",
                "Action", [], EventMotionKind.State),
        ];
        transcript.RecordResponse(EngineProtocol.Resolve, response, displayed);

        string report = transcript.Export();
        InteractionTranscriptReport roundTrip = InteractionTranscript.Read(report);
        InteractionTranscriptResponse recorded = Assert.Single(roundTrip.Entries).Response!;

        Assert.Equal(2, roundTrip.Schema);
        Assert.Collection(
            recorded.Events!,
            moved => Assert.IsType<CardsMoved>(moved),
            changed => Assert.IsType<FieldSet>(changed));
        EventPresentation narrative = Assert.Single(recorded.Narrative!);
        Assert.Equal(displayed[0].Summary, narrative.Summary);
        Assert.Equal(displayed[0].Cause, narrative.Cause);
        Assert.Equal(displayed[0].Motion, narrative.Motion);
        Assert.Equal(displayed[0].Anchors, narrative.Anchors);
        Assert.Contains("\"kind\": \"CardsMoved\"", report, StringComparison.Ordinal);
        Assert.Contains("\"field\": \"damage\"", report, StringComparison.Ordinal);
        Assert.Contains("Scientist", narrative.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportDistinguishesKnownEmptyEventsFromLegacyMissingEvidence()
    {
        var transcript = new InteractionTranscript();
        transcript.RecordResponse(
            EngineProtocol.Sync,
            new EngineResponse(
                EngineProtocol.Version, "request", "game", "secret", null, [], World()),
            []);

        InteractionTranscriptReport current = InteractionTranscript.Read(transcript.Export());
        Assert.Empty(Assert.Single(current.Entries).Response!.Events!);
        Assert.Empty(current.Limitations);

        const string legacy = """
            {
              "format": "marvel-client-interaction",
              "schema": 1,
              "seed": 9,
              "runtime": null,
              "entries": [{
                "kind": "sync",
                "revision": 0,
                "response": {
                  "version": 13,
                  "request_id": "",
                  "game_id": "",
                  "capability": null,
                  "prompt": null,
                  "events": []
                }
              }]
            }
            """;
        InteractionTranscriptReport upgraded = InteractionTranscript.Read(legacy);

        Assert.Null(Assert.Single(upgraded.Entries).Response!.Events);
        Assert.Equal("legacy_report", upgraded.Setup.Availability);
        Assert.Equal(2, upgraded.Limitations.Count);
    }

    [Theory]
    [InlineData(ClientMutationDisposition.Rejected, true)]
    [InlineData(ClientMutationDisposition.Uncertain, false)]
    public void FailureBoundaryPreservesWhetherAResponseWasReceived(
        ClientMutationDisposition disposition,
        bool evidenceAvailable)
    {
        var transcript = new InteractionTranscript();

        transcript.RecordFailure(
            EngineProtocol.Resolve,
            4,
            new ClientStartupError("stale_decision", "The prompt changed."),
            disposition);
        InteractionTranscriptEntry entry = Assert.Single(
            InteractionTranscript.Read(transcript.Export()).Entries);

        Assert.Equal("resolve", entry.Kind);
        Assert.Equal(disposition.ToString().ToLowerInvariant(), entry.Disposition);
        Assert.Equal("stale_decision", entry.Response!.Error!.Code);
        Assert.Equal(evidenceAvailable, entry.Response.Events is not null);
    }

    [Fact]
    public void SetupEvidenceIncludesModeOrderedSeatsAndResolvedModularSets()
    {
        SetupChoices choices = new(
            [new("spider-man", "Spider-Man"), new("captain-marvel", "Captain Marvel")],
            [new("rhino-expert", "Rhino", true, ["bomb-scare"])],
            [new("bomb-scare", "Bomb Scare")],
            Runtime());
        var selection = new GameSetupSelection(
            ["captain-marvel", "spider-man"], "rhino-expert",
            ModularConfiguration.Recommended, [], "123");
        var transcript = new InteractionTranscript();
        transcript.Reset(123, choices.Runtime,
            InteractionTranscriptSetup.FromSelection(choices, selection));

        InteractionTranscriptReport report = InteractionTranscript.Read(transcript.Export());

        Assert.Equal("known", report.Setup.Availability);
        Assert.Equal("rhino-expert", report.Setup.Scenario);
        Assert.Equal("expert", report.Setup.Mode);
        Assert.Equal(["captain-marvel", "spider-man"], report.Setup.Seats);
        Assert.Equal("recommended", report.Setup.ModularSelection);
        Assert.Equal(["bomb-scare"], report.Setup.ModularSets);
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

    private static RuntimeIdentity Runtime() => new(
        "1.2.3", "commit", "replay", "rng", "digest", 11, 1,
        "cards", "setup", "abilities");

    private static WorldDescriptor World() => new(
        [new PlayerDescriptor(0, "Player 1", false)],
        [new AreaDescriptor(
            1, "HandsArea", 0, -1,
            [new CardDescriptor(
                42, CardBack.Player, true, true, -1,
                new CardFaceDescriptor(
                    "01005", "Webbed Up", "", Marvel.Rules.State.CardKind.Upgrade,
                    new Dictionary<string, long>()))],
            [])],
        [], Outcome.Unfinished);
}

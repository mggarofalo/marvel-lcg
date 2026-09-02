using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Session;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class InformationFrontierTests
{
    [Fact]
    public void HiddenInformationAndRandomnessProduceCanonicalAudiences()
    {
        var search = new Prompt(
            0,
            Question.Element,
            TimingPriority.Untimed,
            "Search",
            "Choose a card",
            Cancellable: false,
            [new Affordance(
                1,
                "Choose",
                7,
                0,
                "Choose",
                new TargetRequest([7], 1, 1, IsSearch: true))])
        {
            ExposesConcealedCandidates = true,
        };
        GameEvent[] events =
        [
            new CardsMoved(
                AreaRef.Player("PlayerDeck", 1),
                AreaRef.Player("HandsArea", 1),
                [new Landing(10, 0)]),
            new CardsFlipped([20], FaceUp: true),
            new AreaReordered(
                AreaRef.Scenario("EncounterDeck"),
                [21, 22]),
        ];

        IReadOnlyList<InformationExposure> exposures = InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 10,
            information:
            [
                new InformationSignal(InformationKind.Reveal),
                new InformationSignal(InformationKind.Search),
            ],
            events,
            search);

        Assert.Collection(
            exposures,
            exposure =>
            {
                Assert.Equal(InformationFrontier.Draw, exposure.Reason);
                Assert.Equal([0, 1], exposure.Seats);
            },
            exposure =>
            {
                Assert.Equal(InformationFrontier.Random, exposure.Reason);
                Assert.Equal([0, 1], exposure.Seats);
            },
            exposure =>
            {
                Assert.Equal(InformationFrontier.Reveal, exposure.Reason);
                Assert.Equal([0, 1], exposure.Seats);
            },
            exposure =>
            {
                Assert.Equal(InformationFrontier.Search, exposure.Reason);
                Assert.Equal([0, 1], exposure.Seats);
            });
    }

    [Fact]
    public void AnOrdinaryPublicDeterministicCardMoveDoesNotAdvanceTheFrontier()
    {
        var played = new CardsMoved(
            AreaRef.Player("HandsArea", 0),
            AreaRef.Player("SupportsArea", 0),
            [new Landing(10, 0)]);

        IReadOnlyList<InformationExposure> exposures = InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 8,
            information: [],
            [played],
            nextPrompt: null);

        Assert.Empty(exposures);
    }

    [Fact]
    public void RngConsumptionWithoutAVisibleEventStillProtectsTheTrace()
    {
        InformationExposure exposure = Assert.Single(InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 9,
            information: [],
            [],
            nextPrompt: null));

        Assert.Equal(InformationFrontier.Random, exposure.Reason);
        Assert.Equal([0, 1], exposure.Seats);
    }

    [Fact]
    public void ANoResultSearchSignalProtectsTheTraceWithoutEventsOrRng()
    {
        InformationExposure exposure = Assert.Single(InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 8,
            information: [new InformationSignal(InformationKind.Search)],
            events: [],
            nextPrompt: null));

        Assert.Equal(InformationFrontier.Search, exposure.Reason);
        Assert.Equal([0, 1], exposure.Seats);
    }

    [Fact]
    public void AShuffleProtectsTheTraceEvenWhenItConsumesNoRngWord()
    {
        InformationExposure exposure = Assert.Single(InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 8,
            information: [],
            [new AreaReordered(AreaRef.Player("PlayerDeck", 0), [10])],
            nextPrompt: null));

        Assert.Equal(InformationFrontier.Random, exposure.Reason);
    }

    [Fact]
    public void MovingAConcealedCardIntoAReadableAreaCountsAsAReveal()
    {
        InformationExposure exposure = Assert.Single(InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 8,
            information: [new InformationSignal(InformationKind.Reveal)],
            [new CardsMoved(
                AreaRef.Scenario("EncounterDeck"),
                AreaRef.Scenario("RevealingArea"),
                [new Landing(10, 0)])],
            nextPrompt: null));

        Assert.Equal(InformationFrontier.Reveal, exposure.Reason);
        Assert.Equal([0, 1], exposure.Seats);
    }

    [Fact]
    public void AFacedownCardMovedFromADeckDoesNotCountAsAReveal()
    {
        IReadOnlyList<InformationExposure> exposures = InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 8,
            information: [],
            [new CardsMoved(
                AreaRef.Player("PlayerDeck", 1),
                AreaRef.Player("EngagedEnemiesArea", 1),
                [new Landing(10, 0)])],
            nextPrompt: null);

        Assert.Empty(exposures);
    }

    [Fact]
    public void AHandSelectionMarkerDoesNotCountAsNewInformation()
    {
        var hand = new Prompt(
            1,
            Question.Element,
            TimingPriority.Untimed,
            "Mulligan",
            "Choose cards",
            Cancellable: false,
            [new Affordance(
                1,
                "Choose",
                7,
                1,
                "Choose",
                new TargetRequest([7], 0, 1, IsSearch: true))]);

        Assert.Empty(InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 8,
            information: [],
            [],
            hand));
    }

    [Fact]
    public void AConcealedCandidatePromptCountsAsALookWithoutSearchTargets()
    {
        var lookedAt = new Prompt(
            0,
            Question.Element,
            TimingPriority.Untimed,
            "Look",
            "Choose a looked-at card",
            Cancellable: false,
            [new Affordance(1, "Choose", 10, 0, "known face")])
        {
            ExposesConcealedCandidates = true,
        };

        InformationExposure exposure = Assert.Single(InformationFrontier.Classify(
            players: 2,
            rngBefore: 8,
            rngAfter: 8,
            information: [],
            [],
            lookedAt));

        Assert.Equal(InformationFrontier.Search, exposure.Reason);
        Assert.Equal([0, 1], exposure.Seats);
    }

    [Fact]
    public void ARevealedCardStillCountsWhenItReturnsToAConcealedHand()
    {
        InformationExposure exposure = Assert.Single(InformationFrontier.Classify(
            players: 1,
            rngBefore: 8,
            rngAfter: 8,
            information: [new InformationSignal(InformationKind.Reveal)],
            [
                new CardsMoved(
                    AreaRef.Player("PlayerDeck", 0),
                    AreaRef.Player("DiscardPile", 0),
                    [new Landing(10, 0)]),
                new CardsMoved(
                    AreaRef.Player("DiscardPile", 0),
                    AreaRef.Player("HandsArea", 0),
                    [new Landing(10, 0)]),
            ],
            nextPrompt: null));

        Assert.Equal(InformationFrontier.Reveal, exposure.Reason);
        Assert.Equal([0], exposure.Seats);
    }

    [Fact]
    public void DependentDecisionsMergeIntoOneIndivisibleAudienceSet()
    {
        IReadOnlyList<InformationExposure> merged = InformationFrontier.Merge(
            [new InformationExposure(InformationFrontier.Draw, [1])],
            [
                new InformationExposure(InformationFrontier.Draw, [0]),
                new InformationExposure(InformationFrontier.Search, [1]),
            ]);

        Assert.Equal([0, 1], merged[0].Seats);
        Assert.Equal(InformationFrontier.Search, merged[1].Reason);
    }
}

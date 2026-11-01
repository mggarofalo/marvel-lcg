using Marvel.Observability;
using Marvel.Transport;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Client;
using Marvel.Decisions;
using Marvel.Testing;
using Marvel.View;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class PresentationAssemblyPolicyTests
{
    [Fact]
    public void ViewUsesOnlyReviewedDependencies()
    {
        PresentationAssemblyPolicy.MatchesReviewedMarvelAssemblies(
            typeof(WorldDescriptor).Assembly,
            "Marvel.Rules");
        PresentationAssemblyPolicy.MatchesReviewedMarvelTypes(
            typeof(WorldDescriptor).Assembly,
            // Passive forced-window resolution evidence, with no decision authority.
            "Marvel.Rules.Events.AbilityWindowResult",
            "Marvel.Rules.Events.ActivityCompleted",
            "Marvel.Rules.Events.ActivityParticipant",
            "Marvel.Rules.Events.ActivityPhaseStep",
            "Marvel.Rules.Events.PhaseStarted",
            "Marvel.Rules.Events.AreaRef",
            "Marvel.Rules.Events.AreaReordered",
            "Marvel.Rules.Events.AttackBoundary",
            "Marvel.Rules.Events.AttackIdentity",
            "Marvel.Rules.Events.EventCause",
            "Marvel.Rules.Events.EventCauseKind",
            "Marvel.Rules.Events.AttackCompleted",
            // Passive applied boost facts, with no rules recalculation.
            "Marvel.Rules.Events.BoostResolved",
            "Marvel.Rules.Events.CardAttached",
            "Marvel.Rules.Events.CardDetached",
            "Marvel.Rules.Events.CardFormChanged",
            "Marvel.Rules.Events.CardsCreated",
            "Marvel.Rules.Events.CardsFlipped",
            "Marvel.Rules.Events.CardsMoved",
            "Marvel.Rules.Events.CardsShuffledIntoDeck",
            "Marvel.Rules.Events.ControlChanged",
            "Marvel.Rules.Events.CreatedCard",
            "Marvel.Rules.Events.FieldSet",
            "Marvel.Rules.Events.GameEvent",
            "Marvel.Rules.Events.Landing",
            "Marvel.Rules.Events.PlayAreaDetached",
            "Marvel.Rules.Events.PlayAreaJoined",
            // Passive successful-cancellation fact; no rules are reconstructed.
            "Marvel.Rules.Events.WhenRevealedCanceled",
            // Passive engine-owned attack lifetime and participants; no timing reconstruction.
            "Marvel.Rules.Play.ActiveAttack",
            // Engine-owned game-area summaries and Kang progress facts, copied without recomputing them.
            "Marvel.Rules.Play.GameAreaFacts",
            "Marvel.Rules.Play.GameAreaSummary",
            // Public area identity from the engine-owned typed auxiliary deck.
            "Marvel.Rules.Play.InvocationDeck",
            "Marvel.Rules.Play.KangPendingDeparture",
            "Marvel.Rules.Play.KangTable",
            "Marvel.Rules.Play.KangTableFacts",
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Play.PlayerLossReason",
            "Marvel.Rules.Play.VillainPhaseFocus",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.AffordanceAnchorKind",
            "Marvel.Rules.Prompts.ConditionalResourceBonus",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            // Passive public purpose without answer choices.
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.Question",
            // Detached copies of engine-authored payment facts, without invoking legality.
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.VariableRequest",
            "Marvel.Rules.Prompts.WindowChoice",
            "Marvel.Rules.State.Area",
            "Marvel.Rules.State.Card",
            "Marvel.Rules.State.CardControl",
            "Marvel.Rules.State.CardKind",
            "Marvel.Rules.State.CardKinds",
            // Passive evaluated values and original-source exposure; no mutable engine capability.
            "Marvel.Rules.State.CardSourceExposure",
            "Marvel.Rules.State.CardSourceSnapshot",
            "Marvel.Rules.State.CardValueBaseKind",
            "Marvel.Rules.State.CardValueEvaluation",
            "Marvel.Rules.State.CardValueStep",
            "Marvel.Rules.State.CardValueStepKind",
            "Marvel.Rules.State.CardValues",
            "Marvel.Rules.State.DeckType",
            "Marvel.Rules.State.DeckTypes",
            // Passive active-characteristic reads; no legality or mutation authority.
            "Marvel.Rules.State.EffectiveCards",
            // Passive engine-owned topology; filtered relationship projection only.
            "Marvel.Rules.State.EncounterDeckGroup",
            "Marvel.Rules.State.EncounterTopology",
            "Marvel.Rules.State.GameArea",
            "Marvel.Rules.State.ICardFacts",
            "Marvel.Rules.State.PersistentAbility",
            "Marvel.Rules.State.PersistentCardDescription",
            "Marvel.Rules.State.PersistentCardFacts",
            "Marvel.Rules.State.PersistentCardRelation",
            "Marvel.Rules.State.PersistentCost",
            "Marvel.Rules.State.PersistentEffect",
            "Marvel.Rules.State.PersistentThreshold",
            "Marvel.Rules.State.PersistentTrigger",
            // The engine's game-area answer for each projected card.
            "Marvel.Rules.State.Places",
            "Marvel.Rules.State.PlayArea",
            // Passive canonical source facts copied into View-owned values.
            "Marvel.Rules.State.PrintedStatFacts",
            "Marvel.Rules.State.PrintedStatValue",
            "Marvel.Rules.State.Seat",
            "Marvel.Rules.State.StateFields",
            "Marvel.Rules.State.Traits",
            "Marvel.Rules.State.World",
            // Only its read-only GameAreaOf(PlayArea) lookup; see the member policy.
            "Marvel.Rules.State.WorldTopology",
            "Marvel.Rules.Timing.AbilityType",
            "Marvel.Rules.Timing.Duration",
            "Marvel.Rules.Timing.TimingPriority");
    }

    [Fact]
    public void DecisionsUsesOnlyReviewedDependencies()
    {
        PresentationAssemblyPolicy.MatchesReviewedMarvelAssemblies(
            typeof(DecisionComposer).Assembly,
            "Marvel.Rules");
        PresentationAssemblyPolicy.MatchesReviewedMarvelTypes(
            typeof(DecisionComposer).Assembly,
            "Marvel.Rules.Play.Decision",
            "Marvel.Rules.Play.ResourceAllocation",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.DecisionAssessment",
            "Marvel.Rules.Prompts.DecisionAssessmentResult",
            "Marvel.Rules.Prompts.DecisionRejection",
            "Marvel.Rules.Prompts.ConditionalResourceBonus",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            "Marvel.Rules.Prompts.Question",
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourcePayment",
            // Engine-owned assessment of the current payment draft.
            "Marvel.Rules.Prompts.ResourcePaymentProgress",
            "Marvel.Rules.Prompts.ResourceComponentProgress",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.WindowChoice",
            "Marvel.Rules.Prompts.VariableRequest");
    }

    [Fact]
    public void ClientUsesOnlyReviewedDependencies()
    {
        PresentationAssemblyPolicy.MatchesReviewedMarvelAssemblies(
            typeof(LocalGameClient).Assembly,
            "Marvel.Rules",
            "Marvel.Protocol",
            "Marvel.Transport",
            "Marvel.Observability",
            "Marvel.View");
        PresentationAssemblyPolicy.MatchesReviewedMarvelTypes(
            typeof(LocalGameClient).Assembly,
            // Read-only campaign contracts and private client resume metadata; rules remain server-owned.
            "Marvel.Protocol.CampaignDeckAccess",
            "Marvel.Rules.Campaign.CampaignCard",
            // Campaign contracts are read-only; CampaignHost owns transitions and durable admission.
            "Marvel.Protocol.CampaignDescriptor",
            "Marvel.Protocol.CampaignGameAccess",
            "Marvel.Protocol.CampaignQuery",
            "Marvel.Rules.Campaign.CampaignPlayerState",
            "Marvel.Rules.Campaign.CampaignState",
            "Marvel.Rules.Campaign.CampaignPhase",
            "Marvel.Rules.Campaign.CampaignStage",
            "Marvel.Rules.Campaign.CampaignGameSetup",
            // Read-only public deck contracts and engine-authored corrections;
            // DeckCatalogTransportTests proves admission remains at the host.
            "Marvel.Protocol.DeckAssessmentResult",
            "Marvel.Protocol.DeckCard",
            // Bounded public research pages; research admission tests keep expansions unplayable.
            "Marvel.Protocol.DeckCardPage",
            // Printed-only preview validation rejects live handles and missing render fields.
            "Marvel.View.BoardCardPresentation",
            "Marvel.View.BoardFieldPresentation",
            "Marvel.View.BoardPrintedValueMark",
            "Marvel.Protocol.DeckCatalog",
            "Marvel.Protocol.DeckHero",
            "Marvel.Protocol.DeckQuery",
            "Marvel.Protocol.DeckResponse",
            "Marvel.Rules.Setup.DeckDiagnostic",
            // Passive forced-window resolution evidence, with no decision authority.
            "Marvel.Rules.Events.AbilityWindowResult",
            "Marvel.Rules.Events.ActivityCompleted",
            "Marvel.Rules.Events.ActivityParticipant",
            "Marvel.Rules.Events.PhaseStarted",
            "Marvel.Rules.Events.AreaRef",
            "Marvel.Rules.Events.AreaReordered",
            // Closed event-payload validation accepts the engine's passive completion receipt.
            "Marvel.Rules.Events.AttackBoundary",
            "Marvel.Rules.Events.AttackIdentity",
            "Marvel.Rules.Events.EventCause",
            "Marvel.Rules.Events.EventCauseKind",
            "Marvel.Rules.Events.AttackCompleted",
            // Passive applied boost facts, with no rules recalculation.
            "Marvel.Rules.Events.BoostResolved",
            "Marvel.Rules.Events.CardAttached",
            "Marvel.Rules.Events.CardDetached",
            "Marvel.Rules.Events.CardFormChanged",
            "Marvel.Rules.Events.CardsCreated",
            "Marvel.Rules.Events.CardsFlipped",
            "Marvel.Rules.Events.CardsMoved",
            "Marvel.Rules.Events.CardsShuffledIntoDeck",
            "Marvel.Rules.Events.ControlChanged",
            "Marvel.Rules.Events.CreatedCard",
            "Marvel.Rules.Events.FieldSet",
            "Marvel.Rules.Events.GameEvent",
            "Marvel.Rules.Events.Landing",
            "Marvel.Rules.Events.PlayAreaDetached",
            "Marvel.Rules.Events.PlayAreaJoined",
            // Passive successful-cancellation fact; no rules are reconstructed.
            "Marvel.Rules.Events.WhenRevealedCanceled",
            "Marvel.Rules.Timing.AbilityType",
            "Marvel.Rules.Timing.AbilityTypes",
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Play.PlayerLossReason",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.ConditionalResourceBonus",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            // Passive public situation validation grants no decision authority.
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.VariableRequest",
            "Marvel.Rules.Play.Decision",
            // Wire referential integrity, not game legality.
            "Marvel.Protocol.EncounterGroupContract",
            "Marvel.Protocol.EngineError",
            "Marvel.Protocol.EngineFailureDisposition",
            "Marvel.Protocol.EngineJson",
            "Marvel.Protocol.EntryClaim",
            "Marvel.Rules.Setup.PlayerDeckRecipe",
            "Marvel.Protocol.EngineRequest",
            "Marvel.Protocol.EngineResponse",
            "Marvel.Transport.EngineTransportException",
            "Marvel.Protocol.GameSpecification",
            "Marvel.Protocol.HeroSetupChoice",
            "Marvel.Protocol.HistoryDescriptor",
            "Marvel.Protocol.HistoryEntryDescriptor",
            "Marvel.Protocol.HistoryUndoStatus",
            "Marvel.Protocol.IEngineTransport",
            "Marvel.Protocol.ModularSetupChoice",
            "Marvel.Observability.OperationalLog",
            // The client selects and owns an explicitly queued embedded transport.
            "Marvel.Protocol.RuntimeIdentity",
            "Marvel.Protocol.ScenarioSetupChoice",
            // Discovery variant values are wire data validated by ScenarioChoiceContract;
            // scenario admission and legality remain server-owned.
            "Marvel.Protocol.ScenarioVariantChoice",
            "Marvel.Protocol.SeatInvitation",
            "Marvel.Protocol.SetupChoices",
            "Marvel.View.AreaDescriptor",
            "Marvel.View.CardContributionDescriptor",
            "Marvel.View.CardEffectiveValue",
            // Validates that each card names only a listed game area.
            "Marvel.View.CardLocationDescriptor",
            "Marvel.View.CardPersistentAbilityDescriptor",
            "Marvel.View.CardPersistentCostDescriptor",
            "Marvel.View.CardPersistentDescriptor",
            "Marvel.View.CardPersistentEffectDescriptor",
            "Marvel.View.CardPersistentThresholdDescriptor",
            "Marvel.View.CardPersistentTriggerDescriptor",
            "Marvel.View.CardPrintedValue",
            "Marvel.View.CardRelationDescriptor",
            "Marvel.View.CardValueCalculation",
            "Marvel.View.CardValueSourceDescriptor",
            "Marvel.View.CardDescriptor",
            "Marvel.View.CardFaceDescriptor",
            "Marvel.View.EventPresentation",
            "Marvel.View.GameAreaDescriptor",
            // Validates that Kang facts name only listed cards, seats, game areas and piles.
            "Marvel.View.KangDepartureDescriptor",
            "Marvel.View.KangTableDescriptor",
            "Marvel.View.OutcomeDescription",
            "Marvel.View.PendingSituationDescriptor",
            "Marvel.View.PlayerDescriptor",
            "Marvel.View.ViewerClaim",
            "Marvel.View.TableContextDescriptor",
            "Marvel.View.VillainPhaseFocusDescriptor",
            "Marvel.View.WorldDescriptor");
    }

    [Fact]
    public void PolicyRejectsUnreviewedDependencies()
    {
        Assert.Equal(
            ["Marvel.Core"],
            PresentationAssemblyPolicy.UnexpectedMarvelAssemblies(
                ["Marvel.Core", "Marvel.View", "System.Runtime"],
                ["Marvel.View"]));
        Assert.Equal(
            ["Marvel.Rules.State.World"],
            PresentationAssemblyPolicy.UnexpectedMarvelTypes(
                ["Marvel.Rules.Prompts.Prompt", "Marvel.Rules.State.World"],
                ["Marvel.Rules.Prompts.Prompt"]));
    }
}

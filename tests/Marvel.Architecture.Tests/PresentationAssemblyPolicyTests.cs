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
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Play.PlayerLossReason",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.AffordanceAnchorKind",
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
            "Marvel.Rules.State.PlayArea",
            // Passive canonical source facts copied into View-owned values.
            "Marvel.Rules.State.PrintedStatFacts",
            "Marvel.Rules.State.PrintedStatValue",
            "Marvel.Rules.State.Seat",
            "Marvel.Rules.State.StateFields",
            "Marvel.Rules.State.Traits",
            "Marvel.Rules.State.World",
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
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            "Marvel.Rules.Prompts.Question",
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourcePayment",
            // Engine-owned assessment of the current payment draft.
            "Marvel.Rules.Prompts.ResourcePaymentProgress",
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
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            // Passive public situation validation grants no decision authority.
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.VariableRequest",
            "Marvel.Rules.Play.Decision",
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
            "Marvel.Protocol.SeatInvitation",
            "Marvel.Protocol.SetupChoices",
            "Marvel.View.AreaDescriptor",
            "Marvel.View.CardContributionDescriptor",
            "Marvel.View.CardEffectiveValue",
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
            "Marvel.View.OutcomeDescription",
            "Marvel.View.PendingSituationDescriptor",
            "Marvel.View.PlayerDescriptor",
            "Marvel.View.ViewerClaim",
            "Marvel.View.TableContextDescriptor",
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

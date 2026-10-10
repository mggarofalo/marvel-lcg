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
            "Marvel.Rules.Events.AreaRef",
            "Marvel.Rules.Events.AreaReordered",
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
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.AffordanceAnchorKind",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            // Passive public purpose without answer choices.
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.Question",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
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
            "Marvel.Rules.Play.Resources",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourcePayment",
            // Engine-owned assessment of the current payment draft.
            "Marvel.Rules.Prompts.ResourcePaymentProgress",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.VariableRequest");
    }

    [Fact]
    public void ClientUsesOnlyReviewedDependencies()
    {
        PresentationAssemblyPolicy.MatchesReviewedMarvelAssemblies(
            typeof(LocalGameClient).Assembly,
            "Marvel.Decisions",
            "Marvel.Rules",
            "Marvel.Server",
            "Marvel.View");
        PresentationAssemblyPolicy.MatchesReviewedMarvelTypes(
            typeof(LocalGameClient).Assembly,
            "Marvel.Rules.Events.AreaRef",
            "Marvel.Rules.Events.AreaReordered",
            // Closed event-payload validation accepts the engine's passive completion receipt.
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
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            // Passive public situation validation grants no decision authority.
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.VariableRequest",
            "Marvel.Decisions.EngineDecision",
            "Marvel.Server.CompositeOperationalSink",
            "Marvel.Server.DatasetGameFactory",
            "Marvel.Server.EngineError",
            "Marvel.Server.EngineHost",
            "Marvel.Server.EngineRequest",
            "Marvel.Server.EngineResponse",
            "Marvel.Server.EngineTransportException",
            "Marvel.Server.GameSpecification",
            "Marvel.Server.HeroSetupChoice",
            "Marvel.Server.HistoryDescriptor",
            "Marvel.Server.HistoryEntryDescriptor",
            "Marvel.Server.HistoryUndoStatus",
            "Marvel.Server.HttpTelemetryExporter",
            "Marvel.Server.IEngineEndpoint",
            "Marvel.Server.IEngineTransport",
            "Marvel.Server.IGameFactory",
            "Marvel.Server.IOperationalSink",
            "Marvel.Server.ISessionCapabilityIssuer",
            "Marvel.Server.ISessionStore",
            "Marvel.Server.ITelemetryExporter",
            "Marvel.Server.InProcessTransport",
            "Marvel.Server.JsonTextOperationalSink",
            "Marvel.Server.ModularSetupChoice",
            "Marvel.Server.OperationalLog",
            "Marvel.Server.OperationalTelemetrySink",
            "Marvel.Server.RuntimeIdentity",
            "Marvel.Server.ScenarioSetupChoice",
            "Marvel.Server.SeatInvitation",
            "Marvel.Server.SetupChoices",
            "Marvel.Server.SocketTransport",
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
            "Marvel.View.IVisibilityPolicy",
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

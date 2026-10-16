using Marvel.Desktop;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Testing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PresentationBoundaryTests
{
    [Fact]
    public void GodotUsesOnlyReviewedDependencies()
    {
        PresentationAssemblyPolicy.MatchesReviewedMarvelAssemblies(
            typeof(Main).Assembly,
            "Marvel.Client",
            "Marvel.Decisions",
            "Marvel.Rules",
            "Marvel.Protocol",
            "Marvel.Desktop",
            "Marvel.Observability",
            "Marvel.View");
        PresentationAssemblyPolicy.MatchesReviewedMarvelTypes(
            typeof(Main).Assembly,
            // Current offered tuples and timing select controls; legality remains in Decisions.
            "Marvel.Decisions.WindowChoiceComposition",
            "Marvel.Rules.Prompts.WindowChoice",
            "Marvel.Rules.Timing.AbilityType",
            // Authorized receipts drive local acknowledgment without inventing an engine prompt.
            "Marvel.View.ForcedAbilityNotification",
            "Marvel.Rules.Events.AttackIdentity",
            "Marvel.Rules.Events.GameEvent",
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Play.PlayerLossReason",
            "Marvel.Rules.Prompts.Affordance",
            // Synthetic native selector specimens construct public prompt contracts only.
            "Marvel.Rules.Prompts.AffordanceAnchorKind",
            "Marvel.Rules.Prompts.Question",
            "Marvel.Rules.Timing.TimingPriority",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            // Public prompt purpose selects a presentation surface, not game legality.
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.VariableRequest",
            "Marvel.Desktop.ClientComposition",
            "Marvel.Client.ClientDraftDisposition",
            "Marvel.Client.ClientEntryResult",
            "Marvel.Client.ClientEntryAttempt",
            // Client-owned request/recovery state and its explicit draft instruction.
            "Marvel.Client.ClientGameLifecycle",
            "Marvel.Client.ClientLifecycleUpdate",
            "Marvel.Client.ClientMutationDisposition",
            "Marvel.Client.ClientSetupResult",
            "Marvel.Client.ClientStartupError",
            "Marvel.Client.GameProgressKind",
            "Marvel.Client.GameProgressPresentation",
            "Marvel.Client.GameSeed",
            "Marvel.Client.GameSetupSelection",
            "Marvel.Client.HistoryUndoPresentation",
            "Marvel.Client.InteractionTranscript",
            "Marvel.Client.InteractionTranscriptSetup",
            "Marvel.Client.LocalClientConnection",
            "Marvel.Client.LocalGameClient",
            "Marvel.Client.ModularConfiguration",
            "Marvel.Decisions.CostSelectionState",
            "Marvel.Decisions.DecisionComposer",
            // Decisions asks engine payment validation whether this draft needs another choice.
            "Marvel.Decisions.DecisionCommitmentAssessment",
            "Marvel.Decisions.DecisionProgressPresentation",
            "Marvel.Rules.Play.Decision",
            "Marvel.Decisions.PaymentProgress",
            // Guided source selection delegates slot matching to the engine.
            "Marvel.Decisions.DecisionResourceEligibility",
            // Icon declaration choices are assessed by the engine through the current draft.
            "Marvel.Decisions.DecisionResourceDeclarations",
            "Marvel.Decisions.ResourceIconAssignment",
            "Marvel.Decisions.TableDraftOperations",
            "Marvel.Decisions.TargetSelectionMode",
            "Marvel.Decisions.TargetSelectionProgress",
            "Marvel.Observability.BuildIdentity",
            // Only an accepted transport response may enqueue semantic receipts.
            "Marvel.Protocol.EngineError",
            "Marvel.Protocol.EngineResponse",
            "Marvel.Protocol.HeroSetupChoice",
            "Marvel.Protocol.HistoryDescriptor",
            "Marvel.Protocol.HistoryEntryDescriptor",
            "Marvel.Protocol.ModularSetupChoice",
            "Marvel.Protocol.RuntimeIdentity",
            "Marvel.Protocol.ScenarioSetupChoice",
            "Marvel.Protocol.SeatInvitation",
            "Marvel.Protocol.SetupChoices",
            "Marvel.View.AffordancePresentation",
            // Authorized attack participants; the engine owns their resolution lifetime.
            "Marvel.View.ActiveAttackDescriptor",
            "Marvel.View.AffordanceSourceDescriptor",
            // Catalogue grouping reads only authorized source zones and visible card identity.
            "Marvel.View.AreaDescriptor",
            "Marvel.View.CardDescriptor",
            "Marvel.View.CardFaceDescriptor",
            "Marvel.View.BoardAreaPresentation",
            "Marvel.View.BoardAreaProminence",
            "Marvel.View.BoardCardPresentation",
            "Marvel.View.BoardFieldPresentation",
            "Marvel.View.BoardPrintedValueMark",
            "Marvel.View.BoardLanePresentation",
            "Marvel.View.BoardLayout",
            "Marvel.View.BoardPlayerPresentation",
            "Marvel.View.BoardPresentation",
            "Marvel.View.BoardStageRole",
            // Passive authorized state summaries; no outcome calculation.
            "Marvel.View.CardStatePresentation",
            // Engine-evaluated quantity and modification flag; the renderer does not evaluate sources.
            "Marvel.View.CardEffectiveValue",
            // Disclosed source text and duration are read-only inspection facts.
            "Marvel.View.CardValueCalculation",
            "Marvel.View.CardValueSourceDescriptor",
            "Marvel.View.CardValueDuration",
            // Authorized persistent source relations and complete checked ability meanings.
            "Marvel.View.CardPersistentDescriptor",
            "Marvel.View.CardRelationDescriptor",
            "Marvel.View.CardContributionDescriptor",
            "Marvel.View.CardPersistentAbilityDescriptor",
            "Marvel.View.CardPersistentTriggerDescriptor",
            "Marvel.View.CardPersistentCostDescriptor",
            "Marvel.View.CardPersistentEffectDescriptor",
            "Marvel.View.CardPersistentThresholdDescriptor",
            "Marvel.View.DecisionReceiptContext",
            // View groups authorized costs/results around the accepted intention without determining legality.
            "Marvel.View.AcceptedActivityOccurrence",
            "Marvel.View.ResponseReceiptPresenter",
            "Marvel.View.EventBatchPresentation",
            "Marvel.View.EventChronology",
            "Marvel.View.EventCuePlanner",
            "Marvel.View.EventMotionKind",
            "Marvel.View.EventPresentation",
            // Typed facts derived locally from visibility-filtered events.
            "Marvel.View.EventActivityDescriptor",
            "Marvel.View.EventActivityKind",
            // Native read-only witnesses locate cards in authorized area descriptors.
            "Marvel.View.GameAreaDescriptor",
            "Marvel.View.OutcomeDescription",
            "Marvel.View.ActivitySubject",
            "Marvel.View.ActivitySubjectRole",
            "Marvel.View.ActivityQuantity",
            "Marvel.View.ActivityStatusChange",
            // Public waiting copy contains no actionable private prompt.
            "Marvel.View.PendingSituationPresentation",
            "Marvel.View.PromptPresentation",
            "Marvel.View.PlayerDescriptor",
            "Marvel.View.PlayerSummaryDescriptor",
            // The decision heading identifies the acting seat from public player information.
            "Marvel.View.PaymentSourcePresentation",
            "Marvel.View.RelationshipKind",
            "Marvel.View.TableContextDescriptor",
            "Marvel.View.TableRelationshipDescriptor",
            "Marvel.View.WorldDescriptor");
    }
}

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
            "Marvel.Server",
            "Marvel.View");
        PresentationAssemblyPolicy.MatchesReviewedMarvelTypes(
            typeof(Main).Assembly,
            "Marvel.Rules.Events.GameEvent",
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Play.Resources",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            // Public prompt purpose selects a presentation surface, not game legality.
            "Marvel.Rules.Prompts.PublicDecisionKind",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.VariableRequest",
            "Marvel.Client.ClientComposition",
            "Marvel.Client.ClientDraftDisposition",
            "Marvel.Client.ClientEntryResult",
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
            "Marvel.Decisions.DecisionProgressPresentation",
            "Marvel.Decisions.EngineDecision",
            "Marvel.Decisions.PaymentProgress",
            // Guided source selection delegates slot matching to the engine.
            "Marvel.Decisions.DecisionResourceEligibility",
            "Marvel.Decisions.ResourceIconAssignment",
            "Marvel.Decisions.TableDraftOperations",
            "Marvel.Decisions.TargetSelectionMode",
            "Marvel.Decisions.TargetSelectionProgress",
            "Marvel.Server.EngineBuildIdentity",
            "Marvel.Server.EngineResponse",
            "Marvel.Server.HeroSetupChoice",
            "Marvel.Server.HistoryDescriptor",
            "Marvel.Server.HistoryEntryDescriptor",
            "Marvel.Server.ModularSetupChoice",
            "Marvel.Server.RuntimeIdentity",
            "Marvel.Server.ScenarioSetupChoice",
            "Marvel.Server.SeatInvitation",
            "Marvel.Server.SetupChoices",
            "Marvel.View.AffordancePresentation",
            "Marvel.View.AffordanceSourceDescriptor",
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
            "Marvel.View.DecisionReceiptContext",
            "Marvel.View.ResponseReceiptPresenter",
            "Marvel.View.EventBatchPresentation",
            "Marvel.View.EventChronology",
            "Marvel.View.EventCuePlanner",
            "Marvel.View.EventMotionKind",
            "Marvel.View.EventPresentation",
            // Public waiting copy contains no actionable private prompt.
            "Marvel.View.PendingSituationPresentation",
            "Marvel.View.PromptPresentation",
            "Marvel.View.PlayerSummaryDescriptor",
            "Marvel.View.PaymentSourcePresentation",
            "Marvel.View.RelationshipKind",
            "Marvel.View.TableContextDescriptor",
            "Marvel.View.TableRelationshipDescriptor",
            "Marvel.View.WorldDescriptor");
    }
}

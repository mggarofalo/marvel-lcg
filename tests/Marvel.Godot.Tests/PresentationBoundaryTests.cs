using Marvel.Testing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PresentationBoundaryTests
{
    [Fact]
    public void GodotUsesOnlyReviewedDependencies()
    {
        PresentationAssemblyPolicy.AllowsOnlyMarvelAssemblies(
            typeof(Main).Assembly,
            "Marvel.Client",
            "Marvel.Decisions",
            "Marvel.Rules",
            "Marvel.Server",
            "Marvel.View");
        PresentationAssemblyPolicy.AllowsOnlyMarvelTypes(
            typeof(Main).Assembly,
            "Marvel.Rules.Events.GameEvent",
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Play.Resources",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.Prompts.VariableRequest",
            "Marvel.Client.ClientComposition",
            "Marvel.Client.ClientEntryResult",
            "Marvel.Client.ClientMutationDisposition",
            "Marvel.Client.ClientResolutionResult",
            "Marvel.Client.ClientSession",
            "Marvel.Client.ClientSessionDisposition",
            "Marvel.Client.ClientSetupResult",
            "Marvel.Client.ClientStartupError",
            "Marvel.Client.ClientSynchronizationResult",
            "Marvel.Client.GameProgressKind",
            "Marvel.Client.GameProgressPresentation",
            "Marvel.Client.GameSeed",
            "Marvel.Client.GameSetupSelection",
            "Marvel.Client.LocalClientConnection",
            "Marvel.Client.LocalGameClient",
            "Marvel.Client.ModularConfiguration",
            "Marvel.Decisions.CostSelectionState",
            "Marvel.Decisions.DecisionComposer",
            "Marvel.Decisions.DecisionProgressPresentation",
            "Marvel.Decisions.EngineDecision",
            "Marvel.Decisions.PaymentProgress",
            "Marvel.Decisions.ResourceIconAssignment",
            "Marvel.Decisions.TargetSelectionMode",
            "Marvel.Decisions.TargetSelectionProgress",
            "Marvel.Server.EngineResponse",
            "Marvel.Server.HeroSetupChoice",
            "Marvel.Server.ModularSetupChoice",
            "Marvel.Server.ScenarioSetupChoice",
            "Marvel.Server.SeatInvitation",
            "Marvel.Server.SetupChoices",
            "Marvel.View.AffordancePresentation",
            "Marvel.View.BoardAreaPresentation",
            "Marvel.View.BoardCardPresentation",
            "Marvel.View.BoardFieldPresentation",
            "Marvel.View.BoardLanePresentation",
            "Marvel.View.BoardLayout",
            "Marvel.View.BoardPlayerPresentation",
            "Marvel.View.BoardPresentation",
            "Marvel.View.EventBatchPresentation",
            "Marvel.View.EventChronology",
            "Marvel.View.EventCuePlanner",
            "Marvel.View.EventMotionKind",
            "Marvel.View.EventPresentation",
            "Marvel.View.PromptPresentation",
            "Marvel.View.WorldDescriptor");
    }
}

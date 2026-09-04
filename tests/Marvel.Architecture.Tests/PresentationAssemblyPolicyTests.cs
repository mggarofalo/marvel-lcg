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
            "Marvel.Rules.Events.CardAttached",
            "Marvel.Rules.Events.CardDetached",
            "Marvel.Rules.Events.CardFormChanged",
            "Marvel.Rules.Events.CardsCreated",
            "Marvel.Rules.Events.CardsFlipped",
            "Marvel.Rules.Events.CardsMoved",
            "Marvel.Rules.Events.ControlChanged",
            "Marvel.Rules.Events.CreatedCard",
            "Marvel.Rules.Events.FieldSet",
            "Marvel.Rules.Events.GameEvent",
            "Marvel.Rules.Events.Landing",
            "Marvel.Rules.Events.PlayAreaDetached",
            "Marvel.Rules.Events.PlayAreaJoined",
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
            "Marvel.Rules.Prompts.Question",
            "Marvel.Rules.Prompts.ResourceSource",
            "Marvel.Rules.Prompts.TargetRequest",
            "Marvel.Rules.State.Area",
            "Marvel.Rules.State.Card",
            "Marvel.Rules.State.CardKind",
            "Marvel.Rules.State.DeckType",
            "Marvel.Rules.State.DeckTypes",
            "Marvel.Rules.State.FacedownDrones",
            "Marvel.Rules.State.GameArea",
            "Marvel.Rules.State.ICardFacts",
            "Marvel.Rules.State.PlayArea",
            "Marvel.Rules.State.Seat",
            "Marvel.Rules.State.StateFields",
            "Marvel.Rules.State.Traits",
            "Marvel.Rules.State.World",
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
            "Marvel.Rules.Prompts.ResourceCost",
            "Marvel.Rules.Prompts.ResourcePayment",
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
            "Marvel.Rules.Events.CardAttached",
            "Marvel.Rules.Events.CardDetached",
            "Marvel.Rules.Events.CardFormChanged",
            "Marvel.Rules.Events.CardsCreated",
            "Marvel.Rules.Events.CardsFlipped",
            "Marvel.Rules.Events.CardsMoved",
            "Marvel.Rules.Events.ControlChanged",
            "Marvel.Rules.Events.CreatedCard",
            "Marvel.Rules.Events.FieldSet",
            "Marvel.Rules.Events.GameEvent",
            "Marvel.Rules.Events.Landing",
            "Marvel.Rules.Events.PlayAreaDetached",
            "Marvel.Rules.Events.PlayAreaJoined",
            "Marvel.Rules.Play.Outcome",
            "Marvel.Rules.Prompts.Affordance",
            "Marvel.Rules.Prompts.CostOption",
            "Marvel.Rules.Prompts.Prompt",
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
            "Marvel.View.CardDescriptor",
            "Marvel.View.CardFaceDescriptor",
            "Marvel.View.GameAreaDescriptor",
            "Marvel.View.IVisibilityPolicy",
            "Marvel.View.PlayerDescriptor",
            "Marvel.View.ViewerClaim",
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

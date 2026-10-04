using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CoreDecisionMeaningTransportTests : EngineHostTestBase
{
    [Fact]
    public void LegalCoreJourneyRetainsDistinctCommitmentsAndConsequencesThroughJson()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        const string gameId = "decision-meanings";
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", gameId,
            new GameSpecification("rhino", ["spider_man"], [], Seed: 1)));
        Prompt mulligan = Assert.IsType<Prompt>(opened.Prompt);
        Assert.All(Assert.Single(mulligan.Affordances).Targets!.Details!.Values,
            detail => Assert.Contains("Discard and replace", detail));
        EngineResponse turn = host.Exchange(EngineRequest.ResolveGame("keep", gameId,
            RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        Assert.Equal("End turn", turn.Prompt!.DeclineLabel);
        Affordance change = Assert.Single(turn.Prompt.Affordances, option => option.Verb == Game.ChangeForm);
        EngineResponse hero = host.Exchange(EngineRequest.ResolveGame("form", gameId,
            RequiredCapability(opened), new EngineDecision(change.Id, []), turn.Revision));
        EngineResponse transported = EngineJson.ReadResponse(EngineJson.Write(hero));
        Affordance attack = Assert.Single(transported.Prompt!.Affordances,
            option => option.Verb == BasicPowers.AttackVerb);

        Assert.Contains("Exhaust Spider-Man", attack.Description);
        Assert.Contains("12/14 HP", Assert.Single(attack.Targets!.Details!).Value);
        Assert.Equal("End turn", PromptPresentation.From(transported.Prompt, transported.World!).DeclineLabel);

        EngineResponse ended = host.Exchange(EngineRequest.ResolveGame("end", gameId,
            RequiredCapability(opened), EngineDecision.Decline, hero.Revision));
        Assert.Null(ended.Error);
        Assert.False(ended.Prompt!.Cancellable);
        Affordance discards = Assert.Single(ended.Prompt.Affordances);
        Assert.Equal(Game.EndPhaseVerb, discards.Verb);
        Assert.All(discards.Targets!.Details!.Values,
            detail => Assert.Contains("Discard this card", detail));
    }

    [Fact]
    public void CompletedCoreDefenseRetainsParticipantsThroughHostVisibilityAndJson()
    {
        const string gameId = "completed-defense";
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", gameId,
            new GameSpecification("rhino", ["spider_man"], [], Seed: 1)));
        string capability = RequiredCapability(opened);
        EngineResponse current = host.Exchange(EngineRequest.ResolveGame("keep", gameId,
            capability, TakeOnly(opened), opened.Revision));
        Affordance change = Assert.Single(current.Prompt!.Affordances, option => option.Verb == Game.ChangeForm);
        current = host.Exchange(EngineRequest.ResolveGame("form", gameId, capability,
            new EngineDecision(change.Id, []), current.Revision));
        current = host.Exchange(EngineRequest.ResolveGame("end-turn", gameId, capability,
            EngineDecision.Decline, current.Revision));
        Affordance phase = Assert.Single(current.Prompt!.Affordances);
        current = host.Exchange(EngineRequest.ResolveGame("phase-discards", gameId, capability,
            new EngineDecision(phase.Id, phase.Targets!.Legal.Take(phase.Targets.Min).ToArray()), current.Revision));
        Assert.Null(current.Error);
        Assert.Equal(Question.Opportunity, current.Prompt!.Asking);
        current = host.Exchange(EngineRequest.ResolveGame("pass-interrupt", gameId, capability,
            EngineDecision.Decline, current.Revision));
        Assert.Equal(Question.Defender, current.Prompt!.Asking);
        Affordance defender = Assert.Single(current.Prompt.Affordances);
        current = host.Exchange(EngineRequest.ResolveGame("defend", gameId, capability,
            new EngineDecision(defender.Id, []), current.Revision));
        EngineResponse transported = EngineJson.ReadResponse(EngineJson.Write(current));

        Assert.Null(transported.Error);
        AttackCompleted completion = Assert.Single(transported.Events.OfType<AttackCompleted>());
        Assert.Equal(defender.AnchorId, completion.Target);
        Assert.Equal(defender.AnchorId, completion.Defender);
        Assert.Equal("Rhino", completion.Subjects![completion.Enemy]);
        Assert.Equal("Spider-Man", completion.Subjects[completion.Defender]);
        EventBatchPresentation batch = EventCuePlanner.Plan(transported.Events,
            transported.World!, Outcome.Unfinished);
        Assert.Contains(batch.Highlights, cue => cue.Summary.Contains("Spider-Man defended", StringComparison.Ordinal));
    }
}
